using System.Collections.Generic;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Services;

public interface IMarketOrderService
{
    /// <summary>
    /// Sorts a list of inventory items with a best attempt at ordering if the RetainerSellList window is not open.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public IEnumerable<InventoryItem> SortByRetainerMarketOrder(IEnumerable<InventoryItem> item);

    /// <summary>
    /// Sorts a list of inventory items with a best attempt at ordering if the RetainerSellList window is not open.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> SortByRetainerMarketOrder(
        IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> item);
}