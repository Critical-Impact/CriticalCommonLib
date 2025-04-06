using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CriticalCommonLib.Services.Mediator;

public abstract class WindowMediatorSubscriberBase : Window, IMediatorSubscriber, IDisposable
{
    public MediatorService MediatorService { get; set; }
    public ILogger Logger { get; set; }

    protected WindowMediatorSubscriberBase(ILogger logger, MediatorService mediator, string name) : base(name)
    {
        Logger = logger;
        MediatorService = mediator;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Logger.LogDebug("Disposing {type}", GetType());

        MediatorService.UnsubscribeAll(this);
    }
}