using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Network;
using Dalamud.Logging;
using FFXIVClientInterface;
using InventoryTools;

namespace CriticalCommonLib.Services
{
    public class InventoryMonitor : IDisposable
    {
        private OdrScanner _odrScanner;
        private ClientInterface _clientInterface;
        private ClientState _clientState;
        private CharacterMonitor _characterMonitor;
        private GameUi _gameUi;
        private GameNetwork _network;
        private Framework _framework;
        private DataManager _dataManager;
        private ushort _inventoryTransactionOpcode;

        private InventorySortOrder? _sortOrder;
        private Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> _inventories;
        private IEnumerable<InventoryItem> _allItems;
        private Dictionary<InventoryType, bool> _loadedInventories;

        public delegate void InventoryChangedDelegate(
            Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> _inventories);

        private Queue<DateTime> _networkUpdates = new ();

        public event InventoryChangedDelegate OnInventoryChanged;

        private HashSet<InventoryType> _conditionalInventories = new(){InventoryType.RetainerBag0, InventoryType.PremiumSaddleBag0}; 

        public InventoryMonitor(ClientInterface clientInterface, ClientState clientState, OdrScanner scanner,
            CharacterMonitor monitor, GameUi gameUi, GameNetwork network, Framework framework, DataManager dataManager)
        {
            _odrScanner = scanner;
            _clientInterface = clientInterface;
            _clientState = clientState;
            _characterMonitor = monitor;
            _gameUi = gameUi;
            _network = network;
            _framework = framework;
            _dataManager = dataManager;

            _inventoryTransactionOpcode = (ushort)(this._dataManager.ServerOpCodes.TryGetValue("InventoryTransaction", out var code) ? code : 0x02E4);

            _inventories = new Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>();
            _allItems = new List<InventoryItem>();
            _loadedInventories = new Dictionary<InventoryType, bool>();
            
            _gameUi.WatchWindowState(GameUi.WindowName.InventoryBuddy);
            _gameUi.WatchWindowState(GameUi.WindowName.InventoryBuddy2);

            _network.NetworkMessage +=OnNetworkMessage;
            _odrScanner.OnSortOrderChanged += ReaderOnOnSortOrderChanged;
            _characterMonitor.OnActiveRetainerChanged += CharacterMonitorOnOnActiveCharacterChanged;
            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            _gameUi.UiVisibilityChanged += GameUiOnUiVisibilityChanged;
            _framework.Update += FrameworkOnUpdate;
        }

        private void CharacterMonitorOnOnCharacterUpdated(Character character)
        {
            _loadedInventories.Clear();
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (_networkUpdates.Count != 0)
            {
                if (_networkUpdates.Peek() >= framework.LastUpdate)
                {
                    _networkUpdates.Dequeue();
                    generateInventories();
                }
            }

            foreach (var conditionalInventory in _conditionalInventories)
            {
                unsafe
                {
                    var inventory = GameInterface.GetContainer(conditionalInventory);
                    if (inventory != null)
                    {
                        if (inventory->Loaded == 0)
                        {
                            if(!_loadedInventories.ContainsKey(conditionalInventory))
                            {
                                PluginLog.Verbose(conditionalInventory.ToString() + " is marked as unloaded.");
                                _loadedInventories[conditionalInventory] = false;
                            }
                        }
                        else
                        {
                            if(_loadedInventories.ContainsKey(conditionalInventory) && _loadedInventories[conditionalInventory] == false)
                            {
                                PluginLog.Verbose(conditionalInventory.ToString() + " is marked as loaded after being unloaded.");
                                _loadedInventories[conditionalInventory] = true;
                                _conditionalInventories.Remove(conditionalInventory);
                            }
                        }
                    }
                }
            }
        }

        private void OnNetworkMessage(IntPtr dataptr, ushort opcode, uint sourceactorid, uint targetactorid, NetworkMessageDirection direction)
        {
            if (opcode == _inventoryTransactionOpcode && direction == NetworkMessageDirection.ZoneUp) //Hardcode for now
            {
                PluginLog.Debug("InventoryMonitor: InventoryTransaction");
                _networkUpdates.Enqueue(_framework.LastUpdate.AddSeconds(1));
            }
        }
        
        private void GameUiOnUiVisibilityChanged(GameUi.WindowName windowName, bool isWindowVisible)
        {
            if (windowName == GameUi.WindowName.InventoryBuddy && isWindowVisible)
            {
                PluginLog.Verbose("InventoryMonitor: Chocobo saddle bag opened, generating inventories");
                _loadedInventories[InventoryType.SaddleBag0] = true;
                _loadedInventories[InventoryType.PremiumSaddleBag0] = true;
                //Don't believe we need to resort at this point
                generateInventories();
            }
            if (windowName == GameUi.WindowName.InventoryBuddy2 && isWindowVisible)
            {
                PluginLog.Verbose("InventoryMonitor: Chocobo saddle bag opened, generating inventories");
                _loadedInventories[InventoryType.SaddleBag0] = true;
                _loadedInventories[InventoryType.PremiumSaddleBag0] = true;
                //Don't believe we need to resort at this point
                generateInventories();
            }
        }

        private void CharacterMonitorOnOnActiveCharacterChanged(ulong retainerid)
        {
            PluginLog.Verbose("InventoryMonitor: Retainer changed, generating inventories");
            //Rescan the ODR as our retainer has changed
            _odrScanner.RequestParseOdr();
        }

        public void LoadExistingData(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories)
        {
            _loadedInventories.Clear();
            _inventories = inventories;
            GenerateAllItems();
            OnInventoryChanged?.Invoke(_inventories);
        }

        public Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> Inventories => _inventories;

        public IEnumerable<InventoryItem> AllItems => _allItems;


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

        private unsafe void generateInventories()
        {
            if (_sortOrder == null)
            {
                _odrScanner.RequestParseOdr();
            }

            if (_sortOrder != null)
            {
                var newInventories = new Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>();
                var currentSortOrder = _sortOrder.Value;

                //Actual inventories
                var bag0 = GameInterface.GetContainer(InventoryType.Bag0);
                var bag1 = GameInterface.GetContainer(InventoryType.Bag1);
                var bag2 = GameInterface.GetContainer(InventoryType.Bag2);
                var bag3 = GameInterface.GetContainer(InventoryType.Bag3);
                var saddleBag0 = GameInterface.GetContainer(InventoryType.SaddleBag0);
                var saddleBag1 = GameInterface.GetContainer(InventoryType.SaddleBag1);
                var premiumSaddleBag0 = GameInterface.GetContainer(InventoryType.PremiumSaddleBag0);
                var premiumSaddleBag1 = GameInterface.GetContainer(InventoryType.PremiumSaddleBag1);
                
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
                            sortedBag0[index].RetainerId = _clientState.LocalContentId;
                            sortedBag0[index].SortedSlotIndex = index;
                        }

                        for (var index = 0; index < sortedBag1.Count; index++)
                        {
                            sortedBag1[index].SortedContainer = InventoryType.Bag1;
                            sortedBag1[index].SortedCategory = InventoryCategory.CharacterBags;
                            sortedBag1[index].RetainerId = _clientState.LocalContentId;
                            sortedBag1[index].SortedSlotIndex = index;
                        }

                        for (var index = 0; index < sortedBag2.Count; index++)
                        {
                            sortedBag2[index].SortedContainer = InventoryType.Bag2;
                            sortedBag2[index].SortedCategory = InventoryCategory.CharacterBags;
                            sortedBag2[index].RetainerId = _clientState.LocalContentId;
                            sortedBag2[index].SortedSlotIndex = index;
                        }

                        for (var index = 0; index < sortedBag3.Count; index++)
                        {
                            sortedBag3[index].SortedContainer = InventoryType.Bag3;
                            sortedBag3[index].SortedCategory = InventoryCategory.CharacterBags;
                            sortedBag3[index].RetainerId = _clientState.LocalContentId;
                            sortedBag3[index].SortedSlotIndex = index;
                        }


                        newInventories.Add(_clientState.LocalContentId,
                            new Dictionary<InventoryCategory, List<InventoryItem>>());
                        var mainBag = sortedBag0;
                        mainBag.AddRange(sortedBag1);
                        mainBag.AddRange(sortedBag2);
                        mainBag.AddRange(sortedBag3);
                        newInventories[_clientState.LocalContentId].Add(InventoryCategory.CharacterBags, mainBag);
                    }
                }

                if (currentSortOrder.NormalInventories.ContainsKey("SaddleBag"))
                {
                    var saddleBagLeftSort = currentSortOrder.NormalInventories["SaddleBag"];

                    //Fully sorted bags
                    var sortedSaddleBagLeft = new List<InventoryItem>();
                    var sortedSaddleBagRight = new List<InventoryItem>();


                    if (saddleBag0 != null && saddleBag1 != null && _loadedInventories.ContainsKey(InventoryType.SaddleBag0) && _loadedInventories[InventoryType.SaddleBag0])
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
                            sortedSaddleBagLeft[index].RetainerId = _clientState.LocalContentId;
                            sortedSaddleBagLeft[index].SortedSlotIndex = index;
                        }

                        for (var index = 0; index < sortedSaddleBagRight.Count; index++)
                        {
                            sortedSaddleBagRight[index].SortedContainer = InventoryType.SaddleBag1;
                            sortedSaddleBagRight[index].SortedCategory = InventoryCategory.CharacterSaddleBags;
                            sortedSaddleBagRight[index].RetainerId = _clientState.LocalContentId;
                            sortedSaddleBagRight[index].SortedSlotIndex = index;
                        }

                        var saddleBags = sortedSaddleBagLeft;
                        saddleBags.AddRange(sortedSaddleBagRight);
                        newInventories[_clientState.LocalContentId]
                            .Add(InventoryCategory.CharacterSaddleBags, saddleBags);
                    }
                }
                
                
                if (currentSortOrder.NormalInventories.ContainsKey("SaddleBagPremium"))
                {
                    var saddleBagPremiumSort = currentSortOrder.NormalInventories["SaddleBagPremium"];

                    //Fully sorted bags
                    var sortedPremiumSaddleBagLeft = new List<InventoryItem>();
                    var sortedPremiumSaddleBagRight = new List<InventoryItem>();


                    if (premiumSaddleBag0 != null && premiumSaddleBag1 != null && _loadedInventories.ContainsKey(InventoryType.SaddleBag0) && _loadedInventories[InventoryType.PremiumSaddleBag0])
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
                            sortedPremiumSaddleBagLeft[index].RetainerId = _clientState.LocalContentId;
                            sortedPremiumSaddleBagLeft[index].SortedSlotIndex = index;
                        }

                        for (var index = 0; index < sortedPremiumSaddleBagRight.Count; index++)
                        {
                            sortedPremiumSaddleBagRight[index].SortedContainer = InventoryType.PremiumSaddleBag1;
                            sortedPremiumSaddleBagRight[index].SortedCategory = InventoryCategory.CharacterPremiumSaddleBags;
                            sortedPremiumSaddleBagRight[index].RetainerId = _clientState.LocalContentId;
                            sortedPremiumSaddleBagRight[index].SortedSlotIndex = index;
                        }

                        var saddleBags = sortedPremiumSaddleBagLeft;
                        saddleBags.AddRange(sortedPremiumSaddleBagRight);
                        newInventories[_clientState.LocalContentId]
                            .Add(InventoryCategory.CharacterPremiumSaddleBags, saddleBags);
                    }
                }


                var currentRetainer = _characterMonitor.ActiveRetainer;
                if (currentRetainer != 0)
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

                        if (retainerBag0 != null && retainerBag1 != null && retainerBag2 != null &&
                            retainerBag3 != null && retainerBag4 != null && retainerBag5 != null &&
                            retainerBag6 != null)
                        {
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
                                    if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                    {
                                        sortedRetainerBag0[index].SortedContainer = retainerBags[sortedBagIndex];
                                    }

                                    sortedRetainerBag0[index].SortedCategory = InventoryCategory.RetainerBags;
                                    sortedRetainerBag0[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
                                    sortedRetainerBag0[index].RetainerId = currentRetainer;
                                }

                                absoluteIndex++;
                            }

                            for (var index = 0; index < sortedRetainerBag1.Count; index++)
                            {
                                var sortedBagIndex = absoluteIndex / 35;
                                if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                {
                                    if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                    {
                                        sortedRetainerBag1[index].SortedContainer = retainerBags[sortedBagIndex];
                                    }

                                    sortedRetainerBag1[index].SortedCategory = InventoryCategory.RetainerBags;
                                    sortedRetainerBag1[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
                                    sortedRetainerBag1[index].RetainerId = currentRetainer;
                                }

                                absoluteIndex++;
                            }

                            for (var index = 0; index < sortedRetainerBag2.Count; index++)
                            {
                                var sortedBagIndex = absoluteIndex / 35;
                                if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                {
                                    if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                    {
                                        sortedRetainerBag2[index].SortedContainer = retainerBags[sortedBagIndex];
                                    }

                                    sortedRetainerBag2[index].SortedCategory = InventoryCategory.RetainerBags;
                                    sortedRetainerBag2[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
                                    sortedRetainerBag2[index].RetainerId = currentRetainer;
                                }

                                absoluteIndex++;
                            }

                            for (var index = 0; index < sortedRetainerBag3.Count; index++)
                            {
                                var sortedBagIndex = absoluteIndex / 35;
                                if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                {
                                    if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                    {
                                        sortedRetainerBag3[index].SortedContainer = retainerBags[sortedBagIndex];
                                    }

                                    sortedRetainerBag3[index].SortedCategory = InventoryCategory.RetainerBags;
                                    sortedRetainerBag3[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
                                    sortedRetainerBag3[index].RetainerId = currentRetainer;
                                }

                                absoluteIndex++;
                            }

                            for (var index = 0; index < sortedRetainerBag4.Count; index++)
                            {
                                var sortedBagIndex = absoluteIndex / 35;
                                if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                {
                                    if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                                    {
                                        sortedRetainerBag4[index].SortedContainer = retainerBags[sortedBagIndex];
                                    }

                                    sortedRetainerBag4[index].SortedCategory = InventoryCategory.RetainerBags;
                                    sortedRetainerBag4[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
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
                                    sortedRetainerBag5[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
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
                                    sortedRetainerBag6[index].SortedSlotIndex = absoluteIndex - (sortedBagIndex * 35);
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
                    }
                    else
                    {
                        PluginLog.Verbose("Current retainer has no sort information.");
                    }
                }

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
            }

            GenerateAllItems();
            OnInventoryChanged(_inventories);
        }

        public void ClearHighlights()
        {
            if (_gameUi.IsWindowVisible(GameUi.WindowName.InventoryGrid0E))
            {
                var inventoryGrid0 = _gameUi.GetPrimaryInventoryGrid(0);
                var inventoryGrid1 = _gameUi.GetPrimaryInventoryGrid(1);
                var inventoryGrid2 = _gameUi.GetPrimaryInventoryGrid(2);
                var inventoryGrid3 = _gameUi.GetPrimaryInventoryGrid(3);
                inventoryGrid0.ClearColors();
                inventoryGrid1.ClearColors();
                inventoryGrid2.ClearColors();
                inventoryGrid3.ClearColors();
                PluginLog.Verbose("Cleared inventory colours");
            }

            if (_gameUi.IsWindowVisible(GameUi.WindowName.RetainerGrid0))
            {
                var retainerGrid0 = _gameUi.GetRetainerGrid(0);
                var retainerGrid1 = _gameUi.GetRetainerGrid(1);
                var retainerGrid2 = _gameUi.GetRetainerGrid(2);
                var retainerGrid3 = _gameUi.GetRetainerGrid(3);
                var retainerGrid4 = _gameUi.GetRetainerGrid(4);
                if (retainerGrid0 != null && retainerGrid1 != null && retainerGrid2 != null &&
                    retainerGrid3 != null && retainerGrid4 != null)
                {
                    retainerGrid0.ClearColors();
                    retainerGrid1.ClearColors();
                    retainerGrid2.ClearColors();
                    retainerGrid3.ClearColors();
                    retainerGrid4.ClearColors();
                }
            }
            var retainerList = _gameUi.GetRetainerList();
            if (retainerList != null)
            {
                retainerList.ClearColors();
            }
        }

        public void HighlightItems(IEnumerable<InventoryItem> items, bool highlightInventories = true, bool highlightRetainerList = true)
        {
            if (highlightInventories)
            {
                var containers = items.Select(c => c.Container).Distinct();
                var inventoryTypes = containers.ToList();
                if (inventoryTypes.Contains(InventoryType.Bag0) || inventoryTypes.Contains(InventoryType.Bag1) ||
                    inventoryTypes.Contains(InventoryType.Bag2) || inventoryTypes.Contains(InventoryType.Bag3))
                {
                    if (_gameUi.IsWindowVisible(GameUi.WindowName.InventoryGrid0E))
                    {
                        var inventoryGrid0 = _gameUi.GetPrimaryInventoryGrid(0);
                        var inventoryGrid1 = _gameUi.GetPrimaryInventoryGrid(1);
                        var inventoryGrid2 = _gameUi.GetPrimaryInventoryGrid(2);
                        var inventoryGrid3 = _gameUi.GetPrimaryInventoryGrid(3);
                        if (inventoryGrid0 != null && inventoryGrid1 != null && inventoryGrid2 != null &&
                            inventoryGrid3 != null)
                        {
                            inventoryGrid0.ClearColors();
                            inventoryGrid1.ClearColors();
                            inventoryGrid2.ClearColors();
                            inventoryGrid3.ClearColors();
                            PluginLog.Verbose("Cleared inventory colours");
                            foreach (var item in items)
                            {
                                if (item.RetainerId == _clientState.LocalContentId)
                                {
                                    if (item.SortedContainer == InventoryType.Bag0)
                                    {
                                        inventoryGrid0.SetColor(item.SortedSlotIndex, 50, 100,
                                            50);
                                    }

                                    if (item.SortedContainer == InventoryType.Bag1)
                                    {
                                        inventoryGrid1.SetColor(item.SortedSlotIndex, 50, 100,
                                            50);
                                    }

                                    if (item.SortedContainer == InventoryType.Bag2)
                                    {
                                        inventoryGrid2.SetColor(item.SortedSlotIndex, 50, 100,
                                            50);
                                    }

                                    if (item.SortedContainer == InventoryType.Bag3)
                                    {
                                        inventoryGrid3.SetColor(item.SortedSlotIndex, 50, 100,
                                            50);
                                    }
                                }
                            }
                        }
                    }
                }

                if (inventoryTypes.Contains(InventoryType.RetainerBag0) ||
                    inventoryTypes.Contains(InventoryType.RetainerBag1) ||
                    inventoryTypes.Contains(InventoryType.RetainerBag2) ||
                    inventoryTypes.Contains(InventoryType.RetainerBag3) ||
                    inventoryTypes.Contains(InventoryType.RetainerBag4) ||
                    inventoryTypes.Contains(InventoryType.RetainerBag5) ||
                    inventoryTypes.Contains(InventoryType.RetainerBag6))
                {
                    if (_gameUi.IsWindowVisible(GameUi.WindowName.RetainerGrid0))
                    {
                        var retainerGrid0 = _gameUi.GetRetainerGrid(0);
                        var retainerGrid1 = _gameUi.GetRetainerGrid(1);
                        var retainerGrid2 = _gameUi.GetRetainerGrid(2);
                        var retainerGrid3 = _gameUi.GetRetainerGrid(3);
                        var retainerGrid4 = _gameUi.GetRetainerGrid(4);
                        if (retainerGrid0 != null && retainerGrid1 != null && retainerGrid2 != null &&
                            retainerGrid3 != null && retainerGrid4 != null)
                        {
                            retainerGrid0.ClearColors();
                            retainerGrid1.ClearColors();
                            retainerGrid2.ClearColors();
                            retainerGrid3.ClearColors();
                            retainerGrid4.ClearColors();
                            foreach (var item in items)
                            {
                                if (item.RetainerId == _characterMonitor.ActiveRetainer)
                                {
                                    switch (item.SortedContainer)
                                    {
                                        case InventoryType.RetainerBag0:
                                            retainerGrid0.SetColor(item.SortedSlotIndex, 50, 100,
                                                50);
                                            break;
                                        case InventoryType.RetainerBag1:
                                            retainerGrid1.SetColor(item.SortedSlotIndex, 50, 100,
                                                50);
                                            break;
                                        case InventoryType.RetainerBag2:
                                            retainerGrid2.SetColor(item.SortedSlotIndex, 50, 100,
                                                50);
                                            break;
                                        case InventoryType.RetainerBag3:
                                            retainerGrid3.SetColor(item.SortedSlotIndex, 50, 100,
                                                50);
                                            break;
                                        case InventoryType.RetainerBag4:
                                            retainerGrid4.SetColor(item.SortedSlotIndex, 50, 100,
                                                50);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (inventoryTypes.Contains(InventoryType.SaddleBag0) ||
                    inventoryTypes.Contains(InventoryType.SaddleBag1))
                {
                    if (_gameUi.IsWindowVisible(GameUi.WindowName.InventoryBuddy))
                    {

                    }
                }
            }

            if (highlightRetainerList)
            {
                var retainerList = _gameUi.GetRetainerList();
                var currentCharacterId = _clientState.LocalContentId;
                if (retainerList != null)
                {
                    retainerList.ClearColors();
                    foreach (var listRetainer in retainerList._sortedItems)
                    {
                        var retainer = _characterMonitor.GetCharacterByName(listRetainer.RetainerName, currentCharacterId);
                        if (retainer != null)
                        {
                            var count = items.Count(c => c.RetainerId == retainer.CharacterId);
                            if (count != 0)
                            {
                                retainerList.SetTextAndColor(retainer.Name,retainer.Name + "(" + count + ")", "00FF00");
                            }
                        }
                    }
                }
            }
        }

        private void ReaderOnOnSortOrderChanged(InventorySortOrder sortorder)
        {
            PluginLog.Verbose("InventoryMonitor: Sort order changed, generating inventories");
            _sortOrder = sortorder;
            generateInventories();
        }

        public bool IsDead { get; set; }


        private void Dispose(bool disposing)
        {
            IsDead = true;
            if (disposing)
            {
                if (_odrScanner != null)
                {
                    _odrScanner.OnSortOrderChanged -= ReaderOnOnSortOrderChanged;
                }

                if (_characterMonitor != null)
                {
                    _characterMonitor.OnActiveRetainerChanged -= CharacterMonitorOnOnActiveCharacterChanged;
                }
                _gameUi.UiVisibilityChanged -= GameUiOnUiVisibilityChanged;
                _network.NetworkMessage -=OnNetworkMessage;
                _framework.Update -= FrameworkOnUpdate;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}