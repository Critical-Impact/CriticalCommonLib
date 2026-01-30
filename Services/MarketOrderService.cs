using System;
using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using Autofac.Core;
using CriticalCommonLib.Models;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

public class MarketOrderService : IMarketOrderService
{
    private readonly ItemSheet _itemSheet;

    public MarketOrderService(ItemSheet itemSheet)
    {
        _itemSheet = itemSheet;
    }

    public IEnumerable<InventoryItem> SortByRetainerMarketOrder(IEnumerable<InventoryItem> item)
    {
        return item.OrderBy(c => c.Item.Base.ItemUICategory.ValueNullable?.OrderMajor ?? 999)
            .ThenBy(c => c.Item.Base.ItemUICategory.ValueNullable?.OrderMinor ?? 999)
            .ThenBy(c => c.Item.Base.Unknown4)
            .ThenBy(c => c.Item.RowId);
    }
    public IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> SortByRetainerMarketOrder(IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> item)
    {
        return item.OrderBy(c => GetItem(c)?.Base.ItemUICategory.ValueNullable?.OrderMajor ?? 999)
            .ThenBy(c => GetItem(c)?.Base.ItemUICategory.ValueNullable?.OrderMinor ?? 999)
            .ThenBy(c => GetItem(c)?.Base.Unknown4)
            .ThenBy(c => GetItem(c)?.RowId);
    }

    private ItemRow? GetItem(FFXIVClientStructs.FFXIV.Client.Game.InventoryItem inventoryItem)
    {
        return _itemSheet.GetRow(inventoryItem.ItemId);
    }
}