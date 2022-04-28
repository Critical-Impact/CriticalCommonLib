using CriticalCommonLib.Models;
using Dalamud.Logging;
using Dispatch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Extensions;
#pragma warning disable 8618

namespace CriticalCommonLib.MarketBoard
{

    public class Universalis
    {
        private static SerialQueue _apiRequestQueue = new SerialQueue();
        private static List<IDisposable> _disposables = new List<IDisposable>();
        private static Subject<uint> _queuedItems = new Subject<uint>();
        private static bool _tooManyRequests = false;
        private static bool _initialised = false;
        private static DateTime? _nextRequestTime;
        
        private static readonly int MaxBufferCount = 50;
        private static readonly int BufferInterval = 1;
        private static int _queuedCount = 0;

        public delegate void ItemPriceRetrievedDelegate(uint itemId, PricingResponse response);

        public static event ItemPriceRetrievedDelegate? ItemPriceRetrieved;
        

        public static void Dispose()
        {
            _apiRequestQueue.Dispose();
            _queuedItems.Dispose();
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
        
        public static PricingResponse? RetrieveMarketBoardPrice(InventoryItem item)
        {
            if (!item.CanBeBought)
            {
                return new PricingResponse();
            }
            return RetrieveMarketBoardPrice(item.ItemId);
        }

        public static void Initalise()
        {
            PluginLog.Verbose("Setting up universalis buffer.");
            _initialised = true;
            var stepInterval = _queuedItems
                .Buffer(TimeSpan.FromSeconds(BufferInterval), MaxBufferCount)
                .Select(o => o.Distinct())
                .StepInterval(TimeSpan.FromSeconds(BufferInterval));
            
            _disposables.Add(_queuedItems.Subscribe(_ => _queuedCount++));
            
            _disposables.Add(stepInterval
                .Subscribe(x =>
                {
                    var itemIds = x.ToList();
                    _queuedCount -= itemIds.Count();
                    if (_tooManyRequests && _nextRequestTime != null && _nextRequestTime.Value <= DateTime.Now)
                    {
                        _tooManyRequests = false;
                        _nextRequestTime = null;
                    }
                    if (itemIds.Any())
                    {
                        RetrieveMarketBoardPrices(itemIds);
                    }
                }));
        }

        public static void QueuePriceCheck(uint itemId)
        {
            if (!_initialised)
            {
                Initalise();
            }
            _queuedItems.OnNext(itemId);
        }

        public static int QueuedCount
        {
            get
            {
                return _queuedCount;
            }
        }

        public static void RetrieveMarketBoardPrices(IEnumerable<uint> itemIds)
        {
            if (Service.ClientState.IsLoggedIn && Service.ClientState.LocalPlayer != null)
            {
                if (_tooManyRequests)
                {
                    PluginLog.Debug("Too many requests, readding items.");
                    foreach (var itemId in itemIds)
                    {
                        _queuedItems.OnNext(itemId);
                    }

                    return;
                }

                if (Service.ClientState.LocalPlayer.CurrentWorld.GameData == null)
                {
                    return;
                }
                string datacenter = Service.ClientState.LocalPlayer.CurrentWorld.GameData.Name.RawString;
                if (itemIds.Count() == 1)
                {
                    var itemId = itemIds.First();
                    string url = $"https://universalis.app/api/{datacenter}/{itemId}?listings=0&entries=0";
                        PluginLog.LogVerbose(url);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.AutomaticDecompression = DecompressionMethods.GZip;

                        PricingResponse? listing = new PricingResponse();

                        using (WebResponse response = request.GetResponse())
                        {
                            try
                            {
                                HttpWebResponse webresponse = (HttpWebResponse) response;

                                if (webresponse.StatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    PluginLog.Warning("Universalis: too many requests!");
                                    // sleep for 1 minute if too many requests
                                    Thread.Sleep(60000);

                                    request = (HttpWebRequest) WebRequest.Create(url);
                                    webresponse = (HttpWebResponse) request.GetResponse();
                                }
                                else
                                {
                                    PluginLog.LogVerbose($"Universalis: {webresponse.StatusCode}");
                                }

                                var reader = new StreamReader(webresponse.GetResponseStream());
                                var value = reader.ReadToEnd();
                                PluginLog.LogVerbose(value);
                                listing = JsonConvert.DeserializeObject<PricingResponse>(value);

                                if (listing != null)
                                {
                                    listing.loaded = true;
                                    ItemPriceRetrieved?.Invoke(itemId, listing);
                                }
                                else
                                {
                                    PluginLog.Error("Universalis: Failed to parse universalis json data");
                                }
                            }
                            catch (Exception ex)
                            {
                                PluginLog.Debug(ex.ToString());
                            }
                        }
                }
                else
                {
                    var itemIdsString = String.Join(",", itemIds.Select(c => c.ToString()).ToArray());
                    PluginLog.Verbose($"Sending request for items {itemIdsString} to universalis API.");
                    string url = $"https://universalis.app/api/v2/{datacenter}/{itemIdsString}?listings=0&entries=0";
                    PluginLog.LogVerbose(url);

                    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                    request.AutomaticDecompression = DecompressionMethods.GZip;

                    MultiRequest? multiRequest = new MultiRequest();
                    try
                    {
                        using (WebResponse response = request.GetResponse())
                        {

                            HttpWebResponse webresponse = (HttpWebResponse) response;

                            if (webresponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                PluginLog.Warning("Universalis: too many requests!");
                                _nextRequestTime = DateTime.Now.AddMinutes(1);
                                _tooManyRequests = true;
                            }
                            else
                            {
                                PluginLog.LogVerbose($"Universalis: {webresponse.StatusCode}");
                            }

                            var reader = new StreamReader(webresponse.GetResponseStream());
                            var value = reader.ReadToEnd();
                            PluginLog.LogVerbose(value);
                            multiRequest = JsonConvert.DeserializeObject<MultiRequest>(value);


                            if (multiRequest != null)
                            {
                                foreach (var item in multiRequest.items)
                                {
                                    item.Value.loaded = true;
                                    ItemPriceRetrieved?.Invoke(item.Value.itemID, item.Value);
                                }
                            }
                            else
                            {
                                PluginLog.Verbose("Universalis: could not parse multi request json data");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Debug(ex.ToString());
                    }
                }
            }
        }

        public static PricingResponse? RetrieveMarketBoardPrice(uint itemId)
        {
            if (Service.ClientState.IsLoggedIn && Service.ClientState.LocalPlayer != null)
            {
                if (Service.ClientState.LocalPlayer.CurrentWorld.GameData == null)
                {
                    return new PricingResponse();
                }
                string datacenter = Service.ClientState.LocalPlayer.CurrentWorld.GameData.Name.RawString;

                var dispatch = _apiRequestQueue.DispatchAsync(() =>
                    {
                        string url = $"https://universalis.app/api/{datacenter}/{itemId}?listings=0&entries=0";
                        PluginLog.LogVerbose(url);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.AutomaticDecompression = DecompressionMethods.GZip;

                        PricingResponse? listing = new PricingResponse();

                        using (WebResponse response = request.GetResponse())
                        {
                            try
                            {
                                HttpWebResponse webresponse = (HttpWebResponse)response;

                                if (webresponse.StatusCode == HttpStatusCode.TooManyRequests)
                                {
                                    PluginLog.Warning("Universalis: too many requests!");
                                // sleep for 1 minute if too many requests
                                Thread.Sleep(60000);

                                    request = (HttpWebRequest)WebRequest.Create(url);
                                    webresponse = (HttpWebResponse)request.GetResponse();
                                }
                                else
                                {
                                    PluginLog.LogVerbose($"Universalis: {webresponse.StatusCode}");
                                }

                                var reader = new StreamReader(webresponse.GetResponseStream());
                                var value = reader.ReadToEnd();
                                PluginLog.LogVerbose(value);
                                listing = JsonConvert.DeserializeObject<PricingResponse>(value);


                                if (listing != null)
                                {
                                    listing.loaded = true;
                                    ItemPriceRetrieved?.Invoke(itemId, listing);
                                }
                                else
                                {
                                    PluginLog.Verbose("Universalis: could not parse listing data json");
                                }
                                
                                // Simple way to prevent too many requests
                                Thread.Sleep(500);
                            }
                            catch (Exception ex)
                            {
                                PluginLog.Debug(ex.ToString());
                            }
                        }

                    });
                _disposables.Add(dispatch);
            }

            return null;
        }

        private static void CheckQueue()
        {
            Task checkTask = new Task(async () =>
                {
                    await Universalis._apiRequestQueue;

                });
            checkTask.ContinueWith(task =>
            {
                Cache.CheckCache();
            });
        }
    }


    public class MultiRequest
    {
        public string[] itemIDs { internal get; set; }
        public Dictionary<string,PricingResponse> items { internal get; set; }
    }

    public class PricingResponse
    {
        public bool loaded { get; set; } = false;
        
        public uint itemID { internal get; set; }
        
        public float averagePriceNQ { get; set; }
        public float averagePriceHQ { get; set; }
        public float minPriceNQ { get; set; }
        public float minPriceHQ { get; set; }
    }

    public class Stacksizehistogram
    {
        public int _1 { get; set; }
    }

    public class Stacksizehistogramnq
    {
        public int _1 { get; set; }
    }

    public class Stacksizehistogramhq
    {
        public int _1 { get; set; }
    }

    public class Listing
    {
        
        public int lastReviewTime { get; set; }
        
        public int pricePerUnit { get; set; }
        
        public int quantity { get; set; }
        
        public int stainID { get; set; }
        
        public string creatorName { get; set; }
        
        public object creatorID { get; set; }
        
        public bool hq { get; set; }
        
        public bool isCrafted { get; set; }
        
        public object listingID { get; set; }
        
        public object[] materia { get; set; }
        
        public bool onMannequin { get; set; }
        
        public int retainerCity { get; set; }
        
        public string retainerID { get; set; }
        
        public string retainerName { get; set; }
        
        public string sellerID { get; set; }
        
        public int total { get; set; }
    }

    public class Recenthistory
    {
        public bool hq { get; set; }
        public int pricePerUnit { get; set; }
        public int quantity { get; set; }
        public int timestamp { get; set; }
        public string buyerName { get; set; }
        public int total { get; set; }
    }

}
