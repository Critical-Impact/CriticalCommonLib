using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.MarketBoard;

namespace CriticalCommonLib.Crafting;

public class CraftPricer
{
    private readonly IMarketCache _marketCache;

    public CraftPricer(IMarketCache marketCache)
    {
        this._marketCache = marketCache;
    }
    
    public List<CraftPriceSource> GetItemPricing(MarketPricing marketPricing)
    {
        if (marketPricing.listings == null || marketPricing.listings.Length == 0)
        {
            return new List<CraftPriceSource>();
        }

        var prices = new List<CraftPriceSource>();
        foreach (var listing in marketPricing.listings)
        {
            var craftPriceSource = new CraftPriceSource(marketPricing.ItemId, (uint)listing.quantity, listing.hq, (uint)listing.pricePerUnit, marketPricing.WorldId );
            prices.Add(craftPriceSource);
        }

        return prices;
    }

    public List<CraftPriceSource> GetItemPricing(uint itemId, uint worldId)
    {
        var marketPricing = this._marketCache.GetPricing(itemId, worldId, false);
        if (marketPricing != null)
        {
            return this.GetItemPricing(marketPricing);
        }

        return new List<CraftPriceSource>();
    }

    public List<CraftPriceSource> GetItemPricing(uint itemId, List<uint> worldIds, bool requestPricing = false)
    {
        if (requestPricing)
        {
            this._marketCache.RequestCheck(itemId, worldIds, false);
        }
        return worldIds.SelectMany(c => this.GetItemPricing(itemId, c)).ToList();
    }

    public List<CraftPriceSource> GetItemPricing(List<CraftItem> craftItems, List<uint> worldIds, bool requestPricing = false)
    {
        if (requestPricing)
        {
            this._marketCache.RequestCheck(craftItems.Select(c => c.ItemId).ToList(), worldIds, false);
        }
        return worldIds.SelectMany(c =>
        {
            return craftItems.SelectMany(craftItem =>
            {
                return worldIds.SelectMany(d => this.GetItemPricing(craftItem.ItemId, d)).OrderBy(d => d.UnitPrice).ToList();
            });

        }).ToList();
    }

    public List<CraftPriceSource> GetItemPricing(List<uint> itemIds, List<uint> worldIds, bool requestPricing = false)
    {
        if (requestPricing)
        {
            this._marketCache.RequestCheck(itemIds, worldIds, false);
        }
        return worldIds.SelectMany(c =>
        {
            return itemIds.SelectMany(itemId =>
            {
                return worldIds.SelectMany(d => this.GetItemPricing(itemId, d)).OrderBy(d => d.UnitPrice).ToList();
            });
        }).ToList();
    }
    
    public Dictionary<uint,List<CraftPriceSource>> GetItemPricingDictionary(List<uint> itemIds, List<uint> worldIds, bool requestPricing = false)
    {
        if (requestPricing)
        {
            this._marketCache.RequestCheck(itemIds, worldIds, false);
        }

        var pricingDict = new Dictionary<uint, List<CraftPriceSource>>();
        foreach (var itemId in itemIds)
        {
            var pricing = this.GetItemPricing(itemId, worldIds, requestPricing);
            foreach (var priceSource in pricing)
            {
                priceSource.Reset();
                pricingDict.TryAdd(itemId, new List<CraftPriceSource>());
                pricingDict[itemId].Add(priceSource);
            }
        }

        return pricingDict;
    }
    
}