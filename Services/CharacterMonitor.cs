using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Network;
using Dalamud.Logging;
using FFXIVClientInterface;

namespace InventoryTools
{
    public class CharacterMonitor : IDisposable
    {
        private Framework _framework;

        private GameNetwork _network;

        private ClientInterface _clientInterface;

        private ClientState _clientState;

        private Dictionary<ulong, Character> _characters;
        
        private ulong _activeRetainer;
        private ulong _activeCharacter;

        
        public CharacterMonitor(GameNetwork network, ClientInterface clientInterface, Framework framework, ClientState clientState)
        {
            _framework = framework;
            _characters = new Dictionary<ulong, Character>();
            _network = network;
            _clientState = clientState;
            _network.NetworkMessage +=OnNetworkMessage;
            _clientInterface = clientInterface;
            _framework.Update += FrameworkOnOnUpdateEvent;
            RefreshActiveCharacter();
        }
        
        public void RefreshActiveCharacter()
        {
            if (_clientState.IsLoggedIn && _clientState.LocalPlayer != null)
            {
                PluginLog.Verbose("CharacterMonitor: Character has changed to " + _clientState.LocalContentId);
                var character = new Character();
                character.UpdateFromCurrentPlayer(_clientState.LocalPlayer);
                character.CharacterId = _clientState.LocalContentId;
                _characters[character.CharacterId] = character;
                OnCharacterUpdated?.Invoke(character);
            }
            else
            {
                OnCharacterUpdated?.Invoke(null);
            }
        }

        public delegate void ActiveRetainerChangedDelegate(ulong retainerId);
        public event ActiveRetainerChangedDelegate OnActiveRetainerChanged; 
        
        public delegate void CharacterUpdatedDelegate(Character character);
        public event CharacterUpdatedDelegate OnCharacterUpdated;

        public Dictionary<ulong, Character> Characters => _characters;

        public KeyValuePair<ulong, Character>[] GetPlayerCharacters()
        {
            return Characters.Where(c => c.Value.OwnerId == 0 && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] AllCharacters()
        {
            return Characters.Where(c => c.Value.Name != "").ToArray();
        }

        public Character GetCharacterByName(string name, ulong ownerId)
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
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId].OwnerId == _clientState.LocalContentId || Characters[characterId].CharacterId == _clientState.LocalContentId;
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
                character.OwnerId = _clientState.LocalContentId;
                _characters[character.CharacterId] = character;
                OnCharacterUpdated?.Invoke(character);
            }
        }
        
        private ulong InternalRetainerId
        {
            get
            {
                unsafe
                {
                    var clientInterfaceUiModule = _clientInterface.UiModule;
                    var module = clientInterfaceUiModule?.ItemOrderModule;
                    if (module != null)
                    {
                        var moduleData = module.Data;
                        if (moduleData != null)
                        {
                            return moduleData->RetainerID;
                        }
                    }
                    return 0;
                }
            }
        }
        
        private ulong? InternalCharacterId
        {
            get
            {
                unsafe
                {
                    if (_clientState.LocalPlayer)
                    {
                        return _clientState.LocalContentId;
                    }

                    return null;
                }
            }
        }

        public ulong ActiveRetainer => _activeRetainer;
        public ulong ActiveCharacter => _activeCharacter;

        public DateTime? _lastRetainerSwap;
        public DateTime? _lastCharacterSwap;

        private void CheckRetainerId(DateTime lastUpdate)
        {
            var retainerId = this.InternalRetainerId;
            if (ActiveRetainer != retainerId)
            {
                if (_lastRetainerSwap == null)
                {
                    _lastRetainerSwap = lastUpdate;
                    return;
                }
                //This is the best I can come up with due it the retainer ID changing but the inventory takes almost a second to loate(I assume as it loads in from the network). This won't really take bad network conditions into account but until I can come up with a more reliable way it'll have to do
                if(_lastRetainerSwap.Value.AddSeconds(1) <= lastUpdate)
                {
                    PluginLog.Verbose("CharacterMonitor: Active retainer id has changed");
                    _lastRetainerSwap = null;
                    //Make sure the retainer is fully loaded before firing the event
                    if (ActiveRetainer != 0 && retainerId == 0)
                    {
                        _activeRetainer = retainerId;
                        OnActiveRetainerChanged?.Invoke(ActiveRetainer);
                    }
                    else
                    {
                        _activeRetainer = retainerId;
                        OnActiveRetainerChanged?.Invoke(ActiveRetainer);
                    }   
                }
            }
        }
        
        
        
        private void CheckCharacterId(DateTime lastUpdate)
        {
            var characterId = InternalCharacterId;
            if (characterId != null && ActiveCharacter != characterId)
            {
                if (_lastCharacterSwap == null)
                {
                    _lastCharacterSwap = lastUpdate;
                    return;
                }
                if(_lastCharacterSwap.Value.AddSeconds(1) <= lastUpdate)
                {
                    PluginLog.Verbose("CharacterMonitor: Active character id has changed");
                    _lastCharacterSwap = null;
                    //Make sure the character is fully loaded before firing the event
                    if (ActiveCharacter != 0 && characterId == 0)
                    {
                        _activeCharacter = characterId.Value;
                        RefreshActiveCharacter();
                    }
                    else
                    {
                        _activeCharacter = characterId.Value;
                        RefreshActiveCharacter();
                    }   
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
            _framework.Update -= FrameworkOnOnUpdateEvent;
            _network.NetworkMessage -= OnNetworkMessage;
        }
    }
}
