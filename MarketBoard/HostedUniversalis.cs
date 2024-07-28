using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Interfaces;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CriticalCommonLib.MarketBoard;

public class HostedUniversalis : BackgroundService, IUniversalis
{
    private readonly IFramework _framework;
    private readonly IHostedUniversalisConfiguration _hostedUniversalisConfiguration;
    public ILogger<HostedUniversalis> Logger { get; }
    public HttpClient HttpClient { get; }
    public IBackgroundTaskQueue UniversalisQueue { get; }
    private Dictionary<uint, string> _worldNames = new();
    public uint QueueTime { get; } = 5;
    public uint MaxRetries { get; } = 3;
    public DateTime? LastFailure { get; private set; }
    public bool TooManyRequests { get; private set; }
    
    public int QueuedCount => _queuedCount;


    public HostedUniversalis(ILogger<HostedUniversalis> logger, HttpClient httpClient, MarketboardTaskQueue marketboardTaskQueue, IFramework framework, IHostedUniversalisConfiguration hostedUniversalisConfiguration)
    {
        _framework = framework;
        _hostedUniversalisConfiguration = hostedUniversalisConfiguration;
        Logger = logger;
        HttpClient = httpClient;
        UniversalisQueue = marketboardTaskQueue;
        _framework.Update += FrameworkOnUpdate;
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        foreach (var world in _queueWorldItemIds)
        {
            if (world.Value.Item1 < DateTime.Now)
            {
                _queueWorldItemIds.Remove(world.Key, out var fullList);
                _queuedCount += fullList.Item2.Count;
                UniversalisQueue.QueueBackgroundWorkItemAsync(token => RetrieveMarketBoardPrices(fullList.Item2, world.Key,token));
                break;
            }
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BackgroundProcessing(stoppingToken);
    }
    
    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = 
                await UniversalisQueue.DequeueAsync(stoppingToken);

            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, 
                    "Error occurred executing {WorkItem}.", nameof(workItem));
            }
        }
    }

    public event Universalis.ItemPriceRetrievedDelegate? ItemPriceRetrieved;
    public void SetSaleHistoryLimit(int limit)
    {
    }

    public void Initialise()
    {
    }

    public void QueuePriceCheck(uint itemId, uint worldId)
    {
        _queueWorldItemIds.TryAdd(worldId, (DateTime.Now.AddSeconds(QueueTime), []));
        _queueWorldItemIds[worldId].Item2.Add(itemId);
        if (_queueWorldItemIds[worldId].Item2.Count == 50)
        {
            _queueWorldItemIds.Remove(worldId, out var fullList);
            _queuedCount += fullList.Item2.Count;
            UniversalisQueue.QueueBackgroundWorkItemAsync(token => RetrieveMarketBoardPrices(fullList.Item2, worldId,token));
        }
    }

    private ConcurrentDictionary<uint, (DateTime,HashSet<uint>)> _queueWorldItemIds = new();
    private int _queuedCount;


    public async Task RetrieveMarketBoardPrices(IEnumerable<uint> itemIds, uint worldId, CancellationToken token,uint attempt = 0)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }
        var itemIdList = itemIds.ToList();
        if (attempt == MaxRetries)
        {
            _queuedCount -= itemIdList.Count;
            Service.Log.Error($"Maximum retries for universalis has been reached, cancelling.");
            return;
        }
        string worldName;
        if (!_worldNames.ContainsKey(worldId))
        {
            var world = Service.ExcelCache.GetWorldSheet().GetRow(worldId);
            if (world == null)
            {
                _queuedCount -= itemIdList.Count;
                return;
            }

            _worldNames[worldId] = world.Name.RawString;
        }
        worldName = _worldNames[worldId];

        var itemIdsString = String.Join(",", itemIdList.Select(c => c.ToString()).ToArray());
        Service.Log.Verbose($"Sending request for items {itemIdsString} to universalis API.");
        string url =
            $"https://universalis.app/api/v2/{worldName}/{itemIdsString}?listings=20&entries=20";
        try
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var response = await HttpClient.GetAsync(url, token);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Logger.LogWarning("Too many requests to universalis, waiting a minute.");
                TooManyRequests = true;
                await Task.Delay(TimeSpan.FromMinutes(1));
                await RetrieveMarketBoardPrices(itemIdList, worldId, token, attempt + 1);
                return;
            }

            TooManyRequests = false;

            var value = await response.Content.ReadAsStringAsync(token);
            
            if (itemIdList.Count == 1)
            {
                PricingAPIResponse? apiListing = JsonConvert.DeserializeObject<PricingAPIResponse>(value);

                if (apiListing != null)
                {
                    var listing = MarketPricing.FromApi(apiListing, worldId,
                        _hostedUniversalisConfiguration.SaleHistoryLimit);
                    await Service.Framework.RunOnFrameworkThread(() =>
                        ItemPriceRetrieved?.Invoke(apiListing.itemID, worldId, listing));
                }
                else
                {
                    Logger.LogError("Failed to parse universalis json data, backing off 30 seconds.");
                    LastFailure = DateTime.Now;
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            }
            else
            {
                MultiRequest? multiRequest = JsonConvert.DeserializeObject<MultiRequest>(value);
                if (multiRequest != null)
                {
                    foreach (var item in multiRequest.items)
                    {
                        var listing = MarketPricing.FromApi(item.Value, worldId,
                            _hostedUniversalisConfiguration.SaleHistoryLimit);
                        await Service.Framework.RunOnFrameworkThread(() =>
                            ItemPriceRetrieved?.Invoke(item.Value.itemID, worldId, listing));
                    }
                }
                else
                {
                    Logger.LogError("Failed to parse universalis multi request json data, backing off 30 seconds.");
                    LastFailure = DateTime.Now;
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            
        }
        catch (JsonReaderException readerException)
        {
            Service.Log.Error(readerException.ToString());
            Logger.LogError("Failed to parse universalis data, backing off 30 seconds.");
            LastFailure = DateTime.Now;
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            Service.Log.Debug(ex.ToString());
        }
        _queuedCount -= itemIdList.Count;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _framework.Update -= FrameworkOnUpdate;
        }
    }

    public sealed override void Dispose()
    {
        Dispose(true);
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}