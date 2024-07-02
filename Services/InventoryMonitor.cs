using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Models;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.GameStructs;
using Dalamud.Plugin.Services;
using static FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;
using InventoryItem = CriticalCommonLib.Models.InventoryItem;
using InventoryType = CriticalCommonLib.Enums.InventoryType;

namespace CriticalCommonLib.Services
{
    public class InventoryMonitor : IInventoryMonitor
    {
        public delegate void InventoryChangedDelegate(List<InventoryChange> inventoryChanges, ItemChanges? itemChanges = null);

        private IEnumerable<InventoryItem> _allItems;
        private ICharacterMonitor _characterMonitor;
        private Dictionary<ulong, Inventory> _inventories;
        private Dictionary<(uint, ItemFlags, ulong), int> _retainerItemCounts = new();
        private Dictionary<(uint, ItemFlags), int> _itemCounts = new();
        private Dictionary<InventoryType, bool> _loadedInventories;
        private Queue<DateTime> _scheduledUpdates = new ();
        private Dictionary<uint, ItemMarketBoardInfo> _retainerMarketPrices = new();
        private IInventoryScanner _inventoryScanner;
        private ICraftMonitor _craftMonitor;
        private IFramework _frameworkService;

        public InventoryMonitor(ICharacterMonitor monitor, ICraftMonitor craftMonitor, IInventoryScanner scanner, IFramework frameworkService)
        {
            _characterMonitor = monitor;
            _craftMonitor = craftMonitor;
            _inventoryScanner = scanner;
            _frameworkService = frameworkService;

            _inventories = new Dictionary<ulong, Inventory>();
            _allItems = new List<InventoryItem>();
            _loadedInventories = new Dictionary<InventoryType, bool>();

            _inventoryScanner.BagsChanged += InventoryScannerOnBagsChanged;
            _characterMonitor.OnCharacterRemoved += CharacterMonitorOnOnCharacterRemoved;
        }

        private void InventoryScannerOnBagsChanged(List<BagChange> changes)
        {
            Service.Log.Verbose("Bags changed, generating inventory");
            GenerateInventories(InventoryGenerateReason.ScheduledUpdate);
        }

        private void CharacterMonitorOnOnCharacterRemoved(ulong characterId)
        {
            if (_inventories.ContainsKey(characterId))
            {
                ClearCharacterInventories(characterId);
                
                _frameworkService.RunOnFrameworkThread(() =>
                {
                    OnInventoryChanged?.Invoke(new List<InventoryChange>());
                });
            }
        }

        public Dictionary<ulong, Inventory> Inventories => _inventories;

        public IEnumerable<InventoryItem> AllItems => _allItems;
        
        public Dictionary<(uint, ItemFlags, ulong), int> RetainerItemCounts => _retainerItemCounts;
        public Dictionary<(uint, ItemFlags), int> ItemCounts => _itemCounts;

        public event InventoryChangedDelegate? OnInventoryChanged;

        public List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryCategory category)
        {
            if (_inventories.ContainsKey(characterId))
            {
                return _inventories[characterId].GetItemsByCategory(category);
            }

            return new List<InventoryItem>();
        }

        public List<InventoryItem> GetSpecificInventory(ulong characterId, InventoryType inventoryType)
        {
            if (_inventories.ContainsKey(characterId))
            {
                return _inventories[characterId].GetItemsByType(inventoryType);
            }

            return new List<InventoryItem>();
        }

        public void ClearCharacterInventories(ulong characterId)
        {
            if (_inventories.ContainsKey(characterId))
            {
                _inventoryScanner.ClearRetainerCache(characterId);
                _inventories[characterId].ClearInventories();

                _frameworkService.RunOnFrameworkThread(() =>
                {
                    OnInventoryChanged?.Invoke(new List<InventoryChange>());
                });
            }
        }
        public void LoadExistingData(List<InventoryItem> inventories)
        {
            var groupedInventories = inventories.GroupBy(c => c.RetainerId);
            
            foreach (var characterKvp in groupedInventories)
            {
                var characterId = characterKvp.Key;
                var character = _characterMonitor.GetCharacterById(characterId);
                if (character != null)
                {
                    if (!_inventories.ContainsKey(characterId))
                    {
                        _inventories[characterId] = new Inventory(character.CharacterType, characterId);
                    }

                    _inventories[characterId].LoadItems(characterKvp.ToArray(), true);
                }
                else
                {
                    Service.Log.Warning("Could not find character with ID " + characterId + " while trying to load in existing data.");
                }
            }

            FillEmptySlots();
            GenerateAllItems();
            _frameworkService.RunOnFrameworkThread(() =>
            {
                OnInventoryChanged?.Invoke(new List<InventoryChange>());
            });
        }

        public void SignalRefresh()
        {
            _frameworkService.RunOnFrameworkThread(() =>
            {
                OnInventoryChanged?.Invoke(new List<InventoryChange>());
            });
        }

        public bool Started => _started;

        public void Start()
        {
            _started = true;
        }

        public void FillEmptySlots()
        {
            foreach (var inventory in _inventories)
            {
                inventory.Value.FillSlots();
            }
        }

        public void GenerateItemCounts()
        {
            var retainerItemCounts = new Dictionary<(uint, ItemFlags, ulong), int>();
            var itemCounts = new Dictionary<(uint, ItemFlags), int>();
            foreach (var inventory in _inventories)
            {
                foreach (var itemList in inventory.Value.GetAllInventories())
                {
                    foreach (var item in itemList)
                    {
                        if (item == null) continue;
                        var key = (item.ItemId, item.Flags, item.RetainerId);
                        if (!retainerItemCounts.ContainsKey(key))
                        {
                            retainerItemCounts[key] = 0;
                        }

                        retainerItemCounts[key] += (int)item.Quantity;
                        
                        var key2 = (item.ItemId, item.Flags);
                        if (!itemCounts.ContainsKey(key2))
                        {
                            itemCounts[key2] = 0;
                        }

                        itemCounts[key2] += (int)item.Quantity;

                    }
                }
            }
            _retainerItemCounts = retainerItemCounts;
            _itemCounts = itemCounts;
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
            
            return new ItemChanges( actualAddedItems, actualDeletedItems);
        }

        private void GenerateAllItems()
        {
            List<InventoryItem> newItems = new List<InventoryItem>();

            foreach (var characterInventory in _inventories)
            {
                foreach (var inventory in characterInventory.Value.GetAllInventories())
                {
                    foreach (var item in inventory)
                    {
                        if (item == null) continue;
                        newItems.Add(item);
                    }
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
            MonitorStarted,
        }

        private unsafe void GenerateInventories(InventoryGenerateReason generateReason)
        {
            if (!_started)
            {
                return;
            }
            Task.Run(GenerateInventoriesTask);
        }

        private void GenerateInventoriesTask()
        {
            var characterId = _characterMonitor.LocalContentId;
            if (characterId == 0)
            {
                Service.Log.Debug("Not generating inventory, not logged in.");
                return;
            }


            GenerateItemCounts();
            var oldItemCounts = _retainerItemCounts;

            if (!_inventories.ContainsKey(characterId))
            {
                _inventories[characterId] = new Inventory(CharacterType.Character, characterId);
            }

            var inventory = _inventories[characterId];
            List<InventoryChange> inventoryChanges = new List<InventoryChange>();

            GenerateCharacterInventories(inventory, inventoryChanges);
            GenerateSaddleInventories(inventory, inventoryChanges);
            GenerateArmouryChestInventories(inventory, inventoryChanges);
            GenerateEquippedItems(inventory, inventoryChanges);
            GenerateFreeCompanyInventories(inventoryChanges);
            GenerateHousingInventories(inventoryChanges);
            GenerateRetainerInventories(inventoryChanges);
            GenerateGlamourInventories(inventory, inventoryChanges);
            GenerateArmoireInventories(inventory, inventoryChanges);
            GenerateCurrencyInventories(inventory, inventoryChanges);
            GenerateCrystalInventories(inventory, inventoryChanges);

            GenerateItemCounts();
            var newItemCounts = _retainerItemCounts;
            var itemChanges = CompareItemCounts(oldItemCounts, newItemCounts);
            GenerateAllItems();
            _frameworkService.RunOnFrameworkThread(() =>
            {
                OnInventoryChanged?.Invoke(inventoryChanges, itemChanges);
            });
        }

        private unsafe void GenerateCharacterInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
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
                inventory.LoadGameItems(bag1, InventoryType.Bag0, InventoryCategory.CharacterBags, false, inventoryChanges);
                inventory.LoadGameItems(bag2, InventoryType.Bag1, InventoryCategory.CharacterBags, false, inventoryChanges);
                inventory.LoadGameItems(bag3, InventoryType.Bag2, InventoryCategory.CharacterBags, false, inventoryChanges);
                inventory.LoadGameItems(bag4, InventoryType.Bag3, InventoryCategory.CharacterBags, false, inventoryChanges);
            }
        }

        private unsafe void GenerateSaddleInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
        {
            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.SaddleBag1) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.SaddleBag2))
            {
                var bag1 = _inventoryScanner.SaddleBag1;
                var bag2 = _inventoryScanner.SaddleBag2;
                inventory.LoadGameItems(bag1, InventoryType.SaddleBag0, InventoryCategory.CharacterSaddleBags, false, inventoryChanges);
                inventory.LoadGameItems(bag2, InventoryType.SaddleBag1, InventoryCategory.CharacterSaddleBags, false, inventoryChanges);
            }

            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.PremiumSaddleBag1) &&
                _inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.PremiumSaddleBag2))
            {
                var bag1 = _inventoryScanner.PremiumSaddleBag1;
                var bag2 = _inventoryScanner.PremiumSaddleBag2;
                inventory.LoadGameItems(bag1, InventoryType.PremiumSaddleBag0, InventoryCategory.CharacterPremiumSaddleBags, false, inventoryChanges);
                inventory.LoadGameItems(bag2, InventoryType.PremiumSaddleBag1, InventoryCategory.CharacterPremiumSaddleBags, false, inventoryChanges);
            }
        }

        private unsafe void GenerateArmouryChestInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
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
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    continue;
                }
                var armoryItems = _inventoryScanner.GetInventoryByType(inventoryType);
                inventory.LoadGameItems(armoryItems, inventoryType.Convert(), InventoryCategory.CharacterArmoryChest, false, inventoryChanges,
                    (newItem,_) =>
                {
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
                });
            }
        }

        private void GenerateEquippedItems(Inventory inventory, List<InventoryChange> inventoryChanges)
        {
            if (_inventoryScanner.InMemory.Contains(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.EquippedItems))
            {
                var bag1 = _inventoryScanner.CharacterEquipped;
                var gearSets = _inventoryScanner.GetGearSets();
                inventory.LoadGameItems(bag1, InventoryType.GearSet0, InventoryCategory.CharacterEquipped, false, inventoryChanges,
                    (newItem,_) =>
                    {
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
                    });
            }
        }

        private void GenerateFreeCompanyInventories(List<InventoryChange> inventoryChanges)
        {
            var freeCompanyId = _characterMonitor.ActiveFreeCompanyId;
            if (freeCompanyId == 0) return;
            
            if (!_inventories.ContainsKey(freeCompanyId))
            {
                _inventories[freeCompanyId] = new Inventory(CharacterType.FreeCompanyChest, freeCompanyId);
            }

            var inventory = _inventories[freeCompanyId];

            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage1);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage2);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage3);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage4);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyPage5);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyCrystals);
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.FreeCompanyGil);
            inventoryTypes.Add( (FFXIVClientStructs.FFXIV.Client.Game.InventoryType)InventoryType.FreeCompanyCurrency);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    continue;
                }

                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                inventory.LoadGameItems(items,inventoryType.Convert(), inventoryCategory, false, inventoryChanges);
            }
        }

        private Dictionary<InventoryCategory, HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>> _housingMap =
            new()
            {
                {InventoryCategory.HousingInteriorItems, new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>()
                {
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems1,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems2,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems3,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems4,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems5,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems6,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems7,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorPlacedItems8
                }},
                {InventoryCategory.HousingInteriorAppearance, new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>()
                {
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorAppearance
                }},
                {InventoryCategory.HousingInteriorStoreroom, new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>()
                {
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom1,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom2,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom3,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom4,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom5,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom6,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom7,
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingInteriorStoreroom8
                }},
                {InventoryCategory.HousingExteriorItems, new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>()
                {
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingExteriorPlacedItems
                }},
                {InventoryCategory.HousingExteriorAppearance, new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>()
                {
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingExteriorAppearance
                }},
                {InventoryCategory.HousingExteriorStoreroom, new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>()
                {
                    FFXIVClientStructs.FFXIV.Client.Game.InventoryType.HousingExteriorStoreroom
                }},
            };

        private void GenerateHousingInventories(List<InventoryChange> inventoryChanges)
        {
            var activeHouseId = _characterMonitor.ActiveHouseId;
            if (activeHouseId == 0) return;
            var house = _characterMonitor.GetCharacterById(activeHouseId);
            if (house == null)
            {
                return;
            }
            
            if (!_inventories.ContainsKey(activeHouseId))
            {
                _inventories[activeHouseId] = new Inventory(CharacterType.Housing, activeHouseId);
            }

            var inventory = _inventories[activeHouseId];

            var plotSize = house.GetPlotSize();

            foreach (var housingMap in _housingMap)
            {
                var totalMaxItems = 100;
                switch (housingMap.Key)
                {
                    case InventoryCategory.HousingInteriorItems:
                    case InventoryCategory.HousingInteriorStoreroom:                    
                        totalMaxItems = plotSize.GetInternalSlots();
                        break;
                    case InventoryCategory.HousingExteriorItems:
                    case InventoryCategory.HousingExteriorStoreroom:                    
                        totalMaxItems = plotSize.GetExternalSlots();
                        break;
                    
                }

                HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = housingMap.Value;
                foreach (var inventoryType in inventoryTypes)
                {
                    if (!_inventoryScanner.InMemory.Contains(inventoryType))
                    {
                        continue;
                    }

                    var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                    var items = _inventoryScanner.GetInventoryByType(inventoryType).Take(totalMaxItems).ToArray();
                    inventory.LoadGameItems(items, inventoryType.Convert(), inventoryCategory,false, inventoryChanges);
                }
            }
        }
        
        private unsafe void GenerateRetainerInventories(List<InventoryChange> inventoryChanges)
        {
            var currentRetainer = _characterMonitor.ActiveRetainerId;
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage1);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage2);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage3);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage4);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerPage5);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerEquippedItems);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerMarket);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerCrystals);
            inventoryTypes.Add(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.RetainerGil);
            if (currentRetainer != 0)
            {
                if (!_inventoryScanner.InMemoryRetainers.ContainsKey(currentRetainer))
                {
                    Service.Log.Debug("Inventory scanner does not have information about this retainer.");
                    return;
                }
                foreach (var inventoryType in inventoryTypes)
                {
                    if (!_inventoryScanner.InMemoryRetainers[currentRetainer].Contains(inventoryType))
                    {
                        Service.Log.Debug("Inventory scanner does not have information about a retainer's " + inventoryType.ToString());
                        return;
                    }
                }
                if (!_inventories.ContainsKey(currentRetainer))
                {
                    _inventories[currentRetainer] = new Inventory(CharacterType.Retainer, currentRetainer);
                }

                var inventory = _inventories[currentRetainer];
                Service.Log.Debug("Retainer inventory found in scanner, loading into inventory monitor.");
                foreach (var inventoryType in inventoryTypes)
                {
                    var items = _inventoryScanner.GetInventoryByType(currentRetainer,inventoryType);
                    var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                    inventory.LoadGameItems(items, inventoryType.Convert(), inventoryCategory, false, inventoryChanges,
                        (newItem, index) =>
                        {
                            if (index >= 0 && index < _inventoryScanner.RetainerMarketPrices[currentRetainer].Length)
                            {
                                if (newItem.ItemId != 0)
                                {
                                    newItem.RetainerMarketPrice =
                                        _inventoryScanner.RetainerMarketPrices[currentRetainer][index];
                                }
                            }
                        });
                }
            }
        }
        
        private void GenerateArmoireInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( (FFXIVClientStructs.FFXIV.Client.Game.InventoryType)InventoryType.Armoire);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    return;
                }
            }

            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                inventory.LoadGameItems(items, inventoryType.Convert(), inventoryCategory, false, inventoryChanges);
            }

        }
        private void GenerateCurrencyInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
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

            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                inventory.LoadGameItems(items, inventoryType.Convert(), inventoryCategory, false, inventoryChanges);
            }
        }
        private void GenerateCrystalInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
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

            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = inventoryType.Convert().ToInventoryCategory();
                inventory.LoadGameItems(items, inventoryType.Convert(), inventoryCategory, false, inventoryChanges);
            }
        }
        
        private void GenerateGlamourInventories(Inventory inventory, List<InventoryChange> inventoryChanges)
        {
            HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType> inventoryTypes = new HashSet<FFXIVClientStructs.FFXIV.Client.Game.InventoryType>();
            inventoryTypes.Add( (FFXIVClientStructs.FFXIV.Client.Game.InventoryType)InventoryType.GlamourChest);
            foreach (var inventoryType in inventoryTypes)
            {
                if (!_inventoryScanner.InMemory.Contains(inventoryType))
                {
                    Service.Log.Verbose("in memory does not contain glamour");
                    return;
                }
            }

            foreach (var inventoryType in inventoryTypes)
            {
                var items = _inventoryScanner.GetInventoryByType(inventoryType);
                var inventoryCategory = InventoryCategory.GlamourChest;
                inventory.LoadGameItems(items, inventoryType.Convert(), inventoryCategory, false, inventoryChanges);
            }
        }


        private bool _disposed;
        private bool _started;

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
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }

        public class ItemChanges
        {
            public List<ItemChangesItem> NewItems;
            public List<ItemChangesItem> RemovedItems;

            public ItemChanges(List<ItemChangesItem> newItems, List<ItemChangesItem> removedItems)
            {
                NewItems = newItems;
                RemovedItems = removedItems;
            }
        }

        public class ItemChangesItem
        {
            public int Quantity;
            public uint ItemId;
            public ulong CharacterId;
            public ItemFlags Flags;
            public DateTime Date;
        }
    }
}