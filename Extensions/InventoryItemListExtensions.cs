using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Extensions
{
    public static class InventoryItemListExtensions
    {
        public static IEnumerable<InventoryItem> SortByRetainerMarketOrder(this IEnumerable<InventoryItem> item)
        {
            return item.OrderBy(c => c.Item.ItemUICategory.Value?.OrderMajor ?? 0)
                .ThenBy(c => c.Item.ItemUICategory.Value?.OrderMinor ?? 0)
                .ThenBy(c => c.Item.Unknown19)
                .ThenBy(c => c.Item.RowId);
        }
        public static IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> SortByRetainerMarketOrder(this IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> item)
        {
            return item.OrderBy(c => c.GetItem()?.ItemUICategory.Value?.OrderMajor ?? 0)
                .ThenBy(c => c.GetItem()?.ItemUICategory.Value?.OrderMinor ?? 0)
                .ThenBy(c => c.GetItem()?.Unknown19)
                .ThenBy(c => c.GetItem()?.RowId);
        }

        public static ItemEx? GetItem(this FFXIVClientStructs.FFXIV.Client.Game.InventoryItem inventoryItem)
        {
            return Service.ExcelCache.GetItemExSheet().GetRow(inventoryItem.ItemId);
        }
    }
}