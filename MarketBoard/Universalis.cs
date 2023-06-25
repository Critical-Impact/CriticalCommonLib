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
using CriticalCommonLib.Extensions;
#pragma warning disable 8618

namespace CriticalCommonLib.MarketBoard
{

    public class Universalis : IUniversalis
    {
        private SerialQueue _apiRequestQueue = new SerialQueue();
        private List<IDisposable> _disposables = new List<IDisposable>();
        private Subject<uint> _queuedItems = new Subject<uint>();
        private bool _tooManyRequests = false;
        private bool _initialised = false;
        private DateTime? _nextRequestTime;
        
        private readonly int MaxBufferCount = 50;
        private readonly int BufferInterval = 1;
        private int _queuedCount = 0;
        private int _saleHistoryLimit = 7;

        public delegate void ItemPriceRetrievedDelegate(uint itemId, PricingResponse response);

        public event ItemPriceRetrievedDelegate? ItemPriceRetrieved;

        public PricingResponse? RetrieveMarketBoardPrice(InventoryItem item)
        {
            if (!item.Item.ObtainedGil)
            {
                return new PricingResponse();
            }
            return RetrieveMarketBoardPrice(item.ItemId);
        }

        public void SetSaleHistoryLimit(int limit)
        {
            _saleHistoryLimit = limit;
        }

        public void Initalise()
        {
            PluginLog.Verbose("Setting up universalis buffer.");
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
                        RetrieveMarketBoardPrices(itemIds);
                    }
                }));
        }

        public void QueuePriceCheck(uint itemId)
        {
            if (!_initialised)
            {
                Initalise();
            }

            if (itemId != 0)
            {
                _queuedItems.OnNext(itemId);
            }
        }

        public int QueuedCount
        {
            get
            {
                return _queuedCount;
            }
        }

        public int SaleHistoryLimit => _saleHistoryLimit;

        public void RetrieveMarketBoardPrices(IEnumerable<uint> itemIds)
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
                    var dispatch = _apiRequestQueue.DispatchAsync(() =>
                    {
                        var itemId = itemIds.First();
                        string url = $"https://universalis.app/api/{datacenter}/{itemId}?listings=20&entries=20";

                        HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                        request.AutomaticDecompression = DecompressionMethods.GZip;

                        PricingAPIResponse? apiListing = new PricingAPIResponse();
                        try
                        {
                            using (WebResponse response = request.GetResponse())
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

                                var reader = new StreamReader(webresponse.GetResponseStream());
                                var value = reader.ReadToEnd();
                                apiListing = JsonConvert.DeserializeObject<PricingAPIResponse>(value);

                                if (apiListing != null)
                                {
                                    var listing = apiListing.ToPricingResponse(SaleHistoryLimit);
                                    Service.Framework.RunOnFrameworkThread(() =>
                                        ItemPriceRetrieved?.Invoke(apiListing.itemID, listing));
                                }
                                else
                                {
                                    PluginLog.Error("Universalis: Failed to parse universalis json data");
                                }
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Debug(ex.ToString());
                        }
                    });
                    _disposables.Add(dispatch);
                }
                else
                {
                    var dispatch = _apiRequestQueue.DispatchAsync(() =>
                    {
                        var itemIdsString = String.Join(",", itemIds.Select(c => c.ToString()).ToArray());
                        PluginLog.Verbose($"Sending request for items {itemIdsString} to universalis API.");
                        string url =
                            $"https://universalis.app/api/v2/{datacenter}/{itemIdsString}?listings=20&entries=20";

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

                                var reader = new StreamReader(webresponse.GetResponseStream());
                                var value = reader.ReadToEnd();
                                multiRequest = JsonConvert.DeserializeObject<MultiRequest>(value);


                                if (multiRequest != null)
                                {
                                    foreach (var item in multiRequest.items)
                                    {
                                        var listing = item.Value.ToPricingResponse(SaleHistoryLimit);
                                        Service.Framework.RunOnFrameworkThread(() =>
                                            ItemPriceRetrieved?.Invoke(item.Value.itemID, listing));
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
                    });
                    _disposables.Add(dispatch);
                }
            }
            
            
        }

        public PricingResponse? RetrieveMarketBoardPrice(uint itemId)
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
                        string url = $"https://universalis.app/api/{datacenter}/{itemId}?listings=20&entries=20";

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.AutomaticDecompression = DecompressionMethods.GZip;

                        PricingAPIResponse? apiListing = new PricingAPIResponse();
                        try
                        {
                            using (WebResponse response = request.GetResponse())
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

                                var reader = new StreamReader(webresponse.GetResponseStream());
                                var value = reader.ReadToEnd();
                                apiListing = JsonConvert.DeserializeObject<PricingAPIResponse>(value);

                                if (apiListing != null)
                                {
                                    var listing = apiListing.ToPricingResponse(SaleHistoryLimit);
                                    Service.Framework.RunOnFrameworkThread(() =>
                                        ItemPriceRetrieved?.Invoke(itemId, listing));
                                }
                                else
                                {
                                    PluginLog.Verbose("Universalis: could not parse listing data json");
                                }
                                
                                // Simple way to prevent too many requests
                                Thread.Sleep(500);
                            }
                        }
                        catch (Exception ex)
                        {
                            PluginLog.Debug(ex.ToString() + ex.InnerException?.ToString());
                        }

                    });
                _disposables.Add(dispatch);
            }

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

    public class PricingResponse
    {
        public bool loaded { get; set; } = false;
        
        public uint itemID { internal get; set; }
        
        public float averagePriceNQ { get; set; }
        public float averagePriceHQ { get; set; }
        public float minPriceNQ { get; set; }
        public float minPriceHQ { get; set; }
        public int sevenDaySellCount { get; set; }
        public DateTime? lastSellDate { get; set; }

        public static PricingResponse FromApi(PricingAPIResponse apiResponse, int saleHistoryLimit)
        {
            PricingResponse response = new PricingResponse();
            response.averagePriceNQ = apiResponse.averagePriceNQ;
            response.averagePriceHQ = apiResponse.averagePriceHQ;
            response.minPriceHQ = apiResponse.minPriceHQ;
            response.minPriceNQ = apiResponse.minPriceNQ;
            response.itemID = apiResponse.itemID;
            int? realMinPriceHQ = null;
            int? realMinPriceNQ = null;
            if (apiResponse.listings != null && apiResponse.listings.Length != 0)
            {
                foreach (var listing in apiResponse.listings)
                {
                    if (listing.hq)
                    {
                        if (realMinPriceHQ == null || realMinPriceHQ > listing.pricePerUnit)
                        {
                            realMinPriceHQ = listing.pricePerUnit;
                        }
                    }
                    else
                    {
                        if (realMinPriceNQ == null || realMinPriceNQ > listing.pricePerUnit)
                        {
                            realMinPriceNQ = listing.pricePerUnit;
                        }
                    }
                }

                if (realMinPriceHQ != null)
                {
                    response.minPriceHQ = realMinPriceHQ.Value;
                }

                if (realMinPriceNQ != null)
                {
                    response.minPriceNQ = realMinPriceNQ.Value;
                }
            }
            if (apiResponse.recentHistory != null && apiResponse.recentHistory.Length != 0)
            {
                DateTime? latestDate = null;
                int sevenDaySales = 0;
                foreach (var history in apiResponse.recentHistory)
                {
                    var dateTime = DateTimeOffset.FromUnixTimeSeconds(history.timestamp).LocalDateTime;
                    if (latestDate == null || latestDate <= dateTime)
                    {
                        latestDate = dateTime;
                    }

                    if (dateTime >= DateTime.Now.AddDays(-saleHistoryLimit))
                    {
                        sevenDaySales++;
                    }
                }

                response.sevenDaySellCount = sevenDaySales;
                response.lastSellDate = latestDate;

            }
            else
            {
                response.lastSellDate = null;
                response.sevenDaySellCount = 0;
            }

            response.loaded = true;
            return response;
        }
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

        public PricingResponse ToPricingResponse(int saleHistoryLimit)
        {
            return PricingResponse.FromApi(this, saleHistoryLimit);
        }
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
