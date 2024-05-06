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
}