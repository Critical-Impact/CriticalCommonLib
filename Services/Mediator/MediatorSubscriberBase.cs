using Microsoft.Extensions.Logging;

namespace CriticalCommonLib.Services.Mediator;

public abstract class MediatorSubscriberBase : IMediatorSubscriber
{
    protected MediatorSubscriberBase(ILogger logger, MediatorService mediatorService)
    {
        Logger = logger;

        Logger.LogDebug("Creating {type} ({this})", GetType().Name, this);
        MediatorService = mediatorService;
    }

    public MediatorService MediatorService { get; }
    protected ILogger Logger { get; }

    protected void UnsubscribeAll()
    {
        Logger.LogDebug("Unsubscribing from all for {type} ({this})", GetType().Name, this);
        MediatorService.UnsubscribeAll(this);
    }
}