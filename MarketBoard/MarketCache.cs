using Dalamud.Logging;
using CriticalCommonLib.Resolvers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CriticalCommonLib.MarketBoard
{
    public class CacheEntry
    {
        public uint ItemId { get; set; }
        public PricingResponse Data { get; set; } = null!;
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }

    public class MarketCache : IMarketCache
    {
        private IUniversalis _universalis;
        private ConcurrentDictionary<uint, byte> requestedItems = new ConcurrentDictionary<uint, byte>();
        private Dictionary<uint, CacheEntry> _marketBoardCache = new Dictionary<uint, CacheEntry>();
        private readonly Stopwatch AutomaticSaveTimer = new();
        private readonly Stopwatch AutomaticCheckTimer = new();
        private string? _cacheStorageLocation;

        private int _automaticCheckTime = 300;
        private int _automaticSaveTime = 120;
        private int _cacheTimeHours = 12;
        public bool _cacheAutoRetrieve = false;

        public int AutomaticCheckTime
        {
            get => _automaticCheckTime;
            set
            {
                _automaticCheckTime = value;
                RestartAutomaticCheckTimer();
            }
        }

        public int AutomaticSaveTime
        {
            get => _automaticSaveTime;
            set => _automaticSaveTime = value;
        }

        public int CacheTimeHours
        {
            get => _cacheTimeHours;
            set => _cacheTimeHours = value;
        }

        public bool CacheAutoRetrieve
        {
            get => _cacheAutoRetrieve;
            set => _cacheAutoRetrieve = value;
        }

        public void StartAutomaticCheckTimer()
        {
            if (!AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Start();
            }
        }

        public void RestartAutomaticCheckTimer()
        {
            if (AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Restart();
            }
        }

        public void StopAutomaticCheckTimer()
        {
            if (AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Stop();
            }
        }

        public MarketCache(IUniversalis universalis, string cacheStorageLocation, bool loadExistingCache = true)
        {
            _universalis = universalis;
            _cacheStorageLocation = cacheStorageLocation;
            if (loadExistingCache)
            {
                LoadExistingCache();
            }
            _universalis.ItemPriceRetrieved += UniversalisOnItemPriceRetrieved;
        }
        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                StopAutomaticCheckTimer();
                _universalis.ItemPriceRetrieved -= UniversalisOnItemPriceRetrieved;
            }
            _disposed = true;         
        }
        
        ~MarketCache()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }

        private void UniversalisOnItemPriceRetrieved(uint itemId, PricingResponse response)
        {
            UpdateEntry(itemId, response);
        }

        public void LoadExistingCache()
        {
            if (_cacheStorageLocation == null)
            {
                throw new Exception("Cache not initialised yet.");
            }
            try
            {
                var cacheFile = new FileInfo(_cacheStorageLocation);
                string json = File.ReadAllText(cacheFile.FullName, Encoding.UTF8);
                var oldCache = JsonConvert.DeserializeObject<Dictionary<uint, CacheEntry>>(json);
                if (oldCache != null)
                    foreach (var item in oldCache)
                    {
                        _marketBoardCache[item.Key] = item.Value;
                    }
            }
            catch (Exception e)
            {
                PluginLog.Error("Error while parsing saved universalis data, " + e.Message);
            }
        }
        
        public void ClearCache()
        {
            _marketBoardCache = new Dictionary<uint, CacheEntry>();
        }

        public void SaveCache(bool forceSave = false)
        {
            if (_cacheStorageLocation == null)
            {
                throw new Exception("Cache not initialised yet.");
            }
            if (!forceSave && (AutomaticSaveTimer.IsRunning && AutomaticSaveTimer.Elapsed < TimeSpan.FromSeconds(AutomaticSaveTime)))
            {
                return;
            }

            if (!AutomaticSaveTimer.IsRunning)
            {
                AutomaticSaveTimer.Start();
            }

            var cacheFile = new FileInfo(_cacheStorageLocation);
            PluginLog.Verbose("Saving MarketCache");
            try
            {
                File.WriteAllText(cacheFile.FullName, JsonConvert.SerializeObject((object)_marketBoardCache, Formatting.None, new JsonSerializerSettings()
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                    ContractResolver = new MinifyResolver()
                }));
            }
            catch (Exception e)
            {
                PluginLog.Debug(e, messageTemplate: "Failed to save MarketCache.");
            }

            AutomaticSaveTimer.Restart();
        }

        internal void CheckCache()
        {
            if (AutomaticCheckTimer.IsRunning && AutomaticCheckTimer.Elapsed < TimeSpan.FromSeconds(AutomaticCheckTime))
            {
                return;
            }

            if (!AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Start();
            }

            PluginLog.Verbose("Checking Cache...");
            foreach (var item in _marketBoardCache)
            {
                var now = DateTime.Now;
                var diff = now - item.Value.LastUpdate;
                if (diff.TotalHours > CacheTimeHours)
                {
                    GetPricing(item.Key, true, false);
                }
            }

            SaveCache();
            AutomaticCheckTimer.Restart();
        }

        public PricingResponse? GetPricing(uint itemID, bool forceCheck)
        {
            return GetPricing(itemID, false, forceCheck);
        }

        internal PricingResponse? GetPricing(uint itemID, bool ignoreCache, bool forceCheck)
        {
            if (!ignoreCache && !forceCheck)
            {
                CheckCache();
            }
            

            if (Service.ExcelCache.GetItemExSheet().GetRow(itemID)?.IsUntradable ?? true)
            {
                return new PricingResponse();
            }

            if (!ignoreCache && _marketBoardCache.ContainsKey(itemID) && !forceCheck)
            {
                return _marketBoardCache[itemID].Data;
            }

            if (!CacheAutoRetrieve && !forceCheck)
            {
                return new PricingResponse();
            }

            if (!requestedItems.ContainsKey(itemID) || forceCheck)
            {
                requestedItems.TryAdd(itemID, default);
                _universalis.QueuePriceCheck(itemID);
            }

            return null;
        }

        public void RequestCheck(uint itemID)
        {
            if (Service.ClientState.IsLoggedIn &&
                Service.ClientState.LocalPlayer != null)
            {
                if (!requestedItems.ContainsKey(itemID) && !_marketBoardCache.ContainsKey(itemID))
                {
                    requestedItems.TryAdd(itemID, default);
                    _universalis.QueuePriceCheck(itemID);
                }
            }
        }

        internal void UpdateEntry(uint itemId, PricingResponse pricingResponse)
        {
            var entry = new CacheEntry();
            entry.ItemId = itemId;
            entry.Data = pricingResponse;
            _marketBoardCache[itemId] = entry;
            SaveCache();
        }
    }
}
