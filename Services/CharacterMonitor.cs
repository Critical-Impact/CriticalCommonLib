using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin;
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
            _clientState.Login += ClientStateOnLogin;
            _clientState.Logout += ClientStateOnLogout;
            RefreshActiveCharacter();
        }

        private void ClientStateOnLogout(object? sender, EventArgs e)
        {
        }

        private void ClientStateOnLogin(object? sender, EventArgs e)
        {
            PluginLog.Verbose("CharacterMonitor: Logged In");
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
            if (opcode == (0x022F) && direction == NetworkMessageDirection.ZoneDown) //Hardcode for now
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

        private void CheckRetainerId()
        {
            var retainerId = this.InternalRetainerId;
            if (ActiveRetainer != retainerId)
            {
                unsafe
                {
                    PluginLog.Verbose("CharacterMonitor: Active retainer id has changed");
                    var retainerBag0 = GameInterface.GetContainer(InventoryType.RetainerBag0);
                    var retainerBag1 = GameInterface.GetContainer(InventoryType.RetainerBag1);
                    var retainerBag2 = GameInterface.GetContainer(InventoryType.RetainerBag2);
                    var retainerBag3 = GameInterface.GetContainer(InventoryType.RetainerBag3);
                    var retainerBag4 = GameInterface.GetContainer(InventoryType.RetainerBag4);
                    var retainerBag5 = GameInterface.GetContainer(InventoryType.RetainerBag5);
                    var retainerBag6 = GameInterface.GetContainer(InventoryType.RetainerBag6);
                    //Make sure the retainer is fully loaded before firing the event
                    if (retainerBag0 != null && retainerBag1 != null && retainerBag2 != null && retainerBag3 != null &&
                        retainerBag4 != null && retainerBag5 != null && retainerBag6 != null || (ActiveRetainer != 0 && retainerId == 0))
                    {
                        _activeRetainer = retainerId;
                        OnActiveRetainerChanged?.Invoke(ActiveRetainer);
                    }
                }
            }
        }
        
        
        
        private async Task CheckCharacterId()
        {
            var characterId = InternalCharacterId;
            if (characterId != null && ActiveCharacter != characterId.Value)
            {
                PluginLog.Verbose("CharacterMonitor: Active character id has changed");
                _activeCharacter = characterId.Value;
                await Task.Delay(200);
                RefreshActiveCharacter();
            }
        }
        
        private async void FrameworkOnOnUpdateEvent(Framework framework)
        {
            await CheckCharacterId();
            CheckRetainerId();
        }

        public void Dispose()
        {
            _framework.Update -= FrameworkOnOnUpdateEvent;
            _network.NetworkMessage -= OnNetworkMessage;
            _clientState.Login -= ClientStateOnLogin;
            _clientState.Logout -= ClientStateOnLogout;
        }
    }
}
