namespace CriticalCommonLib.Services.Mediator;

public record PluginLoadedMessage : MessageBase;
public record MarketCacheUpdated(uint itemId) : MessageBase;
public record MarketRequestItemUpdate(uint itemId) : MessageBase;
public record MarketRequestItemWorldUpdate(uint itemId, uint worldId) : MessageBase;