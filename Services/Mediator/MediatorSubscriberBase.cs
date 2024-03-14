using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Services.Mediator;

public abstract class MediatorSubscriberBase : IMediatorSubscriber
{
    protected MediatorSubscriberBase(IPluginLog logger, MediatorService mediatorService)
    {
        Logger = logger;

        Logger.Debug("Creating {type} ({this})", GetType().Name, this);
        MediatorService = mediatorService;
    }

    public MediatorService MediatorService { get; }
    protected IPluginLog Logger { get; }

    protected void UnsubscribeAll()
    {
        Logger.Debug("Unsubscribing from all for {type} ({this})", GetType().Name, this);
        MediatorService.UnsubscribeAll(this);
    }
}