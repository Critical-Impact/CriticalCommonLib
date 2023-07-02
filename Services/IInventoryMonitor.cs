using System;
using System.Collections.Generic;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Services
{
    public interface IInventoryMonitor : IDisposable
    {
        Dictionary<ulong, Inventory> Inventories { get; }
        IEnumerable<InventoryItem> AllItems { get; }
        Dictionary<(uint, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags, ulong), int> RetainerItemCounts { get; }
        Dictionary<(uint, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags), int> ItemCounts { get; }
        void GenerateItemCounts();
        event InventoryMonitor.InventoryChangedDelegate? OnInventoryChanged;
        List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryCategory category);
        List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryType inventoryType);
        void ClearCharacterInventories(ulong characterId);
        void LoadExistingData(List<InventoryItem> inventories);
        void SignalRefresh();
    }
}