using DalaMock.Host.Mediator;

namespace CriticalCommonLib.Services.Mediator;

public record PluginLoadedMessage : MessageBase;
public record MarketCacheUpdatedMessage(uint itemId, uint worldId) : MessageBase;
public record MarketRequestItemUpdateMessage(uint itemId) : MessageBase;
public record MarketRequestItemWorldUpdateMessage(uint itemId, uint worldId) : MessageBase;