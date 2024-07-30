using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace CriticalCommonLib.Services
{
    public class CharacterMonitor : ICharacterMonitor
    {
        private readonly IFramework _framework;
        private readonly IClientState _clientState;
        private readonly ExcelCache _excelCache;
        private Dictionary<ulong, Character> _characters;
        
        private ulong _activeRetainerId;
        private ulong _activeCharacterId;
        private ulong _activeFreeCompanyId;
        private ulong _activeHouseId;
        private uint? _activeClassJobId;
        private bool _isRetainerLoaded ;
        private bool _isFreeCompanyLoaded;
        private bool _isHouseLoaded;
        private bool _initialCheck;
        public CharacterMonitor(IFramework framework, IClientState clientState, ExcelCache excelCache)
        {
            _framework = framework;
            _clientState = clientState;
            _excelCache = excelCache;
            _territoryMap = new Dictionary<uint, uint>();
            _characters = new Dictionary<ulong, Character>();
            _framework.Update += FrameworkOnOnUpdateEvent;
        }

        public Character? ActiveFreeCompany =>
            _characters.ContainsKey(_activeFreeCompanyId) ? _characters[_activeFreeCompanyId] : null;

        public Character? ActiveHouse =>
            _characters.ContainsKey(_activeHouseId) ? _characters[_activeHouseId] : null;
        
        public bool IsLoggedIn
        {
            get
            {
                return _clientState.IsLoggedIn;
            }
        }

        public ulong LocalContentId
        {
            get
            {
                return _clientState.LocalContentId;
            }
        }

        public void UpdateCharacter(Character character)
        {
            _framework.RunOnFrameworkThread(() => { OnCharacterUpdated?.Invoke(character); });
        }

        public void RemoveCharacter(ulong characterId)
        {
            if (_characters.ContainsKey(characterId))
            {
                _characters.Remove(characterId);
                _framework.RunOnFrameworkThread(() => { OnCharacterRemoved?.Invoke(characterId); });
            }
        }

        public unsafe void RefreshActiveCharacter()
        {
            if (_clientState.IsLoggedIn && _clientState.LocalPlayer != null && _clientState.LocalContentId != 0)
            {
                Service.Log.Verbose("CharacterMonitor: Character has changed to " + _clientState.LocalContentId);
                Character character;
                if (_characters.ContainsKey(_clientState.LocalContentId))
                {
                    character = _characters[_clientState.LocalContentId];
                }
                else
                {
                    character = new Character();
                    character.CharacterId = _clientState.LocalContentId;
                    _characters[character.CharacterId] = character;
                }
                var infoProxy = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetInfoModule()->GetInfoProxyById(InfoProxyId.FreeCompany);
                InfoProxyFreeCompany* freeCompanyInfoProxy = null;
                if (infoProxy != null)
                {
                    freeCompanyInfoProxy = (InfoProxyFreeCompany*)infoProxy;
                }

                if (character.UpdateFromCurrentPlayer(_clientState.LocalPlayer, freeCompanyInfoProxy))
                {
                    _framework.RunOnFrameworkThread(() => { OnCharacterUpdated?.Invoke(character); });
                }
            }
            else
            {
                _framework.RunOnFrameworkThread(() => { OnCharacterUpdated?.Invoke(null); });
            }
        }

        public delegate void ActiveRetainerChangedDelegate(ulong retainerId);
        public delegate void ActiveFreeCompanyChangedDelegate(ulong freeCompanyId);
        public delegate void ActiveHouseChangedDelegate(ulong houseId, sbyte wardId,sbyte plotId, byte divisionId, short roomId, bool hasHousePermission);
        public event ActiveRetainerChangedDelegate? OnActiveRetainerChanged; 

        public event ActiveRetainerChangedDelegate? OnActiveRetainerLoaded; 
        public event ActiveFreeCompanyChangedDelegate? OnActiveFreeCompanyChanged; 
        public event ActiveHouseChangedDelegate? OnActiveHouseChanged; 
        
        public delegate void CharacterUpdatedDelegate(Character? character);
        public event CharacterUpdatedDelegate? OnCharacterUpdated;
        
        public delegate void CharacterRemovedDelegate(ulong characterId);
        public event CharacterRemovedDelegate? OnCharacterRemoved;

        public delegate void CharacterJobChangedDelegate();

        public event CharacterJobChangedDelegate? OnCharacterJobChanged;
        
        public event ICharacterMonitor.CharacterLoginEventDelegate? OnCharacterLoggedIn;
        public event ICharacterMonitor.CharacterLoginEventDelegate? OnCharacterLoggedOut;

        public KeyValuePair<ulong, Character>[] GetFreeCompanyCharacters(ulong freeCompanyId)
        {
            return Characters.Where(c => c.Value.Name != "" && (c.Value.CharacterType == CharacterType.Character && c.Value.FreeCompanyId == freeCompanyId)).ToArray();
        }

        public HashSet<uint> GetWorldIds()
        {
            return Characters.Select(c => c.Value.WorldId).ToHashSet();
        }

        public Dictionary<ulong, Character> Characters => _characters;

        public KeyValuePair<ulong, Character>[] GetPlayerCharacters()
        {
            return Characters.Where(c => c.Value.OwnerId == 0 && c.Value.CharacterType == CharacterType.Character && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] GetFreeCompanies()
        {
            return Characters.Where(c => c.Value.OwnerId == 0 && c.Value.CharacterType == CharacterType.FreeCompanyChest && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] GetHouses()
        {
            return Characters.Where(c => c.Value.OwnerId == 0 && c.Value.CharacterType == CharacterType.Housing && c.Key != 0 && c.Value.HousingName != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] AllCharacters()
        {
            return Characters.Where(c => c.Value.Name != "" || (c.Value.CharacterType == CharacterType.Housing && c.Value.HousingName != "")).ToArray();
        }

        public Character? GetCharacterByName(string name, ulong ownerId)
        {
            return Characters.Select(c => c.Value).FirstOrDefault(c => c.Name == name && c.OwnerId == ownerId);
        }

        public bool IsCharacter(ulong characterId)
        {
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId].CharacterType == CharacterType.Character;
            }
            return false;
        }

        public bool IsRetainer(ulong characterId)
        {
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId].CharacterType == CharacterType.Retainer;
            }
            return false;
        }

        public bool IsFreeCompany(ulong characterId)
        {
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId].CharacterType == CharacterType.FreeCompanyChest;
            }
            return false;
        }

        public bool IsHousing(ulong characterId)
        {
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId].CharacterType == CharacterType.Housing;
            }
            return false;
        }

        public bool IsHouse(ulong houseId)
        {
            if (Characters.ContainsKey(houseId))
            {
                return Characters[houseId].CharacterType == CharacterType.Housing;
            }
            return false;
        }

        public Character? GetCharacterById(ulong characterId)
        {
            if (Characters.ContainsKey(characterId))
            {
                return Characters[characterId];
            }
            return null;
        }

        public Character? GetParentCharacterById(ulong characterId)
        {
            var character = GetCharacterById(characterId);

            if (IsFreeCompany(characterId) || IsHousing(characterId)) return character;
            if (character != null)
            {
                return character.CharacterType == CharacterType.Character ?
                    character : GetCharacterById(character.OwnerId);
            }

            return null;
        }

        public string GetCharacterNameById(ulong characterId, bool owner = false)
        {
            if (!owner) return GetCharacterById(characterId)?.FormattedName ?? "Unknown";
            var character = GetParentCharacterById(characterId);
            if (character != null && character.CharacterId == characterId)
                return "";
            return character?.FormattedName ?? "Unknown";
        }
        
        public bool BelongsToActiveCharacter(ulong characterId)
        {
            if (_activeCharacterId == 0)
            {
                return false;
            }
            if (IsFreeCompany(characterId))
            {
                var activeCharacter = ActiveCharacter;
                if (activeCharacter == null)
                {
                    return false;
                }

                return activeCharacter.FreeCompanyId == characterId;
            }
            if (IsHouse(characterId))
            {
                var activeCharacter = ActiveCharacter;
                if (activeCharacter == null)
                {
                    return false;
                }

                if (Characters.ContainsKey(characterId))
                {
                    return Characters[characterId].Owners.Contains(activeCharacter.CharacterId);
                }
            }
            if (characterId != 0 && Characters.ContainsKey(characterId))
            {
                return Characters[characterId].OwnerId == _activeCharacterId || Characters[characterId].CharacterId == _activeCharacterId;
            }
            return false;
        }

        public KeyValuePair<ulong, Character>[] GetRetainerCharacters(ulong ownerId)
        {
            return Characters.Where(c => c.Value.OwnerId == ownerId && c.Value.CharacterType == CharacterType.Retainer && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] GetRetainerCharacters()
        {
            return Characters.Where(c => c.Value.OwnerId != 0 && c.Value.CharacterType == CharacterType.Retainer && c.Key != 0 && c.Value.Name != "").ToArray();
        }

        public KeyValuePair<ulong, Character>[] GetCharacterHouses(ulong characterId)
        {
            return Characters.Where(c => c.Value.Owners.Contains(characterId) && c.Value.CharacterType == CharacterType.Housing && c.Key != 0 && c.Value.HousingName != "").ToArray();
        }
        
        
        public KeyValuePair<ulong, Character>[] GetCharacterHouses()
        {
            return Characters.Where(c => c.Value.Owners.Count != 0 && c.Value.CharacterType == CharacterType.Housing && c.Key != 0 && c.Value.HousingName != "").ToArray();
        }

        public void LoadExistingRetainers(Dictionary<ulong, Character> characters)
        {
            Service.Log.Verbose("CharacterMonitor: Loading existing retainers");
            foreach (var character in characters)
            {
                _characters[character.Key] = character.Value;
            }
        }

        
        public ulong InternalRetainerId
        {
            get
            {
                unsafe
                {
                    var clientInterfaceUiModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework
                        .Instance()->UIModule->GetItemOrderModule();
                    var module = clientInterfaceUiModule;
                    if (module != null)
                    {
                        return module->ActiveRetainerId;
                    }
                    return 0;
                }
            }
        }
       
        public ulong InternalFreeCompanyId
        {
            get
            {
                unsafe
                {
                    var infoProxy = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetInfoModule()->GetInfoProxyById(InfoProxyId.FreeCompany);
                    if (infoProxy != null)
                    {
                        var freeCompanyInfoProxy = (InfoProxyFreeCompany*)infoProxy;
                        return freeCompanyInfoProxy->Id;
                    }

                    return 0;
                }
            }
        }

        private readonly Dictionary<uint, uint> _territoryMap;
        
        public ulong InternalHouseId
        {
            get
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    var character = _clientState.LocalPlayer;
                    var territoryType = _clientState.TerritoryType;
                    
                    if (housingManager != null && character != null)
                    {
                        if (InternalPlotId == 0 || InternalPlotId == -1 || character.HomeWorld.Id == 0 || territoryType == 0)
                        {                        
                            return 0;
                        }

                        if (InternalPlotId < -1 && InternalRoomId == 0)
                        {
                            //In the apartment Lobby
                            return 0;
                        }

                        if (!_territoryMap.ContainsKey(territoryType))
                        {
                            var territory = _excelCache.GetTerritoryTypeExSheet().GetRow(territoryType);
                            if (territory == null)
                            {
                                return 0;
                            }

                            _territoryMap[territoryType] = territory.PlaceNameZone.Row;
                        }
                        var zoneId = _territoryMap[territoryType];
                        byte sb1 = (byte)InternalWardId;
                        byte sb2 = (byte)InternalPlotId;
                        ushort sh1 = (ushort)InternalRoomId;
                        ushort sh2 = (ushort)character.HomeWorld.Id;
                        ushort sh3 = (ushort)zoneId;
                        var houseId = ((ulong)sb1 << 56) | ((ulong)sb2 << 48) | ((ulong)sh1 << 32) | ((ulong)sh2 << 16) | sh3;
                        var hasHousePermission = InternalHasHousePermission;
                        if (houseId != 0 && (hasHousePermission || _characters.ContainsKey(houseId)))
                        {
                            return houseId;
                        }
                    }

                    return 0;
                }
            }
        }
        
        public sbyte InternalWardId
        {
            get
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    if (housingManager != null)
                    {
                        var wardId = housingManager->GetCurrentWard();
                        if (wardId != 0)
                        {
                            return wardId;
                        }
                    }

                    return 0;
                }
            }
        }
        
        public sbyte InternalPlotId
        {
            get
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    if (housingManager != null)
                    {
                        var plotId = housingManager->GetCurrentPlot();
                        if (plotId != 0)
                        {
                            return plotId;
                        }
                    }

                    return 0;
                }
            }
        }
        
        public byte InternalDivisionId
        {
            get
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    if (housingManager != null)
                    {
                        if (housingManager->GetCurrentPlot() > 30)
                        {
                            return 2;
                        }
                        var divisionId = housingManager->GetCurrentDivision();
                        if (divisionId != 0)
                        {
                            return divisionId;
                        }
                    }

                    return 0;
                }
            }
        }
        
        
        public short InternalRoomId
        {
            get
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    if (housingManager != null)
                    {
                        var roomId = housingManager->GetCurrentRoom();
                        if (roomId != 0)
                        {
                            return roomId;
                        }
                    }

                    return 0;
                }
            }
        }
        
        
        public bool InternalHasHousePermission
        {
            get
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    if (housingManager != null)
                    {
                        var hasPermissions = housingManager->HasHousePermissions();
                        return hasPermissions;
                    }

                    return false;
                }
            }
        }
        
        public ulong InternalCharacterId => _clientState.LocalPlayer != null ? _clientState.LocalContentId : 0;

        public bool IsRetainerLoaded => _isRetainerLoaded;
        public ulong ActiveRetainerId => _activeRetainerId;
        public ulong ActiveCharacterId => _activeCharacterId;
        public ulong ActiveFreeCompanyId => _activeFreeCompanyId;

        public ulong ActiveHouseId => _activeHouseId;

        public Character? ActiveCharacter =>
            _characters.ContainsKey(_activeCharacterId) ? _characters[_activeCharacterId] : null;

        public Character? ActiveRetainer =>
            _characters.ContainsKey(_activeRetainerId) ? _characters[_activeRetainerId] : null;
        public uint? ActiveClassJobId => _activeClassJobId;

        private DateTime? _lastRetainerSwap;
        private DateTime? _lastCharacterSwap;
        private DateTime? _lastClassJobSwap;
        private DateTime? _lastRetainerCheck;
        private DateTime? _lastFreeCompanyCheck;
        private DateTime? _lastHouseCheck;
        private DateTime? _lastHouseUpdate;
        private DateTime? _lastFreeCompanyUpdate;

        public void OverrideActiveCharacter(ulong activeCharacter)
        {
            _activeCharacterId = activeCharacter;
        }

        public void OverrideActiveRetainer(ulong activeRetainer)
        {
            _activeRetainerId = activeRetainer;
        }

        public void OverrideActiveFreeCompany(ulong activeFreeCompanyId)
        {
            _activeFreeCompanyId = activeFreeCompanyId;
        }

        public void OverrideHouseId(ulong houseId)
        {
            _activeHouseId = houseId;
        }


        private void CheckRetainerId(DateTime lastUpdate)
        {
            var retainerId = this.InternalRetainerId;
            if (ActiveRetainerId != retainerId)
            {
                if (_lastRetainerSwap == null)
                {
                    _isRetainerLoaded = false;
                    _activeRetainerId = retainerId;
                    _framework.RunOnFrameworkThread(() => { OnActiveRetainerChanged?.Invoke(ActiveRetainerId); });
                    _lastRetainerSwap = lastUpdate;
                    return;
                }
            }
            var waitTime = retainerId == 0 ? 1 : 2;
            //This is the best I can come up with due it the retainer ID changing but the inventory takes almost a second to locate(I assume as it loads in from the network). This won't really take bad network conditions into account but until I can come up with a more reliable way it'll have to do
            if(_lastRetainerSwap != null && _lastRetainerSwap.Value.AddSeconds(waitTime) <= lastUpdate)
            {
                Service.Log.Verbose("CharacterMonitor: Active retainer id has changed");
                _lastRetainerSwap = null;
                //Make sure the retainer is fully loaded before firing the event
                if (retainerId != 0)
                {
                    _activeRetainerId = retainerId;
                    _isRetainerLoaded = true;
                    _framework.RunOnFrameworkThread(() => { OnActiveRetainerLoaded?.Invoke(ActiveRetainerId); });
                }
            }

            if (_lastRetainerSwap == null && ActiveRetainerId != 0 && !_isRetainerLoaded)
            {
                _isRetainerLoaded = true;
            }
        }

        private void CheckFreeCompanyId(DateTime lastUpdate)
        {
            var freeCompanyId = this.InternalFreeCompanyId;
            if (ActiveFreeCompanyId != freeCompanyId)
            {
                if (_lastFreeCompanyCheck == null)
                {
                    _isFreeCompanyLoaded = false;
                    _activeFreeCompanyId = freeCompanyId;
                    _framework.RunOnFrameworkThread(() => { OnActiveFreeCompanyChanged?.Invoke(ActiveFreeCompanyId); });
                    _lastFreeCompanyCheck = lastUpdate;
                    return;
                }
            }
            var waitTime = freeCompanyId == 0 ? 1 : 2;
            
            if(_lastFreeCompanyCheck != null && _lastFreeCompanyCheck.Value.AddSeconds(waitTime) <= lastUpdate)
            {
                Service.Log.Verbose("CharacterMonitor: Active free company id has changed to " + freeCompanyId);
                _lastFreeCompanyCheck = null;
                //Make sure the retainer is fully loaded before firing the event
                if (freeCompanyId != 0)
                {
                    _activeFreeCompanyId = freeCompanyId;
                    _isFreeCompanyLoaded = true;
                    _framework.RunOnFrameworkThread(() => { OnActiveFreeCompanyChanged?.Invoke(ActiveFreeCompanyId); });
                }
            }

            if (_lastFreeCompanyCheck == null && ActiveFreeCompanyId != 0 && !_isFreeCompanyLoaded)
            {
                _isFreeCompanyLoaded = true;
            }
        }

        private void CheckHouseId(DateTime lastUpdate)
        {
            var houseId = this.InternalHouseId;
            if (ActiveHouseId != houseId)
            {
                if (_lastHouseCheck == null)
                {
                    _isHouseLoaded = false;
                    _activeHouseId = houseId;
                    _framework.RunOnFrameworkThread(() => { OnActiveHouseChanged?.Invoke(ActiveHouseId, InternalWardId, InternalPlotId, InternalDivisionId, InternalRoomId, InternalHasHousePermission); });
                    _lastHouseCheck = lastUpdate;
                    return;
                }
            }
            var waitTime = houseId == 0 ? 1 : 2;
            
            if(_lastHouseCheck != null && _lastHouseCheck.Value.AddSeconds(waitTime) <= lastUpdate)
            {
                Service.Log.Verbose("CharacterMonitor: Active house id has changed to " + houseId);
                _lastHouseCheck = null;
                //Make sure the retainer is fully loaded before firing the event
                if (houseId != 0)
                {
                    _activeHouseId = houseId;
                    _isHouseLoaded = true;
                    _framework.RunOnFrameworkThread(() => { OnActiveHouseChanged?.Invoke(ActiveHouseId, InternalWardId, InternalPlotId, InternalDivisionId, InternalRoomId, InternalHasHousePermission); });
                }
            }

            if (_lastHouseCheck == null && ActiveHouseId != 0 && !_isHouseLoaded)
            {
                _isHouseLoaded = true;
            }
        }
        
        private void CheckCharacterId(DateTime lastUpdate)
        {
            var characterId = InternalCharacterId;
            if ( ActiveCharacterId != characterId)
            {
                if (_lastCharacterSwap == null)
                {
                    _lastCharacterSwap = lastUpdate;
                    return;
                }
            }
            
            if(_lastCharacterSwap != null && _lastCharacterSwap.Value.AddSeconds(2) <= lastUpdate)
            {
                Service.Log.Verbose("CharacterMonitor: Active character id has changed");
                _lastCharacterSwap = null;
                //Make sure the character is fully loaded before firing the event
                if (ActiveCharacterId  != characterId)
                {
                    _activeCharacterId = characterId;
                    RefreshActiveCharacter();
                    if (_activeCharacterId != 0)
                    {
                        OnCharacterLoggedIn?.Invoke(_activeCharacterId);
                    }
                    else
                    {
                        OnCharacterLoggedOut?.Invoke(_activeCharacterId);
                    }
                }
            }
        }
        
        
        private unsafe void UpdateRetainers(DateTime lastUpdateTime)
        {

            var retainerManager = RetainerManager.Instance();
            if (retainerManager == null)
            {
                return;
            }
            if (_clientState.LocalPlayer == null || retainerManager->Ready != 1)
                return;
            if (_lastRetainerCheck == null)
            {
                _lastRetainerCheck = lastUpdateTime;
                return;
            }
            if (_lastRetainerCheck.Value.AddSeconds(2) <= lastUpdateTime)
            {
                _lastRetainerCheck = null;
                var retainerList = retainerManager->Retainers;
                var count = retainerManager->GetRetainerCount();
                var currentCharacter = _clientState.LocalPlayer;
                if (currentCharacter != null)
                {
                    for (var i = 0; i < retainerList.Length; i++)
                    {
                        var retainerInformation = retainerList[i];
                        if (retainerInformation.RetainerId != 0)
                        {
                            Character character;
                            if (_characters.ContainsKey(retainerInformation.RetainerId))
                            {
                                character = _characters[retainerInformation.RetainerId];
                            }
                            else
                            {
                                character = new Character();
                                character.CharacterId = retainerInformation.RetainerId;
                                _characters[retainerInformation.RetainerId] = character;
                            }

                            if (character.UpdateFromRetainerInformation(retainerInformation, currentCharacter, i))
                            {
                                Service.Log.Debug("Retainer " + retainerInformation.RetainerId + " was updated.");
                                character.OwnerId = _clientState.LocalContentId;
                                _framework.RunOnFrameworkThread(() =>
                                {
                                    OnCharacterUpdated?.Invoke(character);
                                });
                            }
                        }
                    }
                }
            }
        }
        
        
        private unsafe void UpdateFreeCompany(DateTime lastUpdateTime)
        {

            if (_clientState.LocalPlayer == null)
                return;
            if (_lastFreeCompanyUpdate == null)
            {
                _lastFreeCompanyUpdate = lastUpdateTime;
                return;
            }
            if (_lastFreeCompanyUpdate.Value.AddSeconds(2) <= lastUpdateTime)
            {
                _lastFreeCompanyUpdate = null;
                var infoProxy = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetInfoModule()->GetInfoProxyById(InfoProxyId.FreeCompany);
                if (infoProxy != null)
                {
                    var freeCompanyInfoProxy = (InfoProxyFreeCompany*)infoProxy;
                    var freeCompanyId = freeCompanyInfoProxy->Id;

                    if (freeCompanyId != 0)
                    {
                        Character character;
                        if (_characters.ContainsKey(freeCompanyId))
                        {
                            character = _characters[freeCompanyId];
                        }
                        else
                        {
                            character = new Character();
                            character.CharacterId = freeCompanyId;
                            _characters[freeCompanyId] = character;
                        }
                        
                        if (character.UpdateFromInfoProxyFreeCompany(freeCompanyInfoProxy))
                        {
                            Service.Log.Debug("Free Company " + character.CharacterId + " was updated.");
                            _framework.RunOnFrameworkThread(() =>
                            {
                                OnCharacterUpdated?.Invoke(character);
                            });
                        }
                        else
                        {
                            
                        }
                    }
                }
            }
        }
        
        private unsafe void UpdateHouses(DateTime lastUpdateTime)
        {

            if (_clientState.LocalPlayer == null)
                return;
            if (_lastHouseUpdate == null)
            {
                _lastHouseUpdate = lastUpdateTime;
                return;
            }
            if (_lastHouseUpdate.Value.AddSeconds(2) <= lastUpdateTime)
            {
                _lastHouseUpdate = null;
                var houseId = InternalHouseId;

                if (houseId != 0)
                {
                    Character character;
                    if (_characters.ContainsKey(houseId))
                    {
                        character = _characters[houseId];
                    }
                    else
                    {
                        character = new Character();
                        character.CharacterId = houseId;
                        _characters[houseId] = character;
                    }
                    var housingManager = HousingManager.Instance();
                    var internalCharacter = _clientState.LocalPlayer;
                    var territoryTypeId = _clientState.TerritoryType;
                    if (!_territoryMap.ContainsKey(territoryTypeId))
                    {
                        var territory = _excelCache.GetTerritoryTypeExSheet().GetRow(territoryTypeId);
                        if (territory == null)
                        {
                            return;
                        }

                        _territoryMap[territoryTypeId] = territory.PlaceNameZone.Row;
                    }
                    var zoneId = _territoryMap[territoryTypeId];

                    if (housingManager != null && internalCharacter != null && territoryTypeId != 0)
                    {
                        if (character.UpdateFromCurrentHouse(housingManager, internalCharacter, zoneId, territoryTypeId))
                        {
                            Service.Log.Debug("Free Company " + character.CharacterId + " was updated.");
                            _framework.RunOnFrameworkThread(() => { OnCharacterUpdated?.Invoke(character); });
                        }
                    }
                }
            }
        }
        
        private void FrameworkOnOnUpdateEvent(IFramework framework)
        {
            //Check the active character once when we first load, this is to stop the check from being run off-thread
            if (!_initialCheck)
            {
                RefreshActiveCharacter();
                _initialCheck = true;
            }
            UpdateRetainers(framework.LastUpdate);
            UpdateFreeCompany(framework.LastUpdate);
            UpdateHouses(framework.LastUpdate);
            CheckCharacterId(framework.LastUpdate);
            CheckRetainerId(framework.LastUpdate);
            CheckFreeCompanyId(framework.LastUpdate);
            CheckCurrency(framework.LastUpdate);
            CheckCurrentClassJob(framework.LastUpdate);
            CheckHouseId(framework.LastUpdate);
        }

        private uint? CurrentClassJobId
        {
            get
            {
                if (_clientState.IsLoggedIn && _clientState.LocalPlayer != null)
                {
                    return _clientState.LocalPlayer?.ClassJob.Id ?? null;
                }

                return null;
            }
        }

        private void CheckCurrentClassJob(DateTime frameworkLastUpdate)
        {
            var currentClassJobId = CurrentClassJobId;
            if (currentClassJobId != 0 && ActiveClassJobId != currentClassJobId)
            {
                if (_lastClassJobSwap == null)
                {
                    _lastClassJobSwap = frameworkLastUpdate;
                    return;
                }
            }
            
            if(_lastClassJobSwap != null && _lastClassJobSwap.Value.AddSeconds(1) <= frameworkLastUpdate)
            {
                Service.Log.Verbose("CharacterMonitor: Active character job has changed.");
                _lastClassJobSwap = null;
                //Make sure the character is fully loaded before firing the event
                if (ActiveClassJobId  != currentClassJobId)
                {
                    if (currentClassJobId != null && _activeClassJobId != null)
                    {
                        _framework.RunOnFrameworkThread(() => { OnCharacterJobChanged?.Invoke(); });
                        RefreshActiveCharacter();
                    }
                    _activeClassJobId = currentClassJobId;
                }
            }
        }

        private void CheckCurrency(DateTime lastUpdate)
        {
        }
        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed)
            {
                if(disposing)
                {
                    _framework.Update -= FrameworkOnOnUpdateEvent;
                }
            }
            _disposed = true;         
        }
        
        ~CharacterMonitor()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}
