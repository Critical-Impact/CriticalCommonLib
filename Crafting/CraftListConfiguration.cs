using System;
using System.Collections.Generic;
using System.Linq;

namespace CriticalCommonLib.Crafting;

public class CraftListConfiguration
{
    public CraftPricer? CraftPricer { get; }
    public Dictionary<uint, List<CraftItemSource>> CharacterSources { get; set; }
    public Dictionary<uint, List<CraftItemSource>> ExternalSources { get; set; }

    public Dictionary<(uint, bool), CraftItemSource> SpareIngredients { get; set; }
    public Dictionary<uint, List<CraftPriceSource>> PricingSource { get; set; }


    public List<uint>? WorldPreferences { get; set; }

    private readonly Dictionary<uint, List<CraftPriceSource>> _pricingSources;

    /// <summary>
    /// Gets a list of the prices available for an item
    /// </summary>
    /// <param name="itemId">the item ID</param>
    /// <param name="worldOverride">an extra world you want to prefer for this particular lookup only</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<CraftPriceSource> GetItemPricing(uint itemId, uint? worldOverride = null)
    {
        if (this.CraftPricer == null)
        {
            throw new Exception("Tried to get item pricing but no pricer was provided.");
        }
        if (!this._pricingSources.ContainsKey(itemId))
        {
            var worldPreferences = this.WorldPreferences ?? new();
            worldPreferences = worldPreferences.ToList();
            if (worldOverride != null && !worldPreferences.Contains(worldOverride.Value))
            {
                worldPreferences.Insert(0,worldOverride.Value);
            }
            this._pricingSources[itemId] = worldPreferences.SelectMany(c => this.CraftPricer.GetItemPricing(itemId, c)).ToList();
        }
        return this._pricingSources[itemId];

    }

    public CraftListConfiguration(Dictionary<uint, List<CraftItemSource>>? characterSources = null, Dictionary<uint, List<CraftItemSource>>? externalSources = null, Dictionary<uint, List<CraftPriceSource>>? pricingSource = null, CraftPricer? craftPricer = null)
    {
        this.CraftPricer = craftPricer;
        this._pricingSources = new();
        if (characterSources != null)
        {
            this.CharacterSources = characterSources;
        }
        else
        {
            this.CharacterSources = new();
        }

        if (externalSources != null)
        {
            this.ExternalSources = externalSources;
        }
        else
        {
            this.ExternalSources = new();
        }

        if (pricingSource != null)
        {
            this.PricingSource = pricingSource;
        }
        else
        {
            this.PricingSource = new();
        }

        this.SpareIngredients = new();
    }

    public CraftListConfiguration AddCharacterSource(uint itemId, uint quantity, bool isHq)
    {
        this.CharacterSources.TryAdd(itemId, new List<CraftItemSource>());
        this.CharacterSources[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        return this;
    }

    public CraftListConfiguration AddExternalSource(uint itemId, uint quantity, bool isHq)
    {
        this.ExternalSources.TryAdd(itemId, new List<CraftItemSource>());
        this.ExternalSources[itemId].Add(new CraftItemSource(itemId, quantity, isHq));
        return this;
    }

    public CraftListConfiguration AddSpareIngredient(uint itemId, uint quantity, bool isHq)
    {
        if (!this.SpareIngredients.TryAdd((itemId, isHq), new CraftItemSource(itemId, quantity, isHq)))
        {
            this.SpareIngredients[(itemId, isHq)].Quantity += quantity;
        }
        return this;
    }


}