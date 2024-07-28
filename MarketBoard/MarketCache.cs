using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CriticalCommonLib.Services.Mediator;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.MarketBoard
{
    using Dalamud.Plugin;

    public class MarketCache : IMarketCache
    {
        private IUniversalis _universalis;
        
        private readonly MediatorService? _mediator;
        private ConcurrentDictionary<(uint,uint), byte> requestedItems = new ConcurrentDictionary<(uint,uint), byte>();
        private Dictionary<(uint, uint), MarketPricing> _marketBoardCache = new Dictionary<(uint, uint), MarketPricing>();
        private readonly Stopwatch AutomaticSaveTimer = new();
        private readonly Stopwatch AutomaticCheckTimer = new();
        private string? _cacheStorageLocation;

        private int _automaticCheckTime = 300;
        private int _automaticSaveTime = 120;
        private int _cacheTimeHours = 12;
        public bool _cacheAutoRetrieve;

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
        
        public MarketCache(IUniversalis universalis, MediatorService? mediator, IDalamudPluginInterface pluginInterfaceService)
        {
            _universalis = universalis;
            _mediator = mediator;
            _cacheStorageLocation = Path.Join(pluginInterfaceService.ConfigDirectory.FullName, "market_cache.csv");
            LoadExistingCache();
            _universalis.ItemPriceRetrieved += UniversalisOnItemPriceRetrieved;
        }

        public MarketCache(IUniversalis universalis, MediatorService? mediator, string cacheStorageLocation, bool loadExistingCache = true)
        {
            _universalis = universalis;
            _mediator = mediator;
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
                SaveCacheFile();
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
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }

        private void UniversalisOnItemPriceRetrieved(uint itemId, uint worldId, MarketPricing marketPricing)
        {
            UpdateEntry(itemId, worldId, marketPricing);
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
                if (cacheFile.Exists)
                {
                    var loadedCache = LoadCsv<MarketPricing>(cacheFile.FullName, "Market Cache");
                    foreach (var item in loadedCache)
                    {
                        _marketBoardCache[(item.ItemId, item.WorldId)] = item;
                    }
                }
            }
            catch (Exception e)
            {
                Service.Log.Error("Error while parsing saved universalis data, " + e.Message);
            }
        }
        
        private List<T> LoadCsv<T>(string fileName, string title) where T : ICsv, new()
        {
            try
            {
                var lines = CsvLoader.LoadCsv<T>(fileName, out var failedLines, Service.ExcelCache.GameData, Service.ExcelCache.GameData.Options.DefaultExcelLanguage);
                if (failedLines.Count != 0)
                {
                    foreach (var failedLine in failedLines)
                    {
                        Service.Log.Error("Failed to load line from " + title + ": " + failedLine);
                    }
                }
                return lines;
            }
            catch (Exception e)
            {
                Service.Log.Error("Failed to load " + title);
                Service.Log.Error(e.Message);
            }

            return new List<T>();
        }
        
        public void ClearCache()
        {
            _marketBoardCache = new Dictionary<(uint,uint), MarketPricing>();
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

            Service.Log.Verbose("Saving MarketCache");
            SaveCacheFile();

            AutomaticSaveTimer.Restart();
        }

        private void SaveCacheFile()
        {
            if (_cacheStorageLocation != null)
            {
                try
                {
                    CsvLoader.ToCsvRaw(_marketBoardCache.Values.ToList(), _cacheStorageLocation);
                }
                catch (Exception e)
                {
                    Service.Log.Debug(e, messageTemplate: "Failed to save MarketCache.");
                }
            }
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

            Service.Log.Verbose("Checking Cache...");
            foreach (var item in _marketBoardCache)
            {
                var now = DateTime.Now;
                var diff = now - item.Value.LastUpdate;
                if (diff.TotalHours > CacheTimeHours)
                {
                    GetPricing(item.Key.Item1, item.Key.Item2, true, false);
                }
            }

            SaveCache();
            AutomaticCheckTimer.Restart();
        }

        public MarketPricing? GetPricing(uint itemId, uint worldId, bool forceCheck)
        {
            return GetPricing(itemId, worldId, false, forceCheck);
        }

        public List<MarketPricing> GetPricing(uint itemId, List<uint> worldIds, bool forceCheck)
        {
            var keys = worldIds.Select(c => (itemId, c)).ToList();
            var prices = new List<MarketPricing>();
            foreach (var key in keys)
            {
                if (_marketBoardCache.ContainsKey(key))
                {
                    prices.Add(_marketBoardCache[key]);
                }
            }

            return prices;

        }

        private List<uint>? _worldIds;

        public List<MarketPricing> GetPricing(uint itemId, bool forceCheck)
        {
            if (_worldIds == null)
            {
                _worldIds = Service.ExcelCache.GetWorldSheet().Where(c => c.IsPublic).Select(c => c.RowId).ToList();
            }

            return GetPricing(itemId, _worldIds, forceCheck);
        }

        internal MarketPricing? GetPricing(uint itemId, uint worldId, bool ignoreCache, bool forceCheck)
        {
            if (!ignoreCache && !forceCheck)
            {
                CheckCache();
            }

            if (Service.ExcelCache.GetItemExSheet().GetRow(itemId)?.IsUntradable ?? true)
            {
                return new MarketPricing();
            }

            if (!ignoreCache && _marketBoardCache.ContainsKey((itemId,worldId)) && !forceCheck)
            {
                return _marketBoardCache[(itemId,worldId)];
            }

            if (!CacheAutoRetrieve && !forceCheck)
            {
                return new MarketPricing();
            }

            if (!requestedItems.ContainsKey((itemId, worldId)) || forceCheck)
            {
                requestedItems.TryAdd((itemId, worldId), default);
                _universalis.QueuePriceCheck(itemId, worldId);
            }

            return null;
        }

        public bool RequestCheck(uint itemId, uint worldId, bool forceCheck)
        {
            //Allow the check if a force check is requested, or if we haven't requested in the item since we last retrieved it
            if(!requestedItems.ContainsKey((itemId, worldId)) || forceCheck)
            {
                if (!_marketBoardCache.ContainsKey((itemId, worldId)) || _marketBoardCache[(itemId, worldId)].listings == null || forceCheck)
                {
                    requestedItems.TryAdd((itemId, worldId), default);
                    _universalis.QueuePriceCheck(itemId, worldId);
                }
                return true;

            }
            return false;
        }

        public void RequestCheck(List<uint> itemIds, List<uint> worldIds, bool forceCheck)
        {
            foreach (var itemId in itemIds)
            {
                foreach (var worldId in worldIds)
                {
                    RequestCheck(itemId, worldId, forceCheck);
                }
            }
        }

        public void RequestCheck(List<uint> itemIds, uint worldId, bool forceCheck)
        {
            foreach (var itemId in itemIds)
            {
                RequestCheck(itemId, worldId, forceCheck);
            }
        }

        public void RequestCheck(uint itemId, List<uint> worldIds, bool forceCheck)
        {
            foreach (var worldId in worldIds)
            {
                RequestCheck(itemId, worldId, forceCheck);
            }
        }

        internal void UpdateEntry(uint itemId, uint worldId, MarketPricing pricingResponse)
        {
            _marketBoardCache[(itemId, worldId)] = pricingResponse;
            requestedItems.TryRemove((itemId, worldId), out _);
            SaveCache();
            _mediator?.Publish(new MarketCacheUpdatedMessage(itemId, worldId));
        }
    }
}
