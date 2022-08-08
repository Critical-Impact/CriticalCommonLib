using System.Collections.Generic;
using CriticalCommonLib.Models;

namespace CriticalCommonLib
{
    public interface ICharacterMonitor
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
        Character? GetCharacterByName(string name, ulong ownerId);
        bool BelongsToActiveCharacter(ulong characterId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters(ulong retainerId);
        KeyValuePair<ulong, Character>[] GetRetainerCharacters();
        void LoadExistingRetainers(Dictionary<ulong, Character> characters);
        ulong ActiveRetainer { get; }
        ulong ActiveCharacter { get; }
        void OverrideActiveCharacter(ulong activeCharacter);
        void OverrideActiveRetainer(ulong activeRetainer);
    }
}