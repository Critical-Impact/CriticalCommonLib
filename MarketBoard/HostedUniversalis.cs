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
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CriticalCommonLib.MarketBoard;

public class HostedUniversalis : BackgroundService, IUniversalis
{
    private readonly UniversalisUserAgent _userAgent;
    private readonly ExcelSheet<World> _worldSheet;
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


    public HostedUniversalis(ILogger<HostedUniversalis> logger, UniversalisUserAgent userAgent, HttpClient httpClient, MarketboardTaskQueue marketboardTaskQueue, ExcelSheet<World> worldSheet, IFramework framework, IHostedUniversalisConfiguration hostedUniversalisConfiguration)
    {
        _userAgent = userAgent;
        _worldSheet = worldSheet;
        _framework = framework;
        _hostedUniversalisConfiguration = hostedUniversalisConfiguration;
        Logger = logger;
        HttpClient = httpClient;
        httpClient.DefaultRequestHeaders.Add("User-Agent", $"AllaganTools/{_userAgent.PluginVersion}");
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogTrace("Stopping service {Type} ({This})", GetType().Name, this);
        await base.StopAsync(cancellationToken);
        Logger.LogTrace("Stopped service {Type} ({This})", GetType().Name, this);
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
        if (worldId == 0)
        {
            return;
        }
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

        if (worldId == 0)
        {
            return;
        }
        var itemIdList = itemIds.ToList();
        if (attempt == MaxRetries)
        {
            _queuedCount -= itemIdList.Count;
            Logger.LogError($"Maximum retries for universalis has been reached, cancelling.");
            return;
        }
        string worldName;
        if (!_worldNames.ContainsKey(worldId))
        {
            var world = _worldSheet.GetRowOrDefault(worldId);
            if (world == null)
            {
                _queuedCount -= itemIdList.Count;
                return;
            }

            _worldNames[worldId] = world.Value.Name.ExtractText();
        }
        worldName = _worldNames[worldId];

        var itemIdsString = String.Join(",", itemIdList.Select(c => c.ToString()).ToArray());
        Logger.LogTrace("Sending request for items {ItemIds} to universalis API.", itemIdsString);
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
                await Task.Delay(TimeSpan.FromMinutes(1), token);
                if (token.IsCancellationRequested)
                {
                    return;
                }
                await RetrieveMarketBoardPrices(itemIdList, worldId, token, attempt + 1);
                return;
            }

            TooManyRequests = false;

            var value = await response.Content.ReadAsStringAsync(token);

            if (value == "error code: 504")
            {
                Logger.LogWarning("Gateway timeout to universalis, waiting 30 seconds.");
                LastFailure = DateTime.Now;
                await Task.Delay(TimeSpan.FromSeconds(30), token);
                if (token.IsCancellationRequested)
                {
                    return;
                }
                await RetrieveMarketBoardPrices(itemIdList, worldId, token, attempt + 1);
                return;
            }

            if (itemIdList.Count == 1)
            {
                PricingAPIResponse? apiListing = JsonConvert.DeserializeObject<PricingAPIResponse>(value);

                if (apiListing != null)
                {
                    var listing = MarketPricing.FromApi(apiListing, worldId,
                        _hostedUniversalisConfiguration.SaleHistoryLimit);
                    _ = _framework.RunOnFrameworkThread(() =>
                        ItemPriceRetrieved?.Invoke(apiListing.itemID, worldId, listing));
                }
                else
                {
                    Logger.LogError("Failed to parse universalis json data, backing off 30 seconds.");
                    LastFailure = DateTime.Now;
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            else
            {
                MultiRequest? multiRequest = JsonConvert.DeserializeObject<MultiRequest>(value);
                if (multiRequest != null && multiRequest.items != null)
                {
                    foreach (var item in multiRequest.items.Select(c => c.Value))
                    {
                        var listing = MarketPricing.FromApi(item, worldId,
                            _hostedUniversalisConfiguration.SaleHistoryLimit);
                        _ = _framework.RunOnFrameworkThread(() =>
                            ItemPriceRetrieved?.Invoke(item.itemID, worldId, listing));
                    }
                }
                else
                {
                    Logger.LogError("Failed to parse universalis multi request json data, backing off 30 seconds.");
                    LastFailure = DateTime.Now;
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {

        }
        catch (JsonReaderException readerException)
        {
            Logger.LogError(readerException, "Failed to parse universalis data, backing off 30 seconds");
            LastFailure = DateTime.Now;
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            if (token.IsCancellationRequested)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.LogTrace(ex, "Unhandled exception in universalis");
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