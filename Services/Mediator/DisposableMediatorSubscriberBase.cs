using System;
using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Services.Mediator;

public abstract class DisposableMediatorSubscriberBase : MediatorSubscriberBase, IDisposable
{
    protected DisposableMediatorSubscriberBase(IPluginLog logger, MediatorService
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
        Logger.Debug("Disposing {type} ({this})", GetType().Name, this);
        UnsubscribeAll();
    }
}