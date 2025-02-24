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
    private readonly IGameGui gameGui;
    private readonly ItemSheet _itemSheet;

    public MarketOrderService(IGameGui gameGui, ItemSheet itemSheet)
    {
        this.gameGui = gameGui;
        _itemSheet = itemSheet;
    }

    /// <summary>
    /// Returns a dictionary that maps the inventory slots to menu indexes, if the slot is missing then it can be assumed there is no item in the list
    /// </summary>
    /// <returns>An array of slot IDs</returns>
    public unsafe Dictionary<int, int>? GetCurrentOrder()
    {
        var retainerSellListPtr = this.gameGui.GetAddonByName("RetainerSellList");
        if (retainerSellListPtr == IntPtr.Zero)
        {
            return null;
        }

        var retainerSellList = (AtkUnitBase*)retainerSellListPtr;
        var atkValues = retainerSellList->AtkValues;
        if (atkValues == null)
        {
            return null;
        }

        var currentOrder = new Dictionary<int, int>();
        var atkIndex = 15;
        for (var i = 0; i < 20; i++)
        {
            if (atkValues[atkIndex].Type == 0)
            {
                continue;
            }

            currentOrder.TryAdd(atkValues[atkIndex].Int, i);
            atkIndex += 13;
        }

        return currentOrder;
    }

    public IEnumerable<InventoryItem> SortByBackupRetainerMarketOrder(IEnumerable<InventoryItem> item)
    {
        return item.OrderBy(c => c.Item.Base.ItemUICategory.ValueNullable?.OrderMajor ?? 0)
            .ThenBy(c => c.Item.Base.ItemUICategory.ValueNullable?.OrderMinor ?? 0)
            .ThenBy(c => c.Flags == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None ? 0 : 1)
            .ThenBy(c => c.Item.RowId);
    }
    public IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> SortByBackupRetainerMarketOrder(IEnumerable<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem> item)
    {
        return item.OrderBy(c => GetItem(c)?.Base.ItemUICategory.ValueNullable?.OrderMajor ?? 0)
            .ThenBy(c => GetItem(c)?.Base.ItemUICategory.ValueNullable?.OrderMinor ?? 0)
            .ThenBy(c => c.Flags == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None ? 0 : 1)
            .ThenBy(c => GetItem(c)?.RowId);
    }

    private ItemRow? GetItem(FFXIVClientStructs.FFXIV.Client.Game.InventoryItem inventoryItem)
    {
        return _itemSheet.GetRow(inventoryItem.ItemId);
    }
}