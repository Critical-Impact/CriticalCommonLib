using System;
using System.Collections.Generic;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Services
{
    public interface IInventoryMonitor : IDisposable
    {
        Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> Inventories { get; }
        IEnumerable<InventoryItem> AllItems { get; }
        Dictionary<(uint, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags, ulong), int> RetainerItemCounts { get; }
        Dictionary<(uint, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags), int> ItemCounts { get; }
        void GenerateItemCounts();
        event InventoryMonitor.InventoryChangedDelegate? OnInventoryChanged;
        List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryCategory category);
        List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryType inventoryType);
        void ClearCharacterInventories(ulong characterId);
        void LoadExistingData(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories);
    }
}