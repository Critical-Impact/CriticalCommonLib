using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Hosting;

namespace CriticalCommonLib.MarketBoard;

/// <summary>
/// Checks the market cache for items that need to be checked and queues them.
/// </summary>
public class MarketRefreshService : BackgroundService
{
    private readonly IMarketCache _marketCache;
    private readonly MarketCacheConfiguration _marketCacheConfiguration;
    private readonly IPluginLog _pluginLog;

    private const uint RefreshInterval = 60;

    public MarketRefreshService(IMarketCache marketCache, MarketCacheConfiguration marketCacheConfiguration, IPluginLog pluginLog)
    {
        _marketCache = marketCache;
        _marketCacheConfiguration = marketCacheConfiguration;
        _pluginLog = pluginLog;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _pluginLog.Verbose($"Market refresh service waiting for {RefreshInterval} seconds.");
            await Task.Delay(TimeSpan.FromSeconds(RefreshInterval), stoppingToken);
            var queuedItems = ProcessOldPrices(stoppingToken);
            if (queuedItems != 0)
            {
                _pluginLog.Verbose($"Market refresh service has queued {queuedItems} items.");
            }
        }
    }

    public uint ProcessOldPrices(CancellationToken stoppingToken)
    {
        var itemsQueued = 0u;
        foreach (var item in _marketCache.CachedPricing)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            var now = DateTime.Now;
            var diff = now - item.Value.LastUpdate;
            if (diff.TotalHours > _marketCacheConfiguration.CacheMaxAgeHours)
            {
                if (_marketCache.GetPricing(item.Key.Item1, item.Key.Item2, true, false, out _) ==
                    MarketCachePricingResult.Queued)
                {
                    itemsQueued++;
                }
            }
        }

        return itemsQueued;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _pluginLog.Verbose("Stopping service {Type} ({This})", GetType().Name, this);
        await base.StopAsync(cancellationToken);
        _pluginLog.Verbose("Stopped service {Type} ({This})", GetType().Name, this);
    }
}