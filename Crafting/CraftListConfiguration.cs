using System;
using System.Collections.Generic;
using System.Linq;

namespace CriticalCommonLib.Crafting;

public class CraftListConfiguration
{
    public CraftPricer? CraftPricer { get; }
    public Dictionary<uint, List<CraftItemSource>> CharacterSources { get; set; }
    public Dictionary<uint, List<CraftItemSource>> ExternalSources { get; set; }
    public Dictionary<uint, List<CraftPriceSource>> PricingSource { get; set; }
    
    public List<uint>? WorldPreferences { get; set; }

    private Dictionary<uint, List<CraftPriceSource>> _pricingSources;
    
    /// <summary>
    /// Gets a list of the prices available for an item
    /// </summary>
    /// <param name="itemId">the item ID</param>
    /// <param name="worldOverride">an extra world you want to prefer for this particular lookup only</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<CraftPriceSource> GetItemPricing(uint itemId, uint? worldOverride = null)
    {
        if (CraftPricer == null)
        {
            throw new Exception("Tried to get item pricing but no pricer was provided.");
        }
        if (!_pricingSources.ContainsKey(itemId))
        {
            var worldPreferences = WorldPreferences ?? new();
            worldPreferences = worldPreferences.ToList();
            if (worldOverride != null && !worldPreferences.Contains(worldOverride.Value))
            {
                worldPreferences.Insert(0,worldOverride.Value);
            }
            _pricingSources[itemId] = worldPreferences.SelectMany(c => CraftPricer.GetItemPricing(itemId, c)).ToList();
        }
        return _pricingSources[itemId];

    }

    public CraftListConfiguration(Dictionary<uint, List<CraftItemSource>>? characterSources = null, Dictionary<uint, List<CraftItemSource>>? externalSources = null, Dictionary<uint, List<CraftPriceSource>>? pricingSource = null, CraftPricer? craftPricer = null)
    {
        CraftPricer = craftPricer;
        _pricingSources = new();
        if (characterSources != null)
        {
            CharacterSources = characterSources;
        }
        else
        {
            CharacterSources = new();
        }

        if (externalSources != null)
        {
            ExternalSources = externalSources;
        }
        else
        {
            ExternalSources = new();
        }

        if (pricingSource != null)
        {
            PricingSource = pricingSource;
        }
        else
        {
            PricingSource = new();
        }
    }
    
    public CraftListConfiguration AddCharacterSource(uint itemId, uint quantity, bool isHq)
    {
        CharacterSources.TryAdd(itemId, new List<CraftItemSource>());
        CharacterSources[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        return this;
    }

    public CraftListConfiguration AddCharacterSource(string itemName, uint quantity, bool isHq)
    {
        if (Service.ExcelCache.ItemsByName.ContainsKey(itemName))
        {
            var itemId = Service.ExcelCache.ItemsByName[itemName];
            CharacterSources.TryAdd(itemId, new List<CraftItemSource>());
            CharacterSources[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        }
        else
        {
            throw new Exception("Item with name " + itemName + " could not be found");
        }

        return this;
    }

    public CraftListConfiguration AddExternalSource(string itemName, uint quantity, bool isHq)
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