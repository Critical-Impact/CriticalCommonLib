using System;
using System.Collections.Generic;
using CriticalCommonLib.Models;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace CriticalCommonLib.Services
{
    public interface ICharacterMonitor : IDisposable
    {
        void UpdateCharacter(Character character);
        void RemoveCharacter(ulong characterId);
        void RefreshActiveCharacter();
        event CharacterMonitor.ActiveRetainerChangedDelegate? OnActiveRetainerChanged;
        event CharacterMonitor.ActiveRetainerChangedDelegate? OnActiveRetainerLoaded;
        event CharacterMonitor.ActiveFreeCompanyChangedDelegate? OnActiveFreeCompanyChanged;
        event CharacterMonitor.ActiveHouseChangedDelegate? OnActiveHouseChanged;
        event CharacterMonitor.CharacterUpdatedDelegate? OnCharacterUpdated;
        event CharacterMonitor.CharacterRemovedDelegate? OnCharacterRemoved;
        event CharacterMonitor.CharacterJobChangedDelegate? OnCharacterJobChanged;
        public delegate void CharacterLoginEventDelegate(ulong characterId);
        public event CharacterLoginEventDelegate? OnCharacterLoggedIn;
        public event CharacterLoginEventDelegate? OnCharacterLoggedOut;
        KeyValuePair<ulong, Character>[] GetPlayerCharacters();
        KeyValuePair<ulong, Character>[] GetFreeCompanies();
        KeyValuePair<ulong, Character>[] GetHouses();
        KeyValuePair<ulong, Character>[] AllCharacters();
        KeyValuePair<ulong, Character>[] GetFreeCompanyCharacters(ulong freeCompanyId);
        HashSet<uint> GetWorldIds();
        Dictionary<ulong, Character> Characters { get; }
        Character? GetCharacterByName(string name, ulong ownerId);
        bool BelongsToActiveCharacter(ulong characterId);
        public unsafe List<ulong> GetOwnedHouseIds();
        KeyValuePair<ulong, Character>[] GetRetainerCharacters(ulong ownerId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters();
        KeyValuePair<ulong, Character>[] GetCharacterHouses();
        KeyValuePair<ulong, Character>[] GetCharacterHouses(ulong characterId);
        public bool IsCharacter(ulong characterId);
        public bool IsRetainer(ulong characterId);
        public bool IsFreeCompany(ulong characterId);
        public bool IsHousing(ulong characterId);
        public Character? GetCharacterById(ulong characterId);
        public Character? GetParentCharacterById(ulong characterId);
        public string GetCharacterNameById(ulong characterId, bool owner = false);

        void LoadExistingRetainers(Dictionary<ulong, Character> characters);
        ulong ActiveRetainerId { get; }
        ulong ActiveCharacterId { get; }
        ulong ActiveHouseId { get; }
        ulong ActiveFreeCompanyId { get; }
        public ulong InternalCharacterId { get; }
        public bool InternalHasHousePermission { get; }
        public short InternalRoomId { get; }
        public byte InternalDivisionId { get; }
        public sbyte InternalPlotId { get; }
        public sbyte InternalWardId { get; }
        public ulong InternalHouseId { get; }
        public uint CorrectedTerritoryTypeId { get; }
        public Character? ActiveCharacter { get; }
        public Character? ActiveFreeCompany { get; }
        public Character? ActiveRetainer { get; }
        public bool IsLoggedIn { get; }
        public ulong LocalContentId { get; }
        void OverrideActiveCharacter(ulong activeCharacter);
        void OverrideActiveRetainer(ulong activeRetainer);
        void OverrideActiveFreeCompany(ulong freeCompanyId);
    }
}