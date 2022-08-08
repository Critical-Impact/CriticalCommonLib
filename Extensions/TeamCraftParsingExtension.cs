using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Extensions
{
    public static class TeamCraftParsingExtension
    {
        public static IDictionary<uint, uint> ParseItemsFromCraftList(this string craftList)
        {
            var itemSheet = Service.ExcelCache.GetSheet<ItemEx>();
            var parsedItems = new Dictionary<uint, uint>();
            var lines = craftList.Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                if (line.Length > 0 && char.IsDigit(line[0]))
                {
                    var qtyAndName = line.Split("x ", 2);
                    if (qtyAndName.Length >= 2)
                    {
                        var qty = qtyAndName[0];
                        var name = qtyAndName[1].Trim().ToLower();
                        var actualItem = itemSheet.FirstOrDefault(c => c.Name.ToString().Trim().ToLower() == name);
                        if (actualItem != null && UInt32.TryParse(qty, out var actualQty))
                        {
                            parsedItems.Add(actualItem.RowId, actualQty);
                        }
                    }

                }
            }

            return parsedItems;
        }
    }
}