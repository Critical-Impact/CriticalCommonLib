using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CriticalCommonLib.MarketBoard
{
    public interface IMarketCache : IDisposable
    {
        void LoadExistingCache();
        void ClearCache();
        void SaveCache(bool forceSave = false);
        MarketPricing? GetPricing(uint itemId, uint worldId, bool forceCheck);
        MarketCachePricingResult GetPricing(uint itemId, uint worldId, bool ignoreCache, bool forceCheck, out MarketPricing? pricing);
        List<MarketPricing> GetPricing(uint itemId, List<uint> worldIds, bool forceCheck);
        List<MarketPricing> GetPricing(uint itemId, bool forceCheck);

        ConcurrentDictionary<(uint, uint), MarketPricing> CachedPricing { get; }
        /// <summary>
        ///
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="worldId"></param>
        /// <returns>Was the request successful</returns>
        bool RequestCheck(uint itemId, uint worldId, bool forceCheck);
        void RequestCheck(List<uint> itemIds, List<uint> worldIds, bool forceCheck);
        void RequestCheck(List<uint> itemIds, uint worldId, bool forceCheck);
        void RequestCheck(uint itemId, List<uint> worldIDs, bool forceCheck);
    }

    public enum MarketCachePricingResult
    {
        Untradable,
        Queued,
        AlreadyQueued,
        Successful,
        NoPricing,
        Disabled
    }
}