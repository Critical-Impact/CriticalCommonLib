using CriticalCommonLib.Models;
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
using CriticalCommonLib.Extensions;
#pragma warning disable SYSLIB0014
#pragma warning disable 8618

namespace CriticalCommonLib.MarketBoard
{

    public class Universalis : IUniversalis
    {
        private SerialQueue _apiRequestQueue = new SerialQueue();
        private List<IDisposable> _disposables = new List<IDisposable>();
        private Subject<(uint, uint)> _queuedItems = new Subject<(uint, uint)>();
        private Dictionary<uint, string> _worldNames = new();
        private bool _tooManyRequests;
        private bool _initialised;
        private DateTime? _nextRequestTime;
        
        private readonly int MaxBufferCount = 50;
        private readonly int BufferInterval = 1;
        private int _queuedCount;
        private int _saleHistoryLimit = 7;

        public delegate void ItemPriceRetrievedDelegate(uint itemId, uint worldId, MarketPricing response);

        public event ItemPriceRetrievedDelegate? ItemPriceRetrieved;

        public MarketPricing? RetrieveMarketBoardPrice(InventoryItem item, uint worldId)
        {
            if (!item.Item.ObtainedGil)
            {
                return new MarketPricing();
            }
            return RetrieveMarketBoardPrice(item.ItemId, worldId);
        }

        public void SetSaleHistoryLimit(int limit)
        {
            _saleHistoryLimit = limit;
        }

        public void Initialise()
        {
            Service.Log.Verbose("Setting up universalis buffer.");
            _initialised = true;
            var stepInterval = _queuedItems
                .Buffer(TimeSpan.FromSeconds(BufferInterval), MaxBufferCount)
                .StepInterval(TimeSpan.FromSeconds(BufferInterval));
            
            _disposables.Add(_queuedItems.Subscribe(_ => _queuedCount++));
            
            _disposables.Add(stepInterval
                .Subscribe(x =>
                {
                    var itemIds = x.ToList();
                    var queuedCount = itemIds.Count();
                    _queuedCount -= queuedCount;
                    if (_tooManyRequests && _nextRequestTime != null && _nextRequestTime.Value <= DateTime.Now)
                    {
                        _tooManyRequests = false;
                        _nextRequestTime = null;
                    }
                    itemIds = itemIds.Distinct().ToList();
                    if (itemIds.Any())
                    {
                        var byWorld = itemIds.GroupBy(c => c.Item2);
                        foreach (var itemWorldId in byWorld)
                        {
                            RetrieveMarketBoardPrices(itemWorldId.Select(c => c.Item1).ToList(), itemWorldId.Key);
                        }
                    }
                }));
        }

        public void QueuePriceCheck(uint itemId, uint worldId)
        {
            if (!_initialised)
            {
                Initialise();
            }

            if (itemId != 0)
            {
                _queuedItems.OnNext((itemId, worldId));
            }
        }

        public DateTime? LastFailure { get; private set; }
        public bool TooManyRequests => _tooManyRequests;

        public int QueuedCount
        {
            get
            {
                return _queuedCount;
            }
        }

        public int SaleHistoryLimit => _saleHistoryLimit;

        public void RetrieveMarketBoardPrices(IEnumerable<uint> itemIds, uint worldId)
        {
            if (_tooManyRequests)
            {
                Service.Log.Debug("Too many requests, readding items.");
                foreach (var itemId in itemIds)
                {
                    _queuedItems.OnNext((itemId, worldId));
                }

                return;
            }

            string worldName;
            if (!_worldNames.ContainsKey(worldId))
            {
                var world = Service.ExcelCache.GetWorldSheet().GetRow(worldId);
                if (world == null)
                {
                    return;
                }

                _worldNames[worldId] = world.Name.RawString;
            }
            worldName = _worldNames[worldId];
            
            if (itemIds.Count() == 1)
            {
                var dispatch = _apiRequestQueue.DispatchAsync(() =>
                {
                    var itemId = itemIds.First();
                    string url = $"https://universalis.app/api/{worldName}/{itemId}?listings=20&entries=20";

                    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                    request.AutomaticDecompression = DecompressionMethods.GZip;

                    try
                    {
                        using (WebResponse response = request.GetResponse())
                        {
                            HttpWebResponse webresponse = (HttpWebResponse) response;

                            if (webresponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                Service.Log.Warning("Universalis: too many requests!");
                                _tooManyRequests = true;
                                // sleep for 1 minute if too many requests
                                Thread.Sleep(60000);

                                request = (HttpWebRequest) WebRequest.Create(url);
                                webresponse = (HttpWebResponse) request.GetResponse();
                            }
                            _tooManyRequests = false;

                            var reader = new StreamReader(webresponse.GetResponseStream());
                            var value = reader.ReadToEnd();
                            PricingAPIResponse? apiListing = JsonConvert.DeserializeObject<PricingAPIResponse>(value);

                            if (apiListing != null)
                            {
                                var listing = MarketPricing.FromApi(apiListing, worldId, SaleHistoryLimit);
                                Service.Framework.RunOnFrameworkThread(() =>
                                    ItemPriceRetrieved?.Invoke(apiListing.itemID, worldId, listing));
                            }
                            else
                            {
                                LastFailure = DateTime.Now;
                                Service.Log.Error("Universalis: Failed to parse universalis json data");
                            }
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Debug(ex.ToString());
                    }
                });
                _disposables.Add(dispatch);
            }
            else
            {
                var dispatch = _apiRequestQueue.DispatchAsync(() =>
                {
                    var itemIdsString = String.Join(",", itemIds.Select(c => c.ToString()).ToArray());
                    Service.Log.Verbose($"Sending request for items {itemIdsString} to universalis API.");
                    string url =
                        $"https://universalis.app/api/v2/{worldName}/{itemIdsString}?listings=20&entries=20";

                    HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                    request.AutomaticDecompression = DecompressionMethods.GZip;

                    try
                    {
                        using (WebResponse response = request.GetResponse())
                        {

                            HttpWebResponse webresponse = (HttpWebResponse) response;

                            if (webresponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                Service.Log.Warning("Universalis: too many requests!");
                                _nextRequestTime = DateTime.Now.AddMinutes(1);
                                _tooManyRequests = true;
                            }

                            var reader = new StreamReader(webresponse.GetResponseStream());
                            var value = reader.ReadToEnd();
                            MultiRequest? multiRequest = JsonConvert.DeserializeObject<MultiRequest>(value);


                            if (multiRequest != null)
                            {
                                foreach (var item in multiRequest.items)
                                {
                                    var listing = MarketPricing.FromApi(item.Value, worldId, SaleHistoryLimit);
                                    Service.Framework.RunOnFrameworkThread(() =>
                                        ItemPriceRetrieved?.Invoke(item.Value.itemID, worldId, listing));
                                }
                            }
                            else
                            {
                                LastFailure = DateTime.Now;   
                                Service.Log.Verbose("Universalis: could not parse multi request json data");
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Debug(ex.ToString());
                    }
                });
                _disposables.Add(dispatch);
            }
        }

        public MarketPricing? RetrieveMarketBoardPrice(uint itemId, uint worldId)
        {
            string worldName;
            if (!_worldNames.ContainsKey(worldId))
            {
                var world = Service.ExcelCache.GetWorldSheet().GetRow(worldId);
                if (world == null)
                {
                    return null;
                }

                _worldNames[worldId] = world.Name.RawString;
            }
            worldName = _worldNames[worldId];

            var dispatch = _apiRequestQueue.DispatchAsync(() =>
                {
                    string url = $"https://universalis.app/api/{worldName}/{itemId}?listings=20&entries=20";

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.AutomaticDecompression = DecompressionMethods.GZip;

                    try
                    {
                        using (WebResponse response = request.GetResponse())
                        {
                            HttpWebResponse webresponse = (HttpWebResponse)response;

                            if (webresponse.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                Service.Log.Warning("Universalis: too many requests!");
                                _tooManyRequests = true;
                                // sleep for 1 minute if too many requests
                                Thread.Sleep(60000);

                                request = (HttpWebRequest)WebRequest.Create(url);
                                webresponse = (HttpWebResponse)request.GetResponse();
                            }

                            var reader = new StreamReader(webresponse.GetResponseStream());
                            var value = reader.ReadToEnd();
                            PricingAPIResponse? apiListing = JsonConvert.DeserializeObject<PricingAPIResponse>(value);

                            if (apiListing != null)
                            {
                                var listing = MarketPricing.FromApi(apiListing, worldId, SaleHistoryLimit);
                                Service.Framework.RunOnFrameworkThread(() =>
                                    ItemPriceRetrieved?.Invoke(itemId, worldId, listing));
                            }
                            else
                            {
                                LastFailure = DateTime.Now;
                                Service.Log.Verbose("Universalis: could not parse listing data json");
                            }
                            
                            // Simple way to prevent too many requests
                            Thread.Sleep(500);
                        }
                    }
                    catch (Exception ex)
                    {
                        Service.Log.Debug(ex.ToString() + ex.InnerException?.ToString());
                    }

                });
            _disposables.Add(dispatch);
            return null;
        }
        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
            
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                _apiRequestQueue.Dispose();
                _queuedItems.Dispose();
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
            _disposed = true;         
        }
    }


    public class MultiRequest
    {
        public string[] itemIDs { internal get; set; }
        public Dictionary<string,PricingAPIResponse> items { internal get; set; }
    }

    public class PricingAPIResponse
    {
        public uint itemID { internal get; set; }
        public float averagePriceNQ { get; set; }
        public float averagePriceHQ { get; set; }
        public float minPriceNQ { get; set; }
        public float minPriceHQ { get; set; }
        public RecentHistory[]? recentHistory;
        public Listing[]? listings;
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

    public class RecentHistory
    {
        public bool hq { get; set; }
        public int pricePerUnit { get; set; }
        public int quantity { get; set; }
        public int timestamp { get; set; }
        public string buyerName { get; set; }
        public int total { get; set; }
    }

}
