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
        Dictionary<(uint, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags, ulong), int> CharacterItemCounts { get; }
        Dictionary<(uint, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags), int> ItemCounts { get; }
        void GenerateItemCounts();
        event InventoryMonitor.InventoryChangedDelegate? OnInventoryChanged;
        List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryCategory category);
        List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryType inventoryType);
        
        /// <summary>
        /// Remove a specific character's cached inventory
        /// </summary>
        /// <param name="characterId"></param>
        void ClearCharacterInventories(ulong characterId);
        
        /// <summary>
        /// Load an existing set of inventories into the monitor, run this before running Start
        /// </summary>
        /// <param name="inventories"></param>
        void LoadExistingData(List<InventoryItem> inventories);
        
        /// <summary>
        /// Force the monitor to retrieve a fresh set of data from the scanner
        /// </summary>
        void SignalRefresh();
        /// <summary>
        /// Has the monitoring process begun?
        /// </summary>
        bool Started { get; }
        /// <summary>
        /// Start the monitoring process, won't process inventory events until run
        /// </summary>
        void Start();
    }
}