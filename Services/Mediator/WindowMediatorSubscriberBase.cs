using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Services.Mediator;

public abstract class WindowMediatorSubscriberBase : Window, IMediatorSubscriber, IDisposable
{
    protected readonly IPluginLog _logger;
    public MediatorService MediatorService { get; }

    protected WindowMediatorSubscriberBase() : base("")
    {
        
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        _logger.Debug("Disposing {type}", GetType());

        MediatorService.UnsubscribeAll(this);
    }
}