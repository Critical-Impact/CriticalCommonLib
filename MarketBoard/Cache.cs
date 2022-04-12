using CriticalCommonLib.Services;
using Dalamud.Logging;
using CriticalCommonLib.Resolvers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using CriticalCommonLib;
using CriticalCommonLib.MarketBoard;

namespace CriticalCommonLib.MarketBoard
{
    public class CacheEntry
    {
        public uint ItemId { get; set; }
        public PricingResponse Data { get; set; } = null!;
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }

    public static class Cache
    {
        private static ConcurrentDictionary<uint, byte> requestedItems = new ConcurrentDictionary<uint, byte>();
        private static Dictionary<uint, CacheEntry> _marketBoardCache = new Dictionary<uint, CacheEntry>();
        private static readonly Stopwatch AutomaticSaveTimer = new();
        private static readonly Stopwatch AutomaticCheckTimer = new();
        private static bool IsLoaded = false;
        private static string? _cacheStorageLocation;

        private static int _automaticCheckTime = 300;
        private static int _automaticSaveTime = 120;
        private static int _cacheTimeHours = 12;
        public static bool _cacheAutoRetrieve = false;

        public static int AutomaticCheckTime
        {
            get => _automaticCheckTime;
            set
            {
                _automaticCheckTime = value;
                RestartAutomaticCheckTimer();
            }
        }

        public static int AutomaticSaveTime
        {
            get => _automaticSaveTime;
            set => _automaticSaveTime = value;
        }

        public static int CacheTimeHours
        {
            get => _cacheTimeHours;
            set => _cacheTimeHours = value;
        }

        public static bool CacheAutoRetrieve
        {
            get => _cacheAutoRetrieve;
            set => _cacheAutoRetrieve = value;
        }

        public static void StartAutomaticCheckTimer()
        {
            if (!AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Start();
            }
        }

        public static void RestartAutomaticCheckTimer()
        {
            if (AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Restart();
            }
        }

        public static void StopAutomaticCheckTimer()
        {
            if (AutomaticCheckTimer.IsRunning)
            {
                AutomaticCheckTimer.Stop();
            }
        }

        public static void Initalise(string cacheStorageLocation, bool loadExistingCache = true)
        {
            if (IsLoaded)
            {
                return;
            }

            _cacheStorageLocation = cacheStorageLocation;
            if (loadExistingCache)
            {
                LoadExistingCache();
            }
            Universalis.ItemPriceRetrieved += UniversalisOnItemPriceRetrieved;
            IsLoaded = true;
        }

        public static void Dispose()
        {
            StopAutomaticCheckTimer();
            Universalis.ItemPriceRetrieved -= UniversalisOnItemPriceRetrieved;
            IsLoaded = false;
        }

        private static void UniversalisOnItemPriceRetrieved(uint itemId, PricingResponse response)
        {
            UpdateEntry(itemId, response);
        }

        public static void LoadExistingCache()
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
                PluginLog.Verbose("Error while parsing saved universalis data, " + e.Message);
            }
        }
        
        public static void ClearCache()
        {
            _marketBoardCache = new Dictionary<uint, CacheEntry>();
        }

        public static void SaveCache(bool forceSave = false)
        {
            if (_cacheStorageLocation == null)
            {
                throw new Exception("Cache not initialised yet.");
            }
            if (!forceSave && (AutomaticSaveTimer.IsRunning && AutomaticSaveTimer.Elapsed < TimeSpan.FromSeconds(AutomaticSaveTime) || !IsLoaded))
            {
                return;
            }

            if (!AutomaticSaveTimer.IsRunning)
            {
                AutomaticSaveTimer.Start();
            }

            var cacheFile = new FileInfo(_cacheStorageLocation);
            PluginLog.Verbose("Saving Universalis Cache");
            File.WriteAllText(cacheFile.FullName, JsonConvert.SerializeObject((object)_marketBoardCache, Formatting.None, new JsonSerializerSettings()
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                ContractResolver = new MinifyResolver()
            }));

            AutomaticSaveTimer.Restart();
        }

        internal static void CheckCache()
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

        public static PricingResponse? GetPricing(uint itemID, bool forceCheck)
        {
            return GetPricing(itemID, false, forceCheck);
        }

        internal static PricingResponse? GetPricing(uint itemID, bool ignoreCache, bool forceCheck)
        {
            if (!IsLoaded)
            {
                return null;
            }
            if (!ignoreCache && !forceCheck)
            {
                CheckCache();
            }
            

            if (ExcelCache.GetItem(itemID)?.IsUntradable ?? true)
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
                Universalis.QueuePriceCheck(itemID);
            }

            return null;
        }

        public static void RequestCheck(uint itemID)
        {
            if (Service.ClientState.IsLoggedIn &&
                Service.ClientState.LocalPlayer != null && IsLoaded)
            {
                if (!requestedItems.ContainsKey(itemID) && !_marketBoardCache.ContainsKey(itemID))
                {
                    requestedItems.TryAdd(itemID, default);
                    Universalis.QueuePriceCheck(itemID);
                }
            }
        }

        internal static void UpdateEntry(uint itemId, PricingResponse pricingResponse)
        {
            var entry = new CacheEntry();
            entry.ItemId = itemId;
            entry.Data = pricingResponse;
            _marketBoardCache[itemId] = entry;
            SaveCache();
        }
    }
}
