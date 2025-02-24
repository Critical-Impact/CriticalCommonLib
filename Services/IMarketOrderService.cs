using System.Collections.Generic;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Services;

public interface IMarketOrderService
{
    /// <summary>
    /// Returns a dictionary that maps the inventory slots to menu indexes, if the slot is missing then it can be assumed there is no item in the list
    /// </summary>
    /// <returns>An array of slot IDs</returns>
    unsafe Dictionary<int, int>? GetCurrentOrder();

    /// <summary>
    /// Sorts a list of inventory items with a best attempt at ordering if the RetainerSellList window is not open.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public IEnumerable<InventoryItem> SortByBackupRetainerMarketOrder(IEnumerable<InventoryItem> item);

    /// <summary>
    /// Sorts a list of inventory items with a best attempt at ordering if the RetainerSellList window is not open.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> SortByBackupRetainerMarketOrder(
        IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> item);
}