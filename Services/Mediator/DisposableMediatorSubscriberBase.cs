using System;
using Microsoft.Extensions.Logging;

namespace CriticalCommonLib.Services.Mediator;

public abstract class DisposableMediatorSubscriberBase : MediatorSubscriberBase, IDisposable
{
    protected DisposableMediatorSubscriberBase(ILogger logger, MediatorService
        mediatorService) : base(logger, mediatorService)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Logger.LogDebug("Disposing {type} ({this})", GetType().Name, this);
        UnsubscribeAll();
    }
}