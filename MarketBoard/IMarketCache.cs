using System;

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
        PricingResponse? GetPricing(uint itemID, bool forceCheck);
        void RequestCheck(uint itemID);
    }
}