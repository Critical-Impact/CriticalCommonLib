using System;
using System.Collections.Generic;

namespace CriticalCommonLib.MarketBoard
{
    public interface IMarketCache : IDisposable
    {
        int AutomaticCheckTime { get; set; }
        int AutomaticSaveTime { get; set; }
        int CacheTimeHours { get; set; }
        bool CacheAutoRetrieve { get; set; }
        void StartAutomaticCheckTimer();
        void RestartAutomaticCheckTimer();
        void StopAutomaticCheckTimer();
        void LoadExistingCache();
        void ClearCache();
        void SaveCache(bool forceSave = false);
        MarketPricing? GetPricing(uint itemId, uint worldId, bool forceCheck);
        List<MarketPricing> GetPricing(uint itemId, List<uint> worldIds, bool forceCheck);
        List<MarketPricing> GetPricing(uint itemId, bool forceCheck);
        void RequestCheck(uint itemID, uint worldId);
    }
}