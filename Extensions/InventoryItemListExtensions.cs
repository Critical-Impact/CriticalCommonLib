using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Models;


namespace CriticalCommonLib.Extensions
{
    public static class InventoryItemListExtensions
    {
        public static IEnumerable<InventoryItem> SortByRetainerMarketOrder(this IEnumerable<InventoryItem> item)
        {
            return item.OrderBy(c => c.Item.Base.ItemUICategory.ValueNullable?.OrderMajor ?? 0)
                .ThenBy(c => c.Item.Base.ItemUICategory.ValueNullable?.OrderMinor ?? 0)
                .ThenBy(c => c.Flags == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None ? 0 : 1)
                .ThenBy(c => c.Item.RowId);
        }
        public static IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> SortByRetainerMarketOrder(this IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> item)
        {
            return item.OrderBy(c => c.GetItem()?.Base.ItemUICategory.ValueNullable?.OrderMajor ?? 0)
                .ThenBy(c => c.GetItem()?.Base.ItemUICategory.ValueNullable?.OrderMinor ?? 0)
                .ThenBy(c => c.Flags == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None ? 0 : 1)
                .ThenBy(c => c.GetItem()?.RowId);
        }

        public static ItemRow? GetItem(this FFXIVClientStructs.FFXIV.Client.Game.InventoryItem inventoryItem)
        {
            return Service.ExcelCache.GetItemSheet().GetRow(inventoryItem.ItemId);
        }
    }
}