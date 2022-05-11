using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using CriticalCommonLib.UiModule;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Network;
using Dalamud.Logging;

namespace CriticalCommonLib
{
    public class CharacterMonitor : IDisposable
    {
        private Dictionary<ulong, Character> _characters;
        
        private ulong _activeRetainer;
        private ulong _activeCharacter;
        private bool _isRetainerLoaded = false;

        
        public CharacterMonitor()
        {
            _characters = new Dictionary<ulong, Character>();
            Service.Network.NetworkMessage +=OnNetworkMessage;
            Service.Framework.Update += FrameworkOnOnUpdateEvent;
            RefreshActiveCharacter();
        }

        public CharacterMonitor(bool noSetup)
        {
            _characters = new();
        }
        
        public void RefreshActiveCharacter()
        {
            if (Service.ClientState.IsLoggedIn && Service.ClientState.LocalPlayer != null)
            {
                PluginLog.Verbose("CharacterMonitor: Character has changed to " + Service.ClientState.LocalContentId);
                var character = new Character();
                character.UpdateFromCurrentPlayer(Service.ClientState.LocalPlayer);
                character.CharacterId = Service.ClientState.LocalContentId;
                _characters[character.CharacterId] = character;
                OnCharacterUpdated?.Invoke(character);
            }
            else
            {
                OnCharacterUpdated?.Invoke(null);
            }
        }

        public delegate void ActiveRetainerChangedDelegate(ulong retainerId);
        public event ActiveRetainerChangedDelegate? OnActiveRetainerChanged; 

        public event ActiveRetainerChangedDelegate? OnActiveRetainerLoaded; 
        
        public delegate void CharacterUpdatedDelegate(Character? character);
        public event CharacterUpdatedDelegate? OnCharacterUpdated;

        public Dictionary<ulong, Character> Characters => _characters;

        public KeyValuePair<ulong, Character>[] GetPlayerCharacters()
        {
            return Characters.Where(c => c.Value.OwnerId == 0 && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] AllCharacters()
        {
            return Characters.Where(c => c.Value.Name != "").ToArray();
        }

        public Character? GetCharacterByName(string name, ulong ownerId)
        {
            return Characters.Select(c => c.Value).FirstOrDefault(c => c.Name == name && c.OwnerId == ownerId);
        }
        
        public bool IsRetainer(ulong characterId)
        {
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId].OwnerId != 0;
            }
            return false;
        }
        
        public bool BelongsToActiveCharacter(ulong characterId)
        {
            if (characterId != 0 && Characters.ContainsKey(characterId))
            {
                return Characters[characterId].OwnerId == _activeCharacter || Characters[characterId].CharacterId == _activeCharacter;
            }
            return false;
        }

        public KeyValuePair<ulong, Character>[] GetRetainerCharacters(ulong retainerId)
        {
            return Characters.Where(c => c.Value.OwnerId == retainerId && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] GetRetainerCharacters()
        {
            return Characters.Where(c => c.Value.OwnerId != 0 && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public void LoadExistingRetainers(Dictionary<ulong, Character> characters)
        {
            PluginLog.Verbose("CharacterMonitor: Loading existing retainers");
            foreach (var character in characters)
            {
                _characters[character.Key] = character.Value;
            }
        }

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (opcode == Utils.GetOpcode("RetainerInformation") && direction == NetworkMessageDirection.ZoneDown)
            {
                PluginLog.Verbose("CharacterMonitor: Retainer update received");
                var retainerInformation = NetworkDecoder.DecodeRetainerInformation(dataptr);
                var character = new Character();
                character.UpdateFromNetworkRetainerInformation(retainerInformation);
                character.OwnerId = Service.ClientState.LocalContentId;
                _characters[character.CharacterId] = character;
            }
        }
        
        private ulong InternalRetainerId
        {
            get
            {
                unsafe
                {
                    var clientInterfaceUiModule = (ItemOrderModule*)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework
                        .Instance()->UIModule->GetItemOrderModule();
                    var module = clientInterfaceUiModule;
                    if (module != null)
                    {
                        return module->RetainerID;
                    }
                    return 0;
                }
            }
        }
        
        private ulong InternalCharacterId
        {
            get
            {
                unsafe
                {
                    if (Service.ClientState.LocalPlayer)
                    {
                        return Service.ClientState.LocalContentId;
                    }

                    return 0;
                }
            }
        }

        public bool IsRetainerLoaded => _isRetainerLoaded;
        public ulong ActiveRetainer => _activeRetainer;
        public ulong ActiveCharacter => _activeCharacter;

        public DateTime? _lastRetainerSwap;
        public DateTime? _lastCharacterSwap;

        public void OverrideActiveCharacter(ulong activeCharacter)
        {
            _activeCharacter = activeCharacter;
        }

        public void OverrideActiveRetainer(ulong activeRetainer)
        {
            _activeRetainer = activeRetainer;
        }

        private void CheckRetainerId(DateTime lastUpdate)
        {
            var retainerId = this.InternalRetainerId;
            if (ActiveRetainer != retainerId)
            {
                if (_lastRetainerSwap == null)
                {
                    _isRetainerLoaded = false;
                    _activeRetainer = retainerId;
                    OnActiveRetainerChanged?.Invoke(ActiveRetainer);
                    _lastRetainerSwap = lastUpdate;
                    return;
                }
            }
            var waitTime = retainerId == 0 ? 1 : 2;
            //This is the best I can come up with due it the retainer ID changing but the inventory takes almost a second to loate(I assume as it loads in from the network). This won't really take bad network conditions into account but until I can come up with a more reliable way it'll have to do
            if(_lastRetainerSwap != null && _lastRetainerSwap.Value.AddSeconds(waitTime) <= lastUpdate)
            {
                PluginLog.Verbose("CharacterMonitor: Active retainer id has changed");
                _lastRetainerSwap = null;
                //Make sure the retainer is fully loaded before firing the event
                if (ActiveRetainer != retainerId)
                {
                    _activeRetainer = retainerId;
                    _isRetainerLoaded = retainerId != 0;
                    OnActiveRetainerLoaded?.Invoke(ActiveRetainer);
                }
            }

            if (_lastRetainerSwap == null && ActiveRetainer != 0 && !_isRetainerLoaded)
            {
                _isRetainerLoaded = true;
            }
        }
        
        
        
        private void CheckCharacterId(DateTime lastUpdate)
        {
            var characterId = InternalCharacterId;
            if (characterId != 0 && ActiveCharacter != characterId)
            {
                if (_lastCharacterSwap == null)
                {
                    _lastCharacterSwap = lastUpdate;
                    return;
                }
            }
            
            if(_lastCharacterSwap != null && _lastCharacterSwap.Value.AddSeconds(2) <= lastUpdate)
            {
                PluginLog.Verbose("CharacterMonitor: Active character id has changed");
                _lastCharacterSwap = null;
                //Make sure the character is fully loaded before firing the event
                if (ActiveCharacter  != characterId)
                {
                    _activeCharacter = characterId;
                    RefreshActiveCharacter();
                }
            }
        }
        
        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            CheckCharacterId(framework.LastUpdate);
            CheckRetainerId(framework.LastUpdate);
        }

        public void Dispose()
        {
            Service.Framework.Update -= FrameworkOnOnUpdateEvent;
            Service.Network.NetworkMessage -= OnNetworkMessage;
        }
    }
}
