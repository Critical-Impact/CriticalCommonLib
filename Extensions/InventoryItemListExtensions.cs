using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Extensions
{
    public static class InventoryItemListExtensions
    {
        public static IEnumerable<InventoryItem> SortByRetainerMarketOrder(this IEnumerable<InventoryItem> item)
        {
            return item.OrderBy(c =>
                    c.Item == null ? 0 : c.Item.ItemUICategory.Value?.OrderMajor ?? 0)
                .ThenBy(c =>
                    c.Item == null ? 0 : c.Item.ItemUICategory.Value?.OrderMinor ?? 0)
                .ThenBy(c =>
                    c.Item == null ? 0 : c.Item.Unknown19)
                .ThenBy(c =>
                    c.Item == null ? 0 : c.Item.RowId);
        }
    }
}