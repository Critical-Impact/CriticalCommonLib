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
        event CharacterMonitor.CharacterUpdatedDelegate? OnCharacterUpdated;
        event CharacterMonitor.CharacterRemovedDelegate? OnCharacterRemoved;
        event CharacterMonitor.CharacterJobChangedDelegate? OnCharacterJobChanged;
        event CharacterMonitor.GilUpdatedDelegate? OnGilUpdated;
        KeyValuePair<ulong, Character>[] GetPlayerCharacters();
        KeyValuePair<ulong, Character>[] AllCharacters();
        Dictionary<ulong, Character> Characters { get; }
        Character? GetCharacterByName(string name, ulong ownerId);
        bool BelongsToActiveCharacter(ulong characterId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters(ulong retainerId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters();
        public bool IsRetainer(ulong characterId);
        public Character? GetCharacterById(ulong characterId);

        void LoadExistingRetainers(Dictionary<ulong, Character> characters);
        ulong ActiveRetainer { get; }
        ulong ActiveCharacterId { get; }
        public Character? ActiveCharacter { get; }
        public bool IsLoggedIn { get; }
        public ulong LocalContentId { get; }
        void OverrideActiveCharacter(ulong activeCharacter);
        void OverrideActiveRetainer(ulong activeRetainer);
    }
}