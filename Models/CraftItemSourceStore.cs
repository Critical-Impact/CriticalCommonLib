using System;
using System.Collections.Generic;
using CriticalCommonLib.Crafting;

namespace CriticalCommonLib.Models;

public class CraftItemSourceStore
{
    public Dictionary<uint, List<CraftItemSource>> _characterMaterials;
    private Dictionary<uint, List<CraftItemSource>> _externalSources;

    public CraftItemSourceStore()
    {
        _characterMaterials = new Dictionary<uint, List<CraftItemSource>>();
        _externalSources = new Dictionary<uint, List<CraftItemSource>>();
    }

    public Dictionary<uint, List<CraftItemSource>> ExternalSources => _externalSources;
    public Dictionary<uint, List<CraftItemSource>> CharacterMaterials => _characterMaterials;

    public CraftItemSourceStore AddCharacterSource(uint itemId, uint quantity, bool isHq)
    {
        CharacterMaterials.TryAdd(itemId, new List<CraftItemSource>());
        CharacterMaterials[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        return this;
    }

    public CraftItemSourceStore AddCharacterSource(string itemName, uint quantity, bool isHq)
    {
        if (Service.ExcelCache.ItemsByName.ContainsKey(itemName))
        {
            var itemId = Service.ExcelCache.ItemsByName[itemName];
            CharacterMaterials.TryAdd(itemId, new List<CraftItemSource>());
            CharacterMaterials[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        }
        else
        {
            throw new Exception("Item with name " + itemName + " could not be found");
        }

        return this;
    }

    public CraftItemSourceStore AddExternalSource(string itemName, uint quantity, bool isHq)
    {
        if (Service.ExcelCache.ItemsByName.ContainsKey(itemName))
        {
            var itemId = Service.ExcelCache.ItemsByName[itemName];
            ExternalSources.TryAdd(itemId, new List<CraftItemSource>());
            ExternalSources[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        }
        else
        {
            throw new Exception("Item with name " + itemName + " could not be found");
        }

        return this;
    }
}