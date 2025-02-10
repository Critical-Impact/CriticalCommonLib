using System.Collections.Generic;

namespace CriticalCommonLib.Services;

public interface IMarketOrderService
{
    /// <summary>
    /// Returns a dictionary that maps the inventory slots to menu indexes, if the slot is missing then it can be assumed there is no item in the list
    /// </summary>
    /// <returns>An array of slot IDs</returns>
    unsafe Dictionary<int, int>? GetCurrentOrder();
}