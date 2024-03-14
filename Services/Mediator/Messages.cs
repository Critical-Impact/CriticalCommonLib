namespace CriticalCommonLib.Services.Mediator;

public record PluginLoadedMessage : MessageBase;
public record MarketCacheUpdated(uint itemId) : MessageBase;