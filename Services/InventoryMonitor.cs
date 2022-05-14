using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services.Ui;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Network;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services
{
    public class InventoryMonitor : IDisposable
    {
        public delegate void InventoryChangedDelegate(
            Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories, ItemChanges changedItems);

        private IEnumerable<InventoryItem> _allItems;
        private CharacterMonitor _characterMonitor;
        private HashSet<InventoryType> _conditionalInventories = new(){InventoryType.RetainerBag0, InventoryType.RetainerMarket, InventoryType.PremiumSaddleBag0, InventoryType.FreeCompanyBag0, InventoryType.FreeCompanyBag1, InventoryType.FreeCompanyBag2, InventoryType.FreeCompanyBag3, InventoryType.FreeCompanyBag4};
        private GameUiManager _gameUiManager;
        private Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> _inventories;
        private Dictionary<int, int> _itemCounts = new();
        private Dictionary<InventoryType, bool> _loadedInventories;
        private Queue<DateTime> _scheduledUpdates = new ();
        private Dictionary<uint, ItemMarketBoardInfo> _retainerMarketPrices = new();
        private OdrScanner _odrScanner;
        private InventorySortOrder? _sortOrder;

        public InventoryMonitor(OdrScanner scanner,
            CharacterMonitor monitor, GameUiManager gameUiManager)
        {
            _odrScanner = scanner;
            _characterMonitor = monitor;
            _gameUiManager = gameUiManager;

            _inventories = new Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>();
            _allItems = new List<InventoryItem>();
            _loadedInventories = new Dictionary<InventoryType, bool>();
            
            _gameUiManager.WatchWindowState(WindowName.RetainerSellList);
            _gameUiManager.WatchWindowState(WindowName.MiragePrismPrismBox);
            _gameUiManager.WatchWindowState(WindowName.CabinetWithdraw);

            Service.Network.NetworkMessage += OnNetworkMessage;
            _odrScanner.OnSortOrderChanged += ReaderOnOnSortOrderChanged;
            _characterMonitor.OnActiveRetainerLoaded += CharacterMonitorOnOnActiveRetainerChanged;
            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            _characterMonitor.OnCharacterRemoved += CharacterMonitorOnOnCharacterRemoved;
            _gameUiManager.UiVisibilityChanged += GameUiManagerOnUiManagerVisibilityChanged;
            Service.Framework.Update += FrameworkOnUpdate;
        }

        private void CharacterMonitorOnOnCharacterRemoved(ulong characterId)
        {
            if (_inventories.ContainsKey(characterId))
            {
                foreach (var inventory in _inventories[characterId])
                {
                    inventory.Value.Clear();
                }
                OnInventoryChanged?.Invoke(_inventories, new ItemChanges() { NewItems = new List<ItemChangesItem>(), RemovedItems = new List<ItemChangesItem>()});
            }
        }

        public bool IsDead { get; set; }
        public Dictionary<InventoryType, bool> LoadedInventories => _loadedInventories;

        public Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> Inventories => _inventories;

        public IEnumerable<InventoryItem> AllItems => _allItems;

        public void Dispose()
        {
            Dispose(true);
        }

        public event InventoryChangedDelegate? OnInventoryChanged;

        private void CharacterMonitorOnOnActiveRetainerChanged(ulong retainerid)
        {
            if (retainerid == 0)
            {
                RemoveLoadedInventory(InventoryType.RetainerBag0);
                RemoveLoadedInventory(InventoryType.RetainerMarket);
                _retainerMarketPrices.Clear();
            }
            else
            {
                _scheduledUpdates.Enqueue(Service.Framework.LastUpdate);
            }
        }

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
                foreach (var inventory in _inventories[characterId])
                {
                    inventory.Value.Clear();
                }
                OnInventoryChanged?.Invoke(_inventories, new ItemChanges() { NewItems = new List<ItemChangesItem>(), RemovedItems = new List<ItemChangesItem>()});
            }
        }

        public void RemoveLoadedInventory(InventoryType inventoryType)
        {
            if (_loadedInventories.ContainsKey(inventoryType))
            {
                _loadedInventories.Remove(inventoryType);
            }
        }

        private void CharacterMonitorOnOnCharacterUpdated(Character? character)
        {
            LoadedInventories.Clear();
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (_scheduledUpdates.Count != 0)
            {
                if (_scheduledUpdates.Peek() <= framework.LastUpdate)
                {
                    if (_scheduledUpdates.Count != 1)
                    {
                        var lastItem = _scheduledUpdates.Last();
                        _scheduledUpdates.Clear();
                        _scheduledUpdates.Enqueue(lastItem);
                    }
                    else
                    {
                        _scheduledUpdates.Dequeue();
                    }
                    GenerateInventories(InventoryGenerateReason.ScheduledUpdate);
                }
            }
        }

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (opcode == Utils.GetOpcode("InventoryActionAck") && direction == NetworkMessageDirection.ZoneDown)
            {
                _scheduledUpdates.Enqueue(Service.Framework.LastUpdate.AddSeconds(1));
            }
            if (opcode == Utils.GetOpcode("ContainerInfo") && direction == NetworkMessageDirection.ZoneDown)
            {
                var decodedContainer = NetworkDecoder.DecodeContainerInfo(dataptr);
                var container = (InventoryType) decodedContainer.containerId;
                if (!LoadedInventories.ContainsKey(container) && _conditionalInventories.Contains(container))
                {
                    LoadedInventories[container] = true;
                    _scheduledUpdates.Enqueue(Service.Framework.LastUpdate.AddSeconds(1));
                }
            }
            if (opcode == Utils.GetOpcode("ItemMarketBoardInfo") && direction == NetworkMessageDirection.ZoneDown)
            {
                var decodeItemMarketBoardInfo = NetworkDecoder.DecodeItemMarketBoardInfo(dataptr);
                if (decodeItemMarketBoardInfo.containerId == (uint) InventoryType.RetainerMarket)
                {
                    _retainerMarketPrices[decodeItemMarketBoardInfo.slot] = decodeItemMarketBoardInfo;
                }
                else
                {
                    PluginLog.Debug("Container ID does not match");
                }
            }
            
        }

        private void GameUiManagerOnUiManagerVisibilityChanged(WindowName windowName, bool? isWindowVisible)
        {
            if (windowName == WindowName.RetainerSellList && isWindowVisible.HasValue && isWindowVisible.Value)
            {
                LoadedInventories[InventoryType.RetainerMarket] = true;
                GenerateInventories(InventoryGenerateReason.WindowOpened);
            }
            if (windowName == WindowName.InventoryBuddy && isWindowVisible.HasValue && isWindowVisible.Value)
            {
                PluginLog.Verbose("InventoryMonitor: Chocobo saddle bag opened, generating inventories");
                _loadedInventories[InventoryType.SaddleBag0] = true;
                _loadedInventories[InventoryType.PremiumSaddleBag0] = true;
                GenerateInventories(InventoryGenerateReason.WindowOpened);
            }
            if (windowName == WindowName.InventoryBuddy && isWindowVisible.HasValue && !isWindowVisible.Value)
            {
                _loadedInventories[InventoryType.SaddleBag0] = false;
                _loadedInventories[InventoryType.PremiumSaddleBag0] = false;
            }
            if (windowName == WindowName.InventoryBuddy2 && isWindowVisible.HasValue && isWindowVisible.Value)
            {
                PluginLog.Verbose("InventoryMonitor: Chocobo saddle bag opened, generating inventories");
                _loadedInventories[InventoryType.SaddleBag0] = true;
                _loadedInventories[InventoryType.PremiumSaddleBag0] = true;
                GenerateInventories(InventoryGenerateReason.WindowOpened);
            }
            if (windowName == WindowName.InventoryBuddy2 && isWindowVisible.HasValue && !isWindowVisible.Value)
            {
                _loadedInventories[InventoryType.SaddleBag0] = false;
                _loadedInventories[InventoryType.PremiumSaddleBag0] = false;
            }
            if (windowName == WindowName.MiragePrismPrismBox && isWindowVisible.HasValue && isWindowVisible.Value)
            {
                _scheduledUpdates.Enqueue(DateTime.Now.AddSeconds(1));
            }
            if (windowName == WindowName.CabinetWithdraw && isWindowVisible.HasValue && isWindowVisible.Value)
            {
                _scheduledUpdates.Enqueue(DateTime.Now.AddSeconds(1));
            }
        }

        public void LoadExistingData(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories)
        {
            LoadedInventories.Clear();
            if (inventories.ContainsKey(0))
            {
                inventories.Remove(0);
            }
            _inventories = inventories;
            GenerateAllItems();
            OnInventoryChanged?.Invoke(_inventories, new ItemChanges() { NewItems = new(), RemovedItems = new()});
        }

        private void GenerateItemCounts()
        {
            var itemCounts = new Dictionary<int, int>();
            foreach (var inventory in _inventories)
            {
                foreach (var itemList in inventory.Value.Values)
                {
                    foreach (var item in itemList)
                    {
                        var hashCode = item.GetHashCode();
                        if (!itemCounts.ContainsKey(hashCode))
                        {
                            itemCounts[hashCode] = 0;
                        }

                        itemCounts[hashCode] += (int)item.Quantity;

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

        private ItemChangesItem ConvertHashedItem(int itemHash, int quantity)
        {
            if (itemHash >= (int)ItemFlags.Collectible * 100000)
            {
                return new ItemChangesItem()
                {
                    ItemId = itemHash - (int) ItemFlags.Collectible * 100000, Flags = ItemFlags.Collectible,
                    Quantity = quantity,
                    Date = DateTime.Now
                };
            }
            if (itemHash >= (int)ItemFlags.HQ * 100000)
            {
                return new ItemChangesItem()
                {
                    ItemId = itemHash - (int) ItemFlags.HQ * 100000, Flags = ItemFlags.HQ,
                    Quantity = quantity,
                    Date = DateTime.Now
                };
            }
            return new ItemChangesItem()
            {
                ItemId = itemHash, Flags = ItemFlags.None,
                Quantity = quantity,
                Date = DateTime.Now
            };
        }

        private ItemChanges CompareItemCounts(Dictionary<int, int> oldItemCounts, Dictionary<int, int> newItemCounts)
        {
            Dictionary<int, int> newItems = new();
            Dictionary<int, int> removedItems = new();
            DiffDictionaries(oldItemCounts, newItemCounts, newItems, removedItems);
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
            SortOrderChanged,
            InventoryChanged,
            ScheduledUpdate,
            NetworkUpdate,
            WindowOpened,
        }

        private unsafe void GenerateInventories(InventoryGenerateReason generateReason)
        {
            if (Service.ClientState.LocalContentId == 0)
            {
                return;
            }
            if (_sortOrder == null)
            {
                _odrScanner.RequestParseOdr();
            }

            GenerateItemCounts();
            
            if (_sortOrder != null)
            {
                var newInventories = new Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>();
                newInventories.Add(Service.ClientState.LocalContentId,
                    new Dictionary<InventoryCategory, List<InventoryItem>>());
                var currentSortOrder = _sortOrder.Value;

                GenerateCharacterInventories(currentSortOrder, newInventories);
                GenerateSaddleInventories(currentSortOrder, newInventories);
                GenerateArmouryChestInventories(currentSortOrder, newInventories);
                GenerateEquippedItems(newInventories);
                GenerateFreeCompanyInventories(newInventories);
                GenerateRetainerInventories(currentSortOrder, newInventories);
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
                        PluginLog.Verbose("Managed to parse " + invDict.Key.ToString() + " for " + newInventory.Key);

                        _inventories[newInventory.Key][invDict.Key] = invDict.Value;
                    }
                }
                
                var oldItemCounts = _itemCounts;
                GenerateItemCounts();
                var newItemCounts = _itemCounts;
                var itemChanges = CompareItemCounts(oldItemCounts, newItemCounts);
                GenerateAllItems();
                OnInventoryChanged?.Invoke(_inventories, itemChanges);
            }

        }

        private unsafe void GenerateCharacterInventories(InventorySortOrder currentSortOrder, Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            //Actual inventories
            var bag0 = GameInterface.GetContainer(InventoryType.Bag0);
            var bag1 = GameInterface.GetContainer(InventoryType.Bag1);
            var bag2 = GameInterface.GetContainer(InventoryType.Bag2);
            var bag3 = GameInterface.GetContainer(InventoryType.Bag3);

            //Sort ordering
            if (currentSortOrder.NormalInventories.ContainsKey("PlayerInventory"))
            {
                var sortedBag0 = new List<InventoryItem>();
                var sortedBag1 = new List<InventoryItem>();
                var sortedBag2 = new List<InventoryItem>();
                var sortedBag3 = new List<InventoryItem>();
                var playerInventorySort = currentSortOrder.NormalInventories["PlayerInventory"];
                if (bag0 != null && bag1 != null && bag2 != null && bag3 != null)
                {
                    for (var index = 0; index < playerInventorySort.Count; index++)
                    {
                        var sort = playerInventorySort[index];
                        MemoryInventoryContainer* currentBag;
                        switch (sort.containerIndex)
                        {
                            case 0:
                                currentBag = bag0;
                                break;
                            case 1:
                                currentBag = bag1;
                                break;
                            case 2:
                                currentBag = bag2;
                                break;
                            case 3:
                                currentBag = bag3;
                                break;
                            default:
                                continue;
                        }

                        if (sort.slotIndex >= currentBag->SlotCount)
                        {
                            PluginLog.Verbose("bag was too big UwU");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            List<InventoryItem> currentSortBag;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    currentSortBag = sortedBag0;
                                    break;
                                case 1:
                                    currentSortBag = sortedBag1;
                                    break;
                                case 2:
                                    currentSortBag = sortedBag2;
                                    break;
                                case 3:
                                    currentSortBag = sortedBag3;
                                    break;
                                default:
                                    continue;
                            }

                            currentSortBag.Add(
                                InventoryItem.FromMemoryInventoryItem(currentBag->Items[sort.slotIndex]));
                        }
                    }


                    for (var index = 0; index < sortedBag0.Count; index++)
                    {
                        sortedBag0[index].SortedContainer = InventoryType.Bag0;
                        sortedBag0[index].SortedCategory = InventoryCategory.CharacterBags;
                        sortedBag0[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedBag0[index].SortedSlotIndex = index;
                    }

                    for (var index = 0; index < sortedBag1.Count; index++)
                    {
                        sortedBag1[index].SortedContainer = InventoryType.Bag1;
                        sortedBag1[index].SortedCategory = InventoryCategory.CharacterBags;
                        sortedBag1[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedBag1[index].SortedSlotIndex = index;
                    }

                    for (var index = 0; index < sortedBag2.Count; index++)
                    {
                        sortedBag2[index].SortedContainer = InventoryType.Bag2;
                        sortedBag2[index].SortedCategory = InventoryCategory.CharacterBags;
                        sortedBag2[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedBag2[index].SortedSlotIndex = index;
                    }

                    for (var index = 0; index < sortedBag3.Count; index++)
                    {
                        sortedBag3[index].SortedContainer = InventoryType.Bag3;
                        sortedBag3[index].SortedCategory = InventoryCategory.CharacterBags;
                        sortedBag3[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedBag3[index].SortedSlotIndex = index;
                    }


                    var mainBag = sortedBag0;
                    mainBag.AddRange(sortedBag1);
                    mainBag.AddRange(sortedBag2);
                    mainBag.AddRange(sortedBag3);
                    newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.CharacterBags, mainBag);
                }
            }
        }

        private unsafe void GenerateSaddleInventories(InventorySortOrder currentSortOrder, Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            if (currentSortOrder.NormalInventories.ContainsKey("SaddleBag"))
            {
                var saddleBag0 = GameInterface.GetContainer(InventoryType.SaddleBag0);
                var saddleBag1 = GameInterface.GetContainer(InventoryType.SaddleBag1);
                var saddleBagLeftSort = currentSortOrder.NormalInventories["SaddleBag"];

                //Fully sorted bags
                var sortedSaddleBagLeft = new List<InventoryItem>();
                var sortedSaddleBagRight = new List<InventoryItem>();


                if (saddleBag0 != null && saddleBag1 != null && LoadedInventories.ContainsKey(InventoryType.SaddleBag0) &&
                    LoadedInventories[InventoryType.SaddleBag0])
                {
                    PluginLog.Verbose("Saddle bag sort count: " + saddleBagLeftSort.Count);
                    for (var index = 0; index < saddleBagLeftSort.Count; index++)
                    {
                        var sort = saddleBagLeftSort[index];

                        MemoryInventoryContainer* currentBag;
                        switch (sort.containerIndex)
                        {
                            case 0:
                                currentBag = saddleBag0;
                                break;
                            case 1:
                                currentBag = saddleBag1;
                                break;
                            default:
                                continue;
                        }

                        if (sort.slotIndex >= currentBag->SlotCount)
                        {
                            PluginLog.Verbose("bag was too big UwU");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            List<InventoryItem> currentSortBag;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    currentSortBag = sortedSaddleBagLeft;
                                    break;
                                case 1:
                                    currentSortBag = sortedSaddleBagRight;
                                    break;
                                default:
                                    continue;
                            }

                            currentSortBag.Add(
                                InventoryItem.FromMemoryInventoryItem(currentBag->Items[sort.slotIndex]));
                        }
                    }

                    for (var index = 0; index < sortedSaddleBagLeft.Count; index++)
                    {
                        sortedSaddleBagLeft[index].SortedContainer = InventoryType.SaddleBag0;
                        sortedSaddleBagLeft[index].SortedCategory = InventoryCategory.CharacterSaddleBags;
                        sortedSaddleBagLeft[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedSaddleBagLeft[index].SortedSlotIndex = index;
                    }

                    for (var index = 0; index < sortedSaddleBagRight.Count; index++)
                    {
                        sortedSaddleBagRight[index].SortedContainer = InventoryType.SaddleBag1;
                        sortedSaddleBagRight[index].SortedCategory = InventoryCategory.CharacterSaddleBags;
                        sortedSaddleBagRight[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedSaddleBagRight[index].SortedSlotIndex = index;
                    }

                    var saddleBags = sortedSaddleBagLeft;
                    saddleBags.AddRange(sortedSaddleBagRight);
                    newInventories[Service.ClientState.LocalContentId]
                        .Add(InventoryCategory.CharacterSaddleBags, saddleBags);
                }
            }

            if (currentSortOrder.NormalInventories.ContainsKey("SaddleBagPremium"))
            {
                var premiumSaddleBag0 = GameInterface.GetContainer(InventoryType.PremiumSaddleBag0);
                var premiumSaddleBag1 = GameInterface.GetContainer(InventoryType.PremiumSaddleBag1);
                var saddleBagPremiumSort = currentSortOrder.NormalInventories["SaddleBagPremium"];

                //Fully sorted bags
                var sortedPremiumSaddleBagLeft = new List<InventoryItem>();
                var sortedPremiumSaddleBagRight = new List<InventoryItem>();


                if (premiumSaddleBag0 != null && premiumSaddleBag1 != null &&
                    LoadedInventories.ContainsKey(InventoryType.SaddleBag0) &&
                    LoadedInventories[InventoryType.PremiumSaddleBag0])
                {
                    PluginLog.Verbose("Saddle bag sort count: " + saddleBagPremiumSort.Count);
                    for (var index = 0; index < saddleBagPremiumSort.Count; index++)
                    {
                        var sort = saddleBagPremiumSort[index];

                        MemoryInventoryContainer* currentBag;
                        switch (sort.containerIndex)
                        {
                            case 0:
                                currentBag = premiumSaddleBag0;
                                break;
                            case 1:
                                currentBag = premiumSaddleBag1;
                                break;
                            default:
                                continue;
                        }

                        if (sort.slotIndex >= currentBag->SlotCount)
                        {
                            PluginLog.Verbose("bag was too big UwU");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            List<InventoryItem> currentSortBag;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    currentSortBag = sortedPremiumSaddleBagLeft;
                                    break;
                                case 1:
                                    currentSortBag = sortedPremiumSaddleBagRight;
                                    break;
                                default:
                                    continue;
                            }

                            currentSortBag.Add(
                                InventoryItem.FromMemoryInventoryItem(currentBag->Items[sort.slotIndex]));
                        }
                    }

                    for (var index = 0; index < sortedPremiumSaddleBagLeft.Count; index++)
                    {
                        sortedPremiumSaddleBagLeft[index].SortedContainer = InventoryType.PremiumSaddleBag0;
                        sortedPremiumSaddleBagLeft[index].SortedCategory = InventoryCategory.CharacterPremiumSaddleBags;
                        sortedPremiumSaddleBagLeft[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedPremiumSaddleBagLeft[index].SortedSlotIndex = index;
                    }

                    for (var index = 0; index < sortedPremiumSaddleBagRight.Count; index++)
                    {
                        sortedPremiumSaddleBagRight[index].SortedContainer = InventoryType.PremiumSaddleBag1;
                        sortedPremiumSaddleBagRight[index].SortedCategory = InventoryCategory.CharacterPremiumSaddleBags;
                        sortedPremiumSaddleBagRight[index].RetainerId = Service.ClientState.LocalContentId;
                        sortedPremiumSaddleBagRight[index].SortedSlotIndex = index;
                    }

                    var saddleBags = sortedPremiumSaddleBagLeft;
                    saddleBags.AddRange(sortedPremiumSaddleBagRight);
                    newInventories[Service.ClientState.LocalContentId]
                        .Add(InventoryCategory.CharacterPremiumSaddleBags, saddleBags);
                }
            }
        }

        private unsafe void GenerateArmouryChestInventories(InventorySortOrder currentSortOrder, Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var allArmoryItems = new List<InventoryItem>();

            Dictionary<string, InventoryType> inventoryTypes = new Dictionary<string, InventoryType>();
            inventoryTypes.Add("ArmouryMainHand", InventoryType.ArmoryMain);
            inventoryTypes.Add("ArmouryHead", InventoryType.ArmoryHead);
            inventoryTypes.Add("ArmouryBody", InventoryType.ArmoryBody);
            inventoryTypes.Add("ArmouryHands", InventoryType.ArmoryHand);
            inventoryTypes.Add("ArmouryLegs", InventoryType.ArmoryLegs);
            inventoryTypes.Add("ArmouryFeet", InventoryType.ArmoryFeet);
            inventoryTypes.Add("ArmouryOffHand", InventoryType.ArmoryOff);
            inventoryTypes.Add("ArmouryEars", InventoryType.ArmoryEar);
            inventoryTypes.Add("ArmouryNeck", InventoryType.ArmoryNeck);
            inventoryTypes.Add("ArmouryWrists", InventoryType.ArmoryWrist);
            inventoryTypes.Add("ArmouryRings", InventoryType.ArmoryRing);
            inventoryTypes.Add("ArmourySoulCrystal", InventoryType.ArmorySoulCrystal);

            foreach (var armoryChest in inventoryTypes)
            {
                var armoryItems = new List<InventoryItem>();
                if (currentSortOrder.NormalInventories.ContainsKey(armoryChest.Key))
                {
                    var odrOrdering = currentSortOrder.NormalInventories[armoryChest.Key];
                    var gameOrdering = GameInterface.GetContainer(armoryChest.Value);


                    if (gameOrdering != null && gameOrdering->Loaded != 0)
                    {
                        for (var index = 0; index < odrOrdering.Count; index++)
                        {
                            var sort = odrOrdering[index];

                            if (sort.slotIndex >= gameOrdering->SlotCount)
                            {
                                PluginLog.Verbose("bag was too big UwU");
                            }
                            else
                            {
                                armoryItems.Add(InventoryItem.FromMemoryInventoryItem(gameOrdering->Items[sort.slotIndex]));
                            }
                        }


                        for (var index = 0; index < armoryItems.Count; index++)
                        {
                            armoryItems[index].SortedContainer = armoryChest.Value;
                            armoryItems[index].SortedCategory = InventoryCategory.CharacterArmoryChest;
                            armoryItems[index].RetainerId = Service.ClientState.LocalContentId;
                            armoryItems[index].SortedSlotIndex = index;
                        }

                        allArmoryItems.AddRange(armoryItems);
                    }
                    else
                    {
                        PluginLog.Verbose("Could generate data for " + armoryChest.Value);
                    }
                }
                else
                {
                    PluginLog.Verbose("Could not find sort order for" + armoryChest.Value);
                }
            }

            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.CharacterArmoryChest, allArmoryItems);
        }

        private unsafe void GenerateEquippedItems(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var equippedItems = new List<InventoryItem>();

            var gearSet0 = GameInterface.GetContainer(InventoryType.GearSet0);
            if (gearSet0->Loaded != 0)
            {
                for (int i = 0; i < gearSet0->SlotCount; i++)
                {
                    var memoryInventoryItem = InventoryItem.FromMemoryInventoryItem(gearSet0->Items[i]);
                    memoryInventoryItem.SortedContainer = InventoryType.GearSet0;
                    memoryInventoryItem.SortedCategory = InventoryCategory.CharacterEquipped;
                    memoryInventoryItem.RetainerId = Service.ClientState.LocalContentId;
                    memoryInventoryItem.SortedSlotIndex = i;
                    equippedItems.Add(memoryInventoryItem);
                }
            }

            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.CharacterEquipped, equippedItems);
        }

        private unsafe void GenerateFreeCompanyInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var freeCompanyItems = _inventories.ContainsKey(Service.ClientState.LocalContentId)
                ? _inventories[Service.ClientState.LocalContentId].ContainsKey(InventoryCategory.FreeCompanyBags)
                    ? _inventories[Service.ClientState.LocalContentId][InventoryCategory.FreeCompanyBags].ToList()
                    : new List<InventoryItem>()
                : new List<InventoryItem>();

            var bags = new InventoryType[]
            {
                InventoryType.FreeCompanyBag0, InventoryType.FreeCompanyBag1, InventoryType.FreeCompanyBag2,
                InventoryType.FreeCompanyBag3, InventoryType.FreeCompanyBag4, InventoryType.FreeCompanyGil
            };

            for (int b = 0; b < bags.Length; b++)
            {
                var bagType = bags[b];
                if (LoadedInventories.ContainsKey(bagType) && LoadedInventories[bagType])
                {
                    freeCompanyItems.RemoveAll(c => c.Container == bagType);
                    var bag = GameInterface.GetContainer(bagType);
                    if (bag->Loaded != 0)
                    {
                        for (int i = 0; i < bag->SlotCount; i++)
                        {
                            var memoryInventoryItem = InventoryItem.FromMemoryInventoryItem(bag->Items[i]);
                            memoryInventoryItem.SortedContainer = bagType;
                            memoryInventoryItem.SortedCategory = bagType == InventoryType.FreeCompanyGil ? InventoryCategory.Currency : InventoryCategory.FreeCompanyBags;
                            memoryInventoryItem.RetainerId = Service.ClientState.LocalContentId;
                            memoryInventoryItem.SortedSlotIndex = i;
                            freeCompanyItems.Add(memoryInventoryItem);
                        }
                    }
                }
            }

            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.FreeCompanyBags, freeCompanyItems);
        }
        private unsafe void GenerateRetainerInventories(InventorySortOrder currentSortOrder, Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var currentRetainer = _characterMonitor.ActiveRetainer;
            if (currentRetainer != 0 && _characterMonitor.IsRetainerLoaded)
            {
                if (currentSortOrder.RetainerInventories.ContainsKey(currentRetainer))
                {
                    //Actual inventories
                    var retainerBag0 = GameInterface.GetContainer(InventoryType.RetainerBag0);
                    var retainerBag1 = GameInterface.GetContainer(InventoryType.RetainerBag1);
                    var retainerBag2 = GameInterface.GetContainer(InventoryType.RetainerBag2);
                    var retainerBag3 = GameInterface.GetContainer(InventoryType.RetainerBag3);
                    var retainerBag4 = GameInterface.GetContainer(InventoryType.RetainerBag4);
                    var retainerBag5 = GameInterface.GetContainer(InventoryType.RetainerBag5);
                    var retainerBag6 = GameInterface.GetContainer(InventoryType.RetainerBag6);
                    var retainerEquippedItems = GameInterface.GetContainer(InventoryType.RetainerEquippedGear);
                    var retainerMarketItems = GameInterface.GetContainer(InventoryType.RetainerMarket);
                    var retainerGil = GameInterface.GetContainer(InventoryType.RetainerGil);
                    var retainerCrystal = GameInterface.GetContainer(InventoryType.RetainerCrystal);


                    //Sort ordering
                    var retainerInventory = currentSortOrder.RetainerInventories[currentRetainer];

                    //Fully sorted bags
                    var sortedRetainerBag0 = new List<InventoryItem>();
                    var sortedRetainerBag1 = new List<InventoryItem>();
                    var sortedRetainerBag2 = new List<InventoryItem>();
                    var sortedRetainerBag3 = new List<InventoryItem>();
                    var sortedRetainerBag4 = new List<InventoryItem>();
                    var sortedRetainerBag5 = new List<InventoryItem>();
                    var sortedRetainerBag6 = new List<InventoryItem>();
                    var retainerEquipment = new List<InventoryItem>();
                    var retainerMarket = new List<InventoryItem>();

                    if (retainerBag0 != null && retainerBag1 != null && retainerBag2 != null &&
                        retainerBag3 != null && retainerBag4 != null && retainerBag5 != null &&
                        retainerBag6 != null && retainerEquippedItems != null)
                    {
                        for (var index = 0; index < retainerEquippedItems->SlotCount; index++)
                        {
                            var memoryInventoryItem =
                                InventoryItem.FromMemoryInventoryItem(retainerEquippedItems->Items[index]);
                            memoryInventoryItem.SortedContainer = InventoryType.RetainerEquippedGear;
                            memoryInventoryItem.SortedCategory = InventoryCategory.RetainerEquipped;
                            memoryInventoryItem.RetainerId = currentRetainer;
                            memoryInventoryItem.SortedSlotIndex = index;
                            retainerEquipment.Add(memoryInventoryItem);
                        }

                        if (retainerMarketItems != null)
                        {
                            for (var index = 0; index < retainerMarketItems->SlotCount; index++)
                            {
                                var memoryInventoryItem =
                                    InventoryItem.FromMemoryInventoryItem(retainerMarketItems->Items[index]);
                                memoryInventoryItem.SortedContainer = InventoryType.RetainerMarket;
                                memoryInventoryItem.SortedCategory = InventoryCategory.RetainerMarket;
                                memoryInventoryItem.RetainerId = currentRetainer;
                                memoryInventoryItem.SortedSlotIndex = memoryInventoryItem.Slot;
                                var retainerSlotIndex = (uint) memoryInventoryItem.Slot;
                                if (_retainerMarketPrices.ContainsKey(retainerSlotIndex))
                                {
                                    memoryInventoryItem.RetainerMarketPrice =
                                        _retainerMarketPrices[retainerSlotIndex].unitPrice;
                                    _retainerMarketPrices.Remove(retainerSlotIndex);
                                }
                                else
                                {
                                    PluginLog.Debug("Market prices do not match");
                                }

                                retainerMarket.Add(memoryInventoryItem);
                            }
                        }

                        for (var index = 0; index < retainerInventory.InventoryCoords.Count; index++)
                        {
                            var sort = retainerInventory.InventoryCoords[index];
                            MemoryInventoryContainer* currentBag;
                            switch (sort.containerIndex)
                            {
                                case 0:
                                    currentBag = retainerBag0;
                                    break;
                                case 1:
                                    currentBag = retainerBag1;
                                    break;
                                case 2:
                                    currentBag = retainerBag2;
                                    break;
                                case 3:
                                    currentBag = retainerBag3;
                                    break;
                                case 4:
                                    currentBag = retainerBag4;
                                    break;
                                case 5:
                                    currentBag = retainerBag5;
                                    break;
                                case 6:
                                    currentBag = retainerBag6;
                                    break;
                                default:
                                    continue;
                            }

                            if (sort.slotIndex >= currentBag->SlotCount)
                            {
                                PluginLog.Verbose("bag was too big UwU");
                            }
                            else
                            {
                                var sortedBagIndex = index / 25;
                                List<InventoryItem> currentSortBag;
                                switch (sortedBagIndex)
                                {
                                    case 0:
                                        currentSortBag = sortedRetainerBag0;
                                        break;
                                    case 1:
                                        currentSortBag = sortedRetainerBag1;
                                        break;
                                    case 2:
                                        currentSortBag = sortedRetainerBag2;
                                        break;
                                    case 3:
                                        currentSortBag = sortedRetainerBag3;
                                        break;
                                    case 4:
                                        currentSortBag = sortedRetainerBag4;
                                        break;
                                    case 5:
                                        currentSortBag = sortedRetainerBag5;
                                        break;
                                    case 6:
                                        currentSortBag = sortedRetainerBag6;
                                        break;
                                    default:
                                        continue;
                                }

                                currentSortBag.Add(
                                    InventoryItem.FromMemoryInventoryItem(currentBag->Items[sort.slotIndex]));
                            }
                        }

                        var retainerBags = new List<InventoryType>
                        {
                            InventoryType.RetainerBag0, InventoryType.RetainerBag1, InventoryType.RetainerBag2,
                            InventoryType.RetainerBag3, InventoryType.RetainerBag4, InventoryType.RetainerBag5,
                            InventoryType.RetainerBag6
                        };
                        var absoluteIndex = 0;
                        for (var index = 0; index < sortedRetainerBag0.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                            {
                                if (retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag0[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag0[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag0[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag0[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }

                        for (var index = 0; index < sortedRetainerBag1.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                            {
                                if (retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag1[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag1[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag1[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag1[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }

                        for (var index = 0; index < sortedRetainerBag2.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                            {
                                if (retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag2[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag2[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag2[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag2[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }

                        for (var index = 0; index < sortedRetainerBag3.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                            {
                                if (retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag3[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag3[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag3[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag3[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }

                        for (var index = 0; index < sortedRetainerBag4.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                            {
                                if (retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag4[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag4[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag4[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag4[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }

                        for (var index = 0; index < sortedRetainerBag5.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (index >= 0 && sortedRetainerBag5.Count > index)
                            {
                                if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag5[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag5[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag5[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag5[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }

                        for (var index = 0; index < sortedRetainerBag6.Count; index++)
                        {
                            var sortedBagIndex = absoluteIndex / 35;
                            if (index >= 0 && sortedRetainerBag6.Count > index)
                            {
                                if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                {
                                    sortedRetainerBag6[index].SortedContainer = retainerBags[sortedBagIndex];
                                }

                                sortedRetainerBag6[index].SortedCategory = InventoryCategory.RetainerBags;
                                sortedRetainerBag6[index].SortedSlotIndex = absoluteIndex - sortedBagIndex * 35;
                                sortedRetainerBag6[index].RetainerId = currentRetainer;
                            }

                            absoluteIndex++;
                        }
                    }

                    newInventories.Add(currentRetainer, new Dictionary<InventoryCategory, List<InventoryItem>>());
                    var newRetainerBags = sortedRetainerBag0;
                    newRetainerBags.AddRange(sortedRetainerBag1);
                    newRetainerBags.AddRange(sortedRetainerBag2);
                    newRetainerBags.AddRange(sortedRetainerBag3);
                    newRetainerBags.AddRange(sortedRetainerBag4);
                    newRetainerBags.AddRange(sortedRetainerBag5);
                    newRetainerBags.AddRange(sortedRetainerBag6);
                    newInventories[currentRetainer].Add(InventoryCategory.RetainerBags, newRetainerBags);
                    newInventories[currentRetainer].Add(InventoryCategory.RetainerEquipped, retainerEquipment);
                    if (retainerMarketItems != null)
                    {
                        retainerMarket = retainerMarket
                            .OrderBy(c =>
                                c.Item == null ? 0 :c.Item.ItemUICategory.Value?.OrderMajor ?? 0)
                            .ThenBy(c =>
                                c.Item == null ? 0 :c.Item.ItemUICategory.Value?.OrderMinor ?? 0)
                            .ThenBy(c =>
                                c.Item == null ? 0 :c.Item.Unknown19)
                            .ToList();
                        
                        var actualIndex = 0;
                        for (var index = 0; index < retainerMarket.Count; index++)
                        {
                            var item = retainerMarket[index];
                            if (!item.IsEmpty)
                            {
                                item.SortedSlotIndex = actualIndex;
                                actualIndex++;
                            }
                        }

                        newInventories[currentRetainer].Add(InventoryCategory.RetainerMarket, retainerMarket);
                    }

                    if (retainerGil != null)
                    {
                        var sortedRetainerGil = new List<InventoryItem>();

                        for (var index = 0; index < retainerGil->SlotCount; index++)
                        {
                            var memoryInventoryItem =
                                InventoryItem.FromMemoryInventoryItem(retainerGil->Items[index]);
                            memoryInventoryItem.SortedContainer = InventoryType.RetainerGil;
                            memoryInventoryItem.SortedCategory = InventoryCategory.Currency;
                            memoryInventoryItem.RetainerId = currentRetainer;
                            memoryInventoryItem.SortedSlotIndex = index;
                            sortedRetainerGil.Add(memoryInventoryItem);
                        }
                        
                        var actualIndex = 0;
                        for (var index = 0; index < sortedRetainerGil.Count; index++)
                        {
                            var item = sortedRetainerGil[index];
                            if (!item.IsEmpty)
                            {
                                item.SortedSlotIndex = actualIndex;
                                actualIndex++;
                            }
                        }
                        newInventories[currentRetainer].Add(InventoryCategory.Currency, sortedRetainerGil);
                    }

                    if (retainerCrystal != null)
                    {
                        var sortedRetainerCrystal = new List<InventoryItem>();

                        for (var index = 0; index < retainerCrystal->SlotCount; index++)
                        {
                            var memoryInventoryItem =
                                InventoryItem.FromMemoryInventoryItem(retainerCrystal->Items[index]);
                            memoryInventoryItem.SortedContainer = InventoryType.RetainerCrystal;
                            memoryInventoryItem.SortedCategory = InventoryCategory.Crystals;
                            memoryInventoryItem.RetainerId = currentRetainer;
                            memoryInventoryItem.SortedSlotIndex = index;
                            sortedRetainerCrystal.Add(memoryInventoryItem);
                        }
                        
                        var actualIndex = 0;
                        for (var index = 0; index < sortedRetainerCrystal.Count; index++)
                        {
                            var item = sortedRetainerCrystal[index];
                            if (!item.IsEmpty)
                            {
                                item.SortedSlotIndex = actualIndex;
                                actualIndex++;
                            }
                        }
                        newInventories[currentRetainer].Add(InventoryCategory.Crystals, sortedRetainerCrystal);
                    }
                    
                }
                else
                {
                    PluginLog.Verbose("Current retainer has no sort information.");
                }
            }
            else
            {
                PluginLog.Verbose("Attempted to generate retainer inventories while not in a retainer.");
            }
        }
        
        private unsafe void GenerateArmoireInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var list = new List<InventoryItem>();

            if (!GameInterface.ArmoireLoaded)
            {
                return;
            }
             
            int actualIndex = 0;
            uint currentCategory = 0;
            foreach (var row in ExcelCache.GetSheet<Cabinet>().OrderBy(c => c.Category.Row).ThenBy(c => c.Order))
            {
                var itemId = row.Item.Row;
                var index = row.RowId;
                var isInArmoire = GameInterface.IsInArmoire(itemId);
                var potentialIndex = GameInterface.ArmoireIndexIfPresent(itemId);
                var memoryInventoryItem = InventoryItem.FromArmoireItem(isInArmoire ? itemId : 0, (short)index);
                memoryInventoryItem.SortedContainer = InventoryType.Armoire;
                memoryInventoryItem.SortedCategory = InventoryCategory.Armoire;
                memoryInventoryItem.RetainerId = Service.ClientState.LocalContentId;
                memoryInventoryItem.CabCat = row.Category.Value?.Category.Row ?? 0;
                if (memoryInventoryItem.CabCat != currentCategory)
                {
                    actualIndex = 0;
                    currentCategory = memoryInventoryItem.CabCat;
                }
                memoryInventoryItem.SortedSlotIndex = actualIndex;
                if (memoryInventoryItem.ItemId != 0)
                {
                    actualIndex++;
                }
                list.Add(memoryInventoryItem);
            }
            
            PluginLog.Verbose("Finished parsing armoire.");
            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.Armoire, list);
        }
        private unsafe void GenerateCurrencyInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var currencyItems = new List<InventoryItem>();
            var bag = GameInterface.GetContainer(InventoryType.Currency);
            if (bag != null && bag->Loaded != 0)
            {
                for (int i = 0; i < bag->SlotCount; i++)
                {
                    var memoryInventoryItem = InventoryItem.FromMemoryInventoryItem(bag->Items[i]);
                    memoryInventoryItem.SortedContainer = InventoryType.Currency;
                    memoryInventoryItem.SortedCategory = InventoryCategory.Currency;
                    memoryInventoryItem.RetainerId = Service.ClientState.LocalContentId;
                    memoryInventoryItem.SortedSlotIndex = i;
                    currencyItems.Add(memoryInventoryItem);
                }
            }

            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.Currency, currencyItems);
        }
        private unsafe void GenerateCrystalInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var currencyItems = new List<InventoryItem>();
            var bag = GameInterface.GetContainer(InventoryType.Crystal);
            if (bag != null && bag->Loaded != 0)
            {
                for (int i = 0; i < bag->SlotCount; i++)
                {
                    var memoryInventoryItem = InventoryItem.FromMemoryInventoryItem(bag->Items[i]);
                    memoryInventoryItem.SortedContainer = InventoryType.Crystal;
                    memoryInventoryItem.SortedCategory = InventoryCategory.Crystals;
                    memoryInventoryItem.RetainerId = Service.ClientState.LocalContentId;
                    memoryInventoryItem.SortedSlotIndex = i;
                    currencyItems.Add(memoryInventoryItem);
                }
            }

            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.Crystals, currencyItems);
        }
        
        private unsafe void GenerateGlamourInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newInventories)
        {
            var list = new List<InventoryItem>();
            var agents = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule();
            var dresserAgent = agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
            var itemsStart = *(IntPtr*) ((IntPtr) dresserAgent + 0x28);
            if (itemsStart == IntPtr.Zero) {
                return;
            }
            for (var i = 0; i < 400; i++) {
                var glamItem = *(GlamourItem*) (itemsStart + i * 28);
                var memoryInventoryItem = InventoryItem.FromGlamourItem(glamItem);
                memoryInventoryItem.SortedContainer = InventoryType.GlamourChest;
                memoryInventoryItem.SortedCategory = InventoryCategory.GlamourChest;
                memoryInventoryItem.RetainerId = Service.ClientState.LocalContentId;
                memoryInventoryItem.SortedSlotIndex = i;
                list.Add(memoryInventoryItem);
            }
            
            PluginLog.Verbose("Finished parsing glamour chest.");
            newInventories[Service.ClientState.LocalContentId].Add(InventoryCategory.GlamourChest, list);
        }
        
        private void ReaderOnOnSortOrderChanged(InventorySortOrder sortorder)
        {
            _sortOrder = sortorder;
            GenerateInventories(InventoryGenerateReason.SortOrderChanged);
        }


        private void Dispose(bool disposing)
        {
            IsDead = true;
            if (disposing)
            {
                _odrScanner.OnSortOrderChanged -= ReaderOnOnSortOrderChanged;
                _gameUiManager.UiVisibilityChanged -= GameUiManagerOnUiManagerVisibilityChanged;
                Service.Network.NetworkMessage -=OnNetworkMessage;
                Service.Framework.Update -= FrameworkOnUpdate;
                _characterMonitor.OnActiveRetainerLoaded -= CharacterMonitorOnOnActiveRetainerChanged;
                _characterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
                _characterMonitor.OnCharacterRemoved -= CharacterMonitorOnOnCharacterRemoved;
            }
        }

        public struct ItemChanges
        {
            public List<ItemChangesItem> NewItems;
            public List<ItemChangesItem> RemovedItems;
        }

        public struct ItemChangesItem
        {
            public int Quantity;
            public int ItemId;
            public ItemFlags Flags;
            public DateTime Date;
        }
    }
}