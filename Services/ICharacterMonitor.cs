using System;
using System.Collections.Generic;
using CriticalCommonLib.Models;

namespace CriticalCommonLib
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
        KeyValuePair<ulong, Character>[] GetPlayerCharacters();
        KeyValuePair<ulong, Character>[] GetFreeCompanies();
        KeyValuePair<ulong, Character>[] GetHouses();
        KeyValuePair<ulong, Character>[] AllCharacters();
        Dictionary<ulong, Character> Characters { get; }
        Character? GetCharacterByName(string name, ulong ownerId);
        bool BelongsToActiveCharacter(ulong characterId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters(ulong retainerId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters();
        KeyValuePair<ulong, Character>[] GetCharacterHouses();
        KeyValuePair<ulong, Character>[] GetCharacterHouses(ulong characterId);
        public bool IsCharacter(ulong characterId);
        public bool IsRetainer(ulong characterId);
        public bool IsFreeCompany(ulong characterId);
        public bool IsHousing(ulong characterId);
        public Character? GetCharacterById(ulong characterId);

        void LoadExistingRetainers(Dictionary<ulong, Character> characters);
        ulong ActiveRetainer { get; }
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
        public Character? ActiveCharacter { get; }
        public Character? ActiveFreeCompany { get; }
        public bool IsLoggedIn { get; }
        public ulong LocalContentId { get; }
        void OverrideActiveCharacter(ulong activeCharacter);
        void OverrideActiveRetainer(ulong activeRetainer);
        void OverrideActiveFreeCompany(ulong freeCompanyId);
    }
}