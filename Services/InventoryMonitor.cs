using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services.Ui;
using Dalamud.Game;
using Dalamud.Game.Network;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using CriticalCommonLib.Extensions;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using static FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;
using InventoryItem = CriticalCommonLib.Models.InventoryItem;
using InventoryType = CriticalCommonLib.Enums.InventoryType;

namespace CriticalCommonLib.Services
{
    public class InventoryMonitor : IDisposable
    {
        public delegate void InventoryChangedDelegate(
            Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories, ItemChanges changedItems);

        private IEnumerable<InventoryItem> _allItems;
        private CharacterMonitor _characterMonitor;
        private Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> _inventories;
        private Dictionary<(uint, ItemFlags, ulong), int> _itemCounts = new();
        private Dictionary<InventoryType, bool> _loadedInventories;
        private Queue<DateTime> _scheduledUpdates = new ();
        private Dictionary<uint, ItemMarketBoardInfo> _retainerMarketPrices = new();
        private InventoryScanner _inventoryScanner;
        private CraftMonitor _craftMonitor;

        public InventoryMonitor(CharacterMonitor monitor, CraftMonitor craftMonitor, InventoryScanner scanner)
        {
            _characterMonitor = monitor;
            _craftMonitor = craftMonitor;
            _inventoryScanner = scanner;

            _inventories = new Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>();
            _allItems = new List<InventoryItem>();
            _loadedInventories = new Dictionary<InventoryType, bool>();

            _inventoryScanner.BagsChanged += InventoryScannerOnBagsChanged;
            _characterMonitor.OnCharacterRemoved += CharacterMonitorOnOnCharacterRemoved;
        }

        private void InventoryScannerOnBagsChanged(List<BagChange> changes)
        {
            PluginLog.Verbose("Bags changed, generating inventory");
            GenerateInventories(InventoryGenerateReason.ScheduledUpdate);
        }

        private void CharacterMonitorOnOnCharacterRemoved(ulong characterId)
        {
            if (_inventories.ContainsKey(characterId))
            {
                _inventoryScanner.ClearRetainerCache(characterId);
                foreach (var inventory in _inventories[characterId])
                {
                    inventory.Value.Clear();
                }

                Service.Framework.RunOnFrameworkThread(() =>
                {
                    OnInventoryChanged?.Invoke(_inventories,
                        new ItemChanges()
                            { NewItems = new List<ItemChangesItem>(), RemovedItems = new List<ItemChangesItem>() });
                });
            }
        }

        public Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> Inventories => _inventories;

        public IEnumerable<InventoryItem> AllItems => _allItems;
        
        public Dictionary<(uint, ItemFlags, ulong), int> ItemCounts => _itemCounts;

        public event InventoryChangedDelegate? OnInventoryChanged;

        public List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryCategory category)
        {
            if (_inventories.ContainsKey(characterId))
            {
                if (_inventories[characterId].ContainsKey(category))
                {
                    return _inventories[characterId][category];
                }
            }

            return new List<InventoryItem>();
        }

        public void ClearCharacterInventories(ulong characterId)
        {
            if (_inventories.ContainsKey(characterId))
            {
                _inventoryScanner.ClearRetainerCache(characterId);
                foreach (var inventory in _inventories[characterId])
                {
                    inventory.Value.Clear();
                }

                Service.Framework.RunOnFrameworkThread(() =>
                {
                    OnInventoryChanged?.Invoke(_inventories,
                        new ItemChanges()
                            { NewItems = new List<ItemChangesItem>(), RemovedItems = new List<ItemChangesItem>() });
                });
            }
        }

        public void LoadExistingData(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories)
        {
            if (inventories.ContainsKey(0))
            {
                inventories.Remove(0);
            }
            _inventories = inventories;
            GenerateAllItems();
            Service.Framework.RunOnFrameworkThread(() =>
            {
                OnInventoryChanged?.Invoke(_inventories,
                    new ItemChanges() { NewItems = new(), RemovedItems = new() });
            });
        }

        private void GenerateItemCounts()
        {
            var itemCounts = new Dictionary<(uint, ItemFlags, ulong), int>();
            foreach (var inventory in _inventories)
            {
                foreach (var itemList in inventory.Value.Values)
                {
                    foreach (var item in itemList)
                    {
                        var key = (item.ItemId, item.Flags, item.RetainerId);
                        if (!itemCounts.ContainsKey(key))
                        {
                            itemCounts[key] = 0;
                        }

                        itemCounts[key] += (int)item.Quantity;

                    }
                }
            }
            _itemCounts = itemCounts;
        }

        public static void DiffDictionaries<T, U>(
            Dictionary<T, U> dicA,
            Dictionary<T, U> dicB,
            Dictionary<T, U> dicAdd,
            Dictionary<T, U> dicDel) where T : notnull
        {
            // dicDel has entries that are in A, but not in B, 
            // ie they were deleted when moving from A to B
            diffDicSub<T, U>(dicA, dicB, dicDel);

            // dicAdd has entries that are in B, but not in A,
            // ie they were added when moving from A to B
            diffDicSub<T, U>(dicB, dicA, dicAdd);
        }

        private static void diffDicSub<T, U>(
            Dictionary<T, U> dicA,
            Dictionary<T, U> dicB,
            Dictionary<T, U> dicAExceptB) where T : notnull
        {
            // Walk A, and if any of the entries are not
            // in B, add them to the result dictionary.

            foreach (KeyValuePair<T, U> kvp in dicA)
            {
                if (!dicB.Contains(kvp))
                {
                    dicAExceptB[kvp.Key] = kvp.Value;
                }
            }
        }

        private ItemChangesItem ConvertHashedItem((uint, ItemFlags, ulong) itemHash, int quantity)
        {
            return new ItemChangesItem()
            {
                ItemId = itemHash.Item1, 
                Flags = itemHash.Item2,
                CharacterId = itemHash.Item3,
                Quantity = quantity,
                Date = DateTime.Now
            };
        }

        private ItemChanges CompareItemCounts(Dictionary<(uint, ItemFlags, ulong), int> oldItemCounts, Dictionary<(uint, ItemFlags, ulong), int> newItemCounts)
        {
            Dictionary<(uint, ItemFlags, ulong), int> newItems = new Dictionary<(uint, ItemFlags, ulong), int>();
            Dictionary<(uint, ItemFlags, ulong), int> removedItems = new Dictionary<(uint, ItemFlags, ulong), int>();

            // Iterate through the oldItems dictionary
            foreach (var oldItem in oldItemCounts)
            {
                // Check if the item is present in the newItems dictionary
                if (newItemCounts.ContainsKey(oldItem.Key))
                {
                    // Check if the quantity has changed
                    if (newItemCounts[oldItem.Key] != oldItem.Value)
                    {
                        // Add the item to the updatedItems dictionary
                        var relativeCount = newItemCounts[oldItem.Key] - oldItem.Value;
                        if (relativeCount > 0)
                        {
                            newItems.Add(oldItem.Key, relativeCount);
                        }
                        else
                        {
                            removedItems.Add(oldItem.Key, Math.Abs(relativeCount));
                        }
                    }
                }
                else
                {
                    // Add the item to the removedItems dictionary
                    removedItems.Add(oldItem.Key, oldItem.Value);
                }
            }

            // Iterate through the newItems dictionary
            foreach (var newItem in newItemCounts)
            {
                // Check if the item is not present in the oldItems dictionary
                if (!oldItemCounts.ContainsKey(newItem.Key))
                {
                    // Add the item to the newItems dictionary
                    newItems.Add(newItem.Key, newItem.Value);
                }
            }
            
            List<ItemChangesItem> actualAddedItems = new();
            List<ItemChangesItem> actualDeletedItems = new();
            
            foreach (var newItem in newItems)
            {
                actualAddedItems.Add(ConvertHashedItem(newItem.Key, newItem.Value));
            }

            foreach (var removedItem in removedItems)
            {
                actualDeletedItems.Add(ConvertHashedItem(removedItem.Key, removedItem.Value));
            }
            
            return new ItemChanges() {NewItems = actualAddedItems, RemovedItems = actualDeletedItems};
        }

        private void GenerateAllItems()
        {
            IEnumerable<InventoryItem> newItems = new List<InventoryItem>();

            foreach (var inventory in _inventories)
            {
                foreach (var item in inventory.Value.Values)
                {
                    newItems = newItems.Concat(item);
                }
            }
            _allItems = newItems;
        }

        public enum InventoryGenerateReason
        {
            InventoryChanged,
            ScheduledUpdate,
            NetworkUpdate,
            WindowOpened,
        }

        private unsafe void GenerateInventories(InventoryGenerateReason generateReason)
        {
            Task.Run(GenerateInventoriesTask);
        }

        private void GenerateInventoriesTask()
        {
            if (Service.ClientState.LocalContentId == 0)
            {
                PluginLog.Debug("Not generating inventory, not logged in.");
                return;
            }


            GenerateItemCounts();

            var newInventories = new Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>();
            newInventories.Add(Service.ClientState.LocalContentId,
                new Dictionary<InventoryCategory, List<InventoryItem>>());
            GenerateCharacterInventories(newInventories);
            GenerateSaddleInventories(newInventories);
            GenerateArmouryChestInventories(newInventories);
            GenerateEquippedItems(newInventories);
            GenerateFreeCompanyInventories(newInventories);
            GenerateRetainerInventories(newInventories);
            GenerateGlamourInventories(newInventories);
            GenerateArmoireInventories(newInventories);
            GenerateCurrencyInventories(newInventories);
            GenerateCrystalInventories(newInventories);

            foreach (var newInventory in newInventories)
            {
                if (!_inventories.ContainsKey(newInventory.Key))
                {
                    _inventories.Add(newInventory.Key, new Dictionary<InventoryCategory, List<InventoryItem>>());
                }

                foreach (var invDict in newInventory.Value)
                {
                    _inventories[newInventory.Key][invDict.Key] = invDict.Value;
                }
            }

            var oldItemCounts = _itemCounts;
            GenerateItemCounts();
            var newItemCounts = _itemCounts;
            var itemChanges = CompareItemCounts(oldItemCounts, newItemCounts);
            GenerateAllItems();
            Service.Framework.RunOnFrameworkThread(() =>
            {
                OnInventoryChanged?.Invoke(_inventories, itemChanges);
            });
        }

        private unsafe void GenerateCharacterInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory2) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory3) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory1) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory4))
            {
                var bag1 = _inventoryScanner.CharacterBag1;
                var bag2 = _inventoryScanner.CharacterBag2;
                var bag3 = _inventoryScanner.CharacterBag3;
                var bag4 = _inventoryScanner.CharacterBag4;
                var sorted = new List<InventoryItem>();

                
                for (var index = 0; index < bag1.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag1[index]);
                    newItem.SortedContainer = InventoryType.Bag0;
                    newItem.SortedCategory = InventoryCategory.CharacterBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);

                }

                for (var index = 0; index < bag2.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag2[index]);
                    newItem.SortedContainer = InventoryType.Bag1;
                    newItem.SortedCategory = InventoryCategory.CharacterBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);
                }

                for (var index = 0; index < bag3.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag3[index]);
                    newItem.SortedContainer = InventoryType.Bag2;
                    newItem.SortedCategory = InventoryCategory.CharacterBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);
                }

                for (var index = 0; index < bag4.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag4[index]);
                    newItem.SortedContainer = InventoryType.Bag3;
                    newItem.SortedCategory = InventoryCategory.CharacterBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);
                }
                newInventories[Service.ClientState.LocalContentId]
                    .Add(InventoryCategory.CharacterBags, sorted);
            }
        }

        private unsafe void GenerateSaddleInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.SaddleBag1) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.SaddleBag2))
            {
                var bag1 = _inventoryScanner.SaddleBag1;
                var bag2 = _inventoryScanner.SaddleBag2;
                var sorted = new List<InventoryItem>();

                
                for (var index = 0; index < bag1.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag1[index]);
                    newItem.SortedContainer = InventoryType.SaddleBag0;
                    newItem.SortedCategory = InventoryCategory.CharacterSaddleBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);

                }

                for (var index = 0; index < bag2.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag2[index]);
                    newItem.SortedContainer = InventoryType.SaddleBag1;
                    newItem.SortedCategory = InventoryCategory.CharacterSaddleBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);
                }

                newInventories[Service.ClientState.LocalContentId]
                    .Add(InventoryCategory.CharacterSaddleBags, sorted);

            }

            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.PremiumSaddleBag1) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.PremiumSaddleBag2))
            {
                var bag1 = _inventoryScanner.PremiumSaddleBag1;
                var bag2 = _inventoryScanner.PremiumSaddleBag2;
                var sorted = new List<InventoryItem>();

                
                for (var index = 0; index < bag1.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag1[index]);
                    newItem.SortedContainer = InventoryType.PremiumSaddleBag0;
                    newItem.SortedCategory = InventoryCategory.CharacterPremiumSaddleBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);

                }

                for (var index = 0; index < bag2.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag2[index]);
                    newItem.SortedContainer = InventoryType.PremiumSaddleBag1;
                    newItem.SortedCategory = InventoryCategory.CharacterPremiumSaddleBags;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);
                }

                newInventories[Service.ClientState.LocalContentId]
                    .Add(InventoryCategory.CharacterPremiumSaddleBags, sorted);
            }
        }

        private unsafe void GenerateArmouryChestInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryMainHand);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryHead);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryBody);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryHands);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryLegs);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryFeets);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryOffHand);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryEar);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryNeck);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryWrist);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryRings);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmorySoulCrystal);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    return;
                }
            }

            var gearSets = _inventoryScanner.GetGearSets();
            var sorted = new List<InventoryItem>();
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    continue;
                }
                var armoryItems = _inventoryScanner.GetInventoryByType(inventoryType);
                for (var index = 0; index < armoryItems.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(armoryItems[index]);
                    newItem.SortedContainer = inventoryType.Convert();
                    newItem.SortedCategory = InventoryCategory.CharacterArmoryChest;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    if(gearSets.ContainsKey(newItem.ItemId))
                    {
                        newItem.GearSets = gearSets[newItem.ItemId].Select(c => (uint)c.Item1).ToArray();
                        newItem.GearSetNames = gearSets[newItem.ItemId].Select(c => c.Item2).ToArray();
                    }
                    else if(gearSets.ContainsKey(newItem.ItemId + 1_000_000))
                    {
                        newItem.GearSets = gearSets[newItem.ItemId + 1_000_000].Select(c => (uint)c.Item1).ToArray();
                        newItem.GearSetNames = gearSets[newItem.ItemId + 1_000_000].Select(c => c.Item2).ToArray();
                    }
                    else
                    {
                        newItem.GearSets = new uint[]{};
                    }
                    sorted.Add(newItem);
                }
            }
            newInventories[Service.ClientState.LocalContentId]
                .Add(InventoryCategory.CharacterArmoryChest, sorted);
        }

        private void GenerateEquippedItems(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.EquippedItems))
            {
                var bag1 = _inventoryScanner.CharacterEquipped;
                var sorted = new List<InventoryItem>();

                
                for (var index = 0; index < bag1.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(bag1[index]);
                    newItem.SortedContainer = InventoryType.GearSet0;
                    newItem.SortedCategory = InventoryCategory.CharacterEquipped;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted.Add(newItem);

                }
                newInventories[Service.ClientState.LocalContentId]
                    .Add(InventoryCategory.CharacterEquipped, sorted);
            }
        }

        private void GenerateFreeCompanyInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var freeCompanyItems = _inventories.ContainsKey(Service.ClientState.LocalContentId)
                ? _inventories[Service.ClientState.LocalContentId].ContainsKey(InventoryCategory.FreeCompanyBags)
                    ? _inventories[Service.ClientState.LocalContentId][InventoryCategory.FreeCompanyBags].ToList()
                    : new List<InventoryItem>()
                : new List<InventoryItem>();
            
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage1);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage2);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage3);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage4);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage5);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyCrystals);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyGil);
            var inventoryLoaded = false;
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    continue;
                }

                inventoryLoaded = true;
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                freeCompanyItems.RemoveAll(c => c.Container == inventoryType.Convert());
                var items = _inventoryScanner.GetInventoryByType(inventoryType);

                for (var index = 0; index < items.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
                    newItem.SortedContainer = inventoryType.Convert();
                    newItem.SortedCategory = inventoryCategory;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    freeCompanyItems.Add(newItem);
                }
            }

            if (inventoryLoaded)
            {
                newInventories[Service.ClientState.LocalContentId]
                    .Add(InventoryCategory.FreeCompanyBags, freeCompanyItems);
            }
        }
        private unsafe void GenerateRetainerInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var currentRetainer = _characterMonitor.ActiveRetainer;
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage1);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage2);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage3);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage4);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage5);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage6);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage7);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerEquippedItems);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerMarket);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerCrystals);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerGil);
            if (currentRetainer != 0)
            {
                if (!_inventoryScanner.InMemoryRetainers.ContainsKey(currentRetainer))
                {
                    PluginLog.Debug("Inventory scanner does not have information about this retainer.");
                    return;
                }
                foreach (var inventoryType in inventoryTypes)
                {
                    if (!_inventoryScanner.InMemoryRetainers[currentRetainer].Contains(inventoryType))
                    {
                        PluginLog.Debug("Inventory scanner does not have information about a retainer's " + inventoryType.ToString());
                        return;
                    }
                }
                PluginLog.Debug("Retainer inventory found in scanner, loading into inventory monitor.");
                var sorted = new Dictionary<InventoryCategory,List<InventoryItem>>();
                foreach (var inventoryType in inventoryTypes)
                {
                    var items = _inventoryScanner.GetInventoryByType(currentRetainer,inventoryType);
                    var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                    if (!sorted.ContainsKey(inventoryCategory))
                    {
                        sorted.Add(inventoryCategory, new List<InventoryItem>());
                    }
                    for (var index = 0; index < items.Length; index++)
                    {
                        var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
                        newItem.SortedContainer = inventoryType.Convert();
                        newItem.SortedCategory = inventoryCategory;
                        newItem.RetainerId = currentRetainer;
                        newItem.SortedSlotIndex = newItem.Slot;
                        if (inventoryType == FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerMarket)
                        {
                            newItem.RetainerMarketPrice = _inventoryScanner.RetainerMarketPrices[currentRetainer][index];
                        }
                        sorted[inventoryCategory].Add(newItem);
                    }
                }

                foreach (var category in sorted)
                {
                    if (!newInventories.ContainsKey(currentRetainer))
                    {
                        newInventories.Add(currentRetainer, new Dictionary<InventoryCategory, List<InventoryItem>>());
                    }
                    newInventories[currentRetainer]
                        .Add(category.Key, category.Value);
                }
            }
        }
        
        private void GenerateArmoireInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( (FFXIVClientStructs.FFXIV.Client.Game.InventoryType)2500);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    return;
                }
            }

            var sorted = new Dictionary<InventoryCategory,List<InventoryItem>>();
            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                if (!sorted.ContainsKey(inventoryCategory))
                {
                    sorted.Add(inventoryCategory, new List<InventoryItem>());
                }
                for (var index = 0; index < items.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
                    newItem.SortedContainer = InventoryType.Armoire;
                    newItem.SortedCategory = InventoryCategory.Armoire;
                    newItem.Container = InventoryType.Armoire;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted[inventoryCategory].Add(newItem);
                }
            }

            foreach (var category in sorted)
            {
                newInventories[Service.ClientState.LocalContentId]
                    .Add(category.Key, category.Value);
            }
        }
        private void GenerateCurrencyInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Currency);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    return;
                }
            }

            var sorted = new Dictionary<InventoryCategory,List<InventoryItem>>();
            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                if (!sorted.ContainsKey(inventoryCategory))
                {
                    sorted.Add(inventoryCategory, new List<InventoryItem>());
                }
                for (var index = 0; index < items.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
                    newItem.SortedContainer = inventoryType.Convert();
                    newItem.SortedCategory = inventoryCategory;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted[inventoryCategory].Add(newItem);
                }
            }
            
            foreach (var category in sorted)
            {
                newInventories[Service.ClientState.LocalContentId]
                    .Add(category.Key, category.Value);
            }
        }
        private void GenerateCrystalInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Crystals);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    return;
                }
            }

            var sorted = new Dictionary<InventoryCategory,List<InventoryItem>>();
            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                if (!sorted.ContainsKey(inventoryCategory))
                {
                    sorted.Add(inventoryCategory, new List<InventoryItem>());
                }
                for (var index = 0; index < items.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
                    newItem.SortedContainer = inventoryType.Convert();
                    newItem.SortedCategory = inventoryCategory;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    newItem.SortedSlotIndex = newItem.Slot;
                    sorted[inventoryCategory].Add(newItem);
                }
            }
            
            foreach (var category in sorted)
            {
                newInventories[Service.ClientState.LocalContentId]
                    .Add(category.Key, category.Value);
            }
        }
        
        private void GenerateGlamourInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( (FFXIVClientStructs.FFXIV.Client.Game.InventoryType)2501);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    PluginLog.Verbose("in memory does not contain glamour");
                    return;
                }
            }

            var sorted = new Dictionary<InventoryCategory,List<InventoryItem>>();
            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = InventoryCategory.GlamourChest;
                if (!sorted.ContainsKey(inventoryCategory))
                {
                    sorted.Add(inventoryCategory, new List<InventoryItem>());
                }
                for (var index = 0; index < items.Length; index++)
                {
                    var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
                    newItem.SortedSlotIndex = items[index].Spiritbond;
                    newItem.Spiritbond = 0;
                    newItem.SortedContainer = InventoryType.GlamourChest;
                    newItem.SortedCategory = inventoryCategory;
                    newItem.RetainerId = Service.ClientState.LocalContentId;
                    sorted[inventoryCategory].Add(newItem);
                }
            }
            
            foreach (var category in sorted)
            {
                newInventories[Service.ClientState.LocalContentId]
                    .Add(category.Key, category.Value);
            }
        }


        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                _characterMonitor.OnCharacterRemoved -= CharacterMonitorOnOnCharacterRemoved;
            }
            _disposed = true;         
        }
        
        ~InventoryMonitor()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }

        public struct ItemChanges
        {
            public List<ItemChangesItem> NewItems;
            public List<ItemChangesItem> RemovedItems;
        }

        public struct ItemChangesItem
        {
            public int Quantity;
            public uint ItemId;
            public ulong CharacterId;
            public ItemFlags Flags;
            public DateTime Date;
        }
    }
}