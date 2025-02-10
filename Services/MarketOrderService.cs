using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

public class MarketOrderService : IMarketOrderService
{
    private readonly IGameGui gameGui;

    public MarketOrderService(IGameGui gameGui)
    {
        this.gameGui = gameGui;
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
}