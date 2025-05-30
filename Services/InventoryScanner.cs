using System;
using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Helpers;
using CriticalCommonLib.Addons;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.GameStructs;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services.Ui;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace CriticalCommonLib.Services
{
    public class BagChangeContainer
    {
        public List<BagChange>? changes { get; private set; }
        public bool HasChanges { get; private set; }

        public void Add(BagChange change)
        {
            if (changes == null)
            {
                changes = new List<BagChange>();
            }
            changes.Add(change);
            HasChanges = true;
        }
    }
    public class InventoryScanner : IInventoryScanner
    {
        private bool _running;
        private readonly ICharacterMonitor _characterMonitor;
        private readonly IGameUiManager _gameUiManager;
        private readonly IFramework _framework;
        private IGameInterface _gameInterface;
        private IOdrScanner _odrScanner;
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly CabinetSheet _cabinetSheet;
        private readonly IPluginLog _pluginLog;
        private readonly ItemSheet _itemSheet;
        private readonly IClientState _clientState;
        private readonly IMarketOrderService _marketOrderService;
        private readonly IAddonLifecycle _addonLifecycle;
        private readonly ExcelSheet<MirageStoreSetItem> _mirageStoreSetItemSheet;
        public DateTime? _lastStorageCheck;
        public DateTime? _nextBagScan;

        public unsafe InventoryScanner(ICharacterMonitor characterMonitor, IGameUiManager gameUiManager, IFramework framework,
            IGameInterface gameInterface, IOdrScanner odrScanner, IGameInteropProvider gameInteropProvider,
            CabinetSheet cabinetSheet, ExcelSheet<MirageStoreSetItem> mirageStoreSetItemSheet, IPluginLog pluginLog,
            ItemSheet itemSheet, IClientState clientState, IMarketOrderService marketOrderService, IAddonLifecycle addonLifecycle)
        {
            _gameUiManager = gameUiManager;
            _framework = framework;
            _characterMonitor = characterMonitor;
            _gameInterface = gameInterface;
            _odrScanner = odrScanner;
            _gameInteropProvider = gameInteropProvider;
            _cabinetSheet = cabinetSheet;
            _mirageStoreSetItemSheet = mirageStoreSetItemSheet;
            _pluginLog = pluginLog;
            _itemSheet = itemSheet;
            _clientState = clientState;
            _marketOrderService = marketOrderService;
            _addonLifecycle = addonLifecycle;

            _mirageSetLookup = _mirageStoreSetItemSheet.ToDictionary(c => c.RowId, c => new List<uint>()
            {
                c.MainHand.RowId, c.OffHand.RowId, c.Head.RowId, c.Body.RowId, c.Hands.RowId, c.Legs.RowId, c.Feet.RowId, c.Earrings.RowId,
                c.Necklace.RowId, c.Bracelets.RowId, c.Ring.RowId
            }.Where(c => c != 0).Distinct().ToHashSet());

            _mirageSetItemLookup = new Dictionary<uint, HashSet<uint>>();

            foreach (var set in _mirageSetLookup)
            {
                foreach (var setItem in set.Value)
                {
                    _mirageSetItemLookup.TryAdd(setItem, new HashSet<uint>());
                    _mirageSetItemLookup[setItem].Add(set.Key);
                }
            }


            _framework.RunOnFrameworkThread(() =>
            {
                _gameInteropProvider.InitializeFromAttributes(this);
                _containerInfoNetworkHook?.Enable();
                _itemMarketBoardInfoHook?.Enable();
            });
            framework.Update += FrameworkOnUpdate;
            _gameUiManager.UiVisibilityChanged += GameUiManagerOnUiManagerVisibilityChanged;
            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            _characterMonitor.OnActiveRetainerChanged += CharacterMonitorOnOnActiveRetainerChanged;
            _characterMonitor.OnActiveFreeCompanyChanged += CharacterMonitorOnOnActiveFreeCompanyChanged;
            _characterMonitor.OnActiveHouseChanged += CharacterMonitorOnOnActiveHouseChanged;
            _odrScanner.OnSortOrderChanged += SortOrderChanged;
            _clientState.Logout += ClientStateOnLogout;
            Armoire = new InventoryItem[cabinetSheet.Count()];
            GlamourChest = new InventoryItem[8000];
            addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MiragePrismPrismBox", PrismBoxFinalize);
            _pluginLog.Verbose("Starting service {type} ({this})", GetType().Name, this);
        }

        private void ClientStateOnLogout(int type, int code)
        {
            InMemory.Clear();
        }

        private void PrismBoxFinalize(AddonEvent type, AddonArgs args)
        {
            _pluginLog.Verbose("Prism box finalized");
            _glamourAgentActive = false;
            _glamourAgentOpened = null;
        }

        private void SortOrderChanged(InventorySortOrder sortorder)
        {
            _nextBagScan = DateTime.Now;
        }

        private unsafe void FrameworkOnUpdate(IFramework framework)
        {
            var lastUpdate = framework.LastUpdate;

            if (_nextBagScan == null)
            {
                _nextBagScan = DateTime.Now;
            }
            if (_nextBagScan != null && _nextBagScan.Value <= lastUpdate)
            {
                _nextBagScan = null;
                ParseBags();
            }

            if (_loadedInventories.Contains(InventoryType.HousingExteriorPlacedItems))
            {
                if (_lastStorageCheck == null)
                {
                    _lastStorageCheck = lastUpdate;
                    return;
                }

                if (_lastStorageCheck != null && _lastStorageCheck.Value.AddMilliseconds(200) <= lastUpdate)
                {
                    var atkUnitBase = _gameUiManager.GetWindow(WindowName.HousingGoods.ToString());
                    if (atkUnitBase == null)
                    {
                        _loadedInventories.Remove(InventoryType.HousingExteriorStoreroom);
                        return;
                    }

                    var housingGoodsAddon = (AddonHousingGoods*)atkUnitBase;
                    if (housingGoodsAddon == null)
                    {
                        _loadedInventories.Remove(InventoryType.HousingExteriorStoreroom);
                        return;
                    }

                    if (housingGoodsAddon->CurrentTab != 1)
                    {
                        _loadedInventories.Remove(InventoryType.HousingExteriorStoreroom);
                        return;
                    }

                    _loadedInventories.Add(InventoryType.HousingExteriorStoreroom);
                }
            }
            else
            {
                _loadedInventories.Remove(InventoryType.HousingExteriorStoreroom);
            }
        }

        private void CharacterMonitorOnOnActiveHouseChanged(ulong houseid, sbyte wardid, sbyte plotid, byte divisionid, short roomid, bool hashousepermission)
        {
            foreach (var housingCategory in _housingMap)
            {
                foreach (var type in housingCategory.Value)
                {
                    _loadedInventories.Remove(type);
                    InMemory.Remove(type);
                }
            }
        }

        private void CharacterMonitorOnOnActiveFreeCompanyChanged(ulong freeCompanyId)
        {
            if (freeCompanyId == 0)
            {
                _loadedInventories.RemoveWhere(c => c is InventoryType.FreeCompanyPage1 or InventoryType.FreeCompanyPage2
                    or InventoryType.FreeCompanyPage3 or InventoryType.FreeCompanyPage4 or InventoryType.FreeCompanyPage5
                    or InventoryType.FreeCompanyCrystals or InventoryType.FreeCompanyGil or (InventoryType)Enums.InventoryType.FreeCompanyCurrency);
            }
        }

        private Dictionary<InventoryCategory, HashSet<InventoryType>> _housingMap =
            new()
            {
                {InventoryCategory.HousingInteriorItems, new HashSet<InventoryType>()
                {
                    InventoryType.HousingInteriorPlacedItems1,
                    InventoryType.HousingInteriorPlacedItems2,
                    InventoryType.HousingInteriorPlacedItems3,
                    InventoryType.HousingInteriorPlacedItems4,
                    InventoryType.HousingInteriorPlacedItems5,
                    InventoryType.HousingInteriorPlacedItems6,
                    InventoryType.HousingInteriorPlacedItems7,
                    InventoryType.HousingInteriorPlacedItems8
                }},
                {InventoryCategory.HousingInteriorAppearance, new HashSet<InventoryType>()
                {
                    InventoryType.HousingInteriorAppearance
                }},
                {InventoryCategory.HousingInteriorStoreroom, new HashSet<InventoryType>()
                {
                    InventoryType.HousingInteriorStoreroom1,
                    InventoryType.HousingInteriorStoreroom2,
                    InventoryType.HousingInteriorStoreroom3,
                    InventoryType.HousingInteriorStoreroom4,
                    InventoryType.HousingInteriorStoreroom5,
                    InventoryType.HousingInteriorStoreroom6,
                    InventoryType.HousingInteriorStoreroom7,
                    InventoryType.HousingInteriorStoreroom8
                }},
                {InventoryCategory.HousingExteriorItems, new HashSet<InventoryType>()
                {
                    InventoryType.HousingExteriorPlacedItems
                }},
                {InventoryCategory.HousingExteriorAppearance, new HashSet<InventoryType>()
                {
                    InventoryType.HousingExteriorAppearance
                }},
                {InventoryCategory.HousingExteriorStoreroom, new HashSet<InventoryType>()
                {
                    InventoryType.HousingExteriorStoreroom
                }},
            };

        private void GameUiManagerOnUiManagerVisibilityChanged(WindowName windowName, bool? isWindowVisible)
        {
            if (windowName is WindowName.InventoryBuddy or WindowName.InventoryBuddy2 && isWindowVisible.HasValue)
            {
                if (isWindowVisible.Value)
                {
                    _loadedInventories.Add(InventoryType.SaddleBag1);
                    _loadedInventories.Add(InventoryType.SaddleBag2);
                    _loadedInventories.Add(InventoryType.PremiumSaddleBag1);
                    _loadedInventories.Add(InventoryType.PremiumSaddleBag2);
                }
                else
                {
                    _loadedInventories.Remove(InventoryType.SaddleBag1);
                    _loadedInventories.Remove(InventoryType.SaddleBag2);
                    _loadedInventories.Remove(InventoryType.PremiumSaddleBag1);
                    _loadedInventories.Remove(InventoryType.PremiumSaddleBag2);
                    InMemory.Remove(InventoryType.SaddleBag1);
                    InMemory.Remove(InventoryType.SaddleBag2);
                    InMemory.Remove(InventoryType.PremiumSaddleBag1);
                    InMemory.Remove(InventoryType.PremiumSaddleBag2);
                }
            }
            if (windowName is WindowName.CabinetWithdraw or WindowName.Cabinet && isWindowVisible.HasValue)
            {
                if (isWindowVisible.Value)
                {
                    _loadedInventories.Add((InventoryType)Enums.InventoryType.Armoire);
                }
                else
                {
                    _loadedInventories.Remove((InventoryType)Enums.InventoryType.Armoire);
                    InMemory.Remove((InventoryType)Enums.InventoryType.Armoire);
                }
            }
            if (windowName is WindowName.HousingGoods && isWindowVisible.HasValue)
            {
                unsafe
                {
                    var housingManager = HousingManager.Instance();
                    if (housingManager != null)
                    {
                        if (isWindowVisible.Value)
                        {
                            if (housingManager->IsInside())
                            {
                                foreach (var inventoryType in _housingMap[InventoryCategory.HousingInteriorItems])
                                {
                                    _loadedInventories.Add(inventoryType);
                                }
                                //You'd think that we'd also mark the interior housing storage as loaded but nope, it actually uses the loaded flag
                            }
                            else
                            {
                                foreach (var inventoryType in _housingMap[InventoryCategory.HousingExteriorItems])
                                {
                                    _loadedInventories.Add(inventoryType);
                                }
                            }
                        }
                        else
                        {
                            if (housingManager->IsInside())
                            {
                                foreach (var inventoryType in _housingMap[InventoryCategory.HousingInteriorItems])
                                {
                                    _loadedInventories.Remove(inventoryType);
                                    InMemory.Remove(inventoryType);
                                }
                            }
                            else
                            {
                                foreach (var inventoryType in _housingMap[InventoryCategory.HousingExteriorItems])
                                {
                                    _loadedInventories.Remove(inventoryType);
                                    InMemory.Remove(inventoryType);
                                }
                                foreach (var inventoryType in _housingMap[InventoryCategory.HousingExteriorStoreroom])
                                {
                                    _loadedInventories.Remove(inventoryType);
                                    InMemory.Remove(inventoryType);
                                }
                            }
                        }
                    }
                }
            }
            if (windowName is WindowName.HousingEditExterior && isWindowVisible.HasValue)
            {
                if (isWindowVisible.Value)
                {
                    _loadedInventories.Add(InventoryType.HousingExteriorAppearance);
                }
                else
                {
                    _loadedInventories.Remove(InventoryType.HousingExteriorAppearance);
                    InMemory.Remove(InventoryType.HousingExteriorAppearance);
                }
            }
            if (windowName is WindowName.HousingEditInterior && isWindowVisible.HasValue)
            {
                if (isWindowVisible.Value)
                {
                    _loadedInventories.Add(InventoryType.HousingInteriorAppearance);
                }
                else
                {
                    _loadedInventories.Remove(InventoryType.HousingInteriorAppearance);
                    InMemory.Remove(InventoryType.HousingInteriorAppearance);
                }
            }
            if (windowName is WindowName.FreeCompany or WindowName.FreeCompanyCreditShop && isWindowVisible.HasValue)
            {
                if (isWindowVisible.Value)
                {
                    _loadedInventories.Add((InventoryType)Enums.InventoryType.FreeCompanyCurrency);
                }
                else
                {
                    _loadedInventories.Remove((InventoryType)Enums.InventoryType.FreeCompanyCurrency);
                    InMemory.Remove((InventoryType)Enums.InventoryType.FreeCompanyCurrency);
                }
            }
        }


        private void CharacterMonitorOnOnActiveRetainerChanged(ulong retainerid)
        {
            if (retainerid == 0)
                _loadedInventories.RemoveWhere(c => c is InventoryType.RetainerPage1 or InventoryType.RetainerPage2
                    or InventoryType.RetainerPage3 or InventoryType.RetainerPage4 or InventoryType.RetainerPage5
                    or InventoryType.RetainerPage6 or InventoryType.RetainerPage7 or InventoryType.RetainerMarket
                    or InventoryType.RetainerGil or InventoryType.RetainerEquippedItems
                    or InventoryType.RetainerCrystals);
        }

        private void CharacterMonitorOnOnCharacterUpdated(Character? character)
        {
            if (character == null)
            {
                _pluginLog.Debug("Character has been cleared, clearing cache");
                ClearCache();
            }
        }

        public void Enable()
        {
            _running = true;
        }

        public delegate void BagsChangedDelegate(List<BagChange> changes);

        public event BagsChangedDelegate? BagsChanged;

        public delegate void ContainerInfoReceivedDelegate(ContainerInfo containerInfo, InventoryType inventoryType);

        public event ContainerInfoReceivedDelegate? ContainerInfoReceived;

        private unsafe delegate void* ContainerInfoNetworkData(int a2, int* a3);

        private unsafe delegate void* ItemMarketBoardInfoData(int a2, int* a3);

        private unsafe delegate void* NpcSpawnData(int* a1, int a2, int* a3);

        //If the signature for these are ever lost, find the ProcessZonePacketDown signature in Dalamud and then find the relevant function based on the opcode.
        [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 56 10", DetourName = nameof(ContainerInfoDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<ContainerInfoNetworkData>? _containerInfoNetworkHook = null;

        [Signature(
            "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B D6 8B CF E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 56 10",
            DetourName = nameof(ItemMarketBoardInfoDetour))]
        private Hook<ItemMarketBoardInfoData>? _itemMarketBoardInfoHook = null;

        private readonly HashSet<InventoryType> _loadedInventories = new();
        private readonly Dictionary<ulong,uint[]> _cachedRetainerMarketPrices = new Dictionary<ulong, uint[]>();


        private uint[]? GetCachedMarketPrice(ulong retainerId)
        {
            if (_cachedRetainerMarketPrices.ContainsKey(retainerId))
            {
                return _cachedRetainerMarketPrices[retainerId];
            }

            return null;
        }

        private unsafe void* ContainerInfoDetour(int seq, int* a3)
        {
            try
            {
                if (a3 != null)
                {
                    var ptr = (IntPtr)a3 + 16;
                    var containerInfo = NetworkDecoder.DecodeContainerInfo(ptr);
                    if (Enum.IsDefined(typeof(InventoryType), containerInfo.containerId))
                    {
                        // _framework.RunOnFrameworkThread(() =>
                        // {
                        //     _pluginLog.Verbose("Container update " + containerInfo.containerId.ToString());
                        // });
                        var inventoryType = (InventoryType)containerInfo.containerId;
                        //Delay just in case the items haven't loaded.
                        _framework.RunOnTick(() =>
                            {
                                _loadedInventories.Add(inventoryType);
                                ContainerInfoReceived?.Invoke(containerInfo, inventoryType);
                            },
                            TimeSpan.FromMilliseconds(100));
                        ;
                    }
                }
            }
            catch (Exception e)
            {
                _framework.RunOnFrameworkThread(() =>
                {
                    _pluginLog.Error(e, "shits broke yo");
                });
            }

            return _containerInfoNetworkHook!.Original(seq, a3);
        }

        private unsafe void* ItemMarketBoardInfoDetour(int seq, int* a3)
        {
            try
            {
                if (a3 != null)
                {
                    var ptr = (IntPtr)a3 + 16;
                    var containerInfo = NetworkDecoder.DecodeItemMarketBoardInfo(ptr);
                    var currentRetainer = _characterMonitor.ActiveRetainerId;
                    if (currentRetainer != 0)
                    {
                        if (!_cachedRetainerMarketPrices.ContainsKey(currentRetainer))
                        {
                            _cachedRetainerMarketPrices[currentRetainer] = new uint[20];
                        }

                        if (Enum.IsDefined(typeof(InventoryType), containerInfo.containerId) &&
                            containerInfo.containerId != 0)
                        {
                            _cachedRetainerMarketPrices[currentRetainer][containerInfo.slot] = containerInfo.unitPrice;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _pluginLog.Error(e, "shits broke yo");
            }


            return _itemMarketBoardInfoHook!.Original(seq, a3);
        }


        public void ParseBags()
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                if (_clientState.LocalContentId != 0 && _running)
                {
                    var changeSet = new BagChangeContainer();
                    var inventorySortOrder = _odrScanner.GetSortOrder(_clientState.LocalContentId);
                    bool gearSetsChanged = false;
                    if (inventorySortOrder != null)
                    {
                        ParseCharacterBags(inventorySortOrder, changeSet);
                        ParseSaddleBags(inventorySortOrder, changeSet);
                        ParsePremiumSaddleBags(inventorySortOrder, changeSet);
                        ParseArmouryChest(inventorySortOrder, changeSet);
                        ParseRetainerBags(inventorySortOrder, changeSet);
                    }

                    ParseCharacterEquipped(changeSet);
                    ParseFreeCompanyBags(changeSet);
                    ParseHouseBags(changeSet);
                    ParseArmoire(changeSet);
                    ParseGlamourChest(changeSet);
                    gearSetsChanged = ParseGearSets(changeSet);


                    if (changeSet.HasChanges && changeSet.changes != null)
                    {
                        _framework.RunOnFrameworkThread(() => _pluginLog.Verbose($"Change count: {changeSet.changes.Count}"));
                        _framework.RunOnFrameworkThread(() => BagsChanged?.Invoke(changeSet.changes));
                    }
                    else if (gearSetsChanged)
                    {
                        _framework.RunOnFrameworkThread(() => _pluginLog.Verbose($"Gearsets changed"));
                        _framework.RunOnFrameworkThread(() => BagsChanged?.Invoke(new List<BagChange>()));
                    }
                }

                _nextBagScan = DateTime.Now.AddMilliseconds(500);
            }
            catch (Exception e)
            {
                _framework.RunOnFrameworkThread(() => _pluginLog.Error("The inventory scanner has crashed. Details below:"));
                _framework.RunOnFrameworkThread(() => _pluginLog.Error(e.ToString()));
                _framework.RunOnFrameworkThread(() => _pluginLog.Error("Attempting to restart the scanner in 20 seconds."));
                _nextBagScan = DateTime.Now.AddMilliseconds(20000);
            }
        }

        public InventoryItem[] GetInventoryByType(ulong retainerId, InventoryType type)
        {
            var bag = new Dictionary<ulong, InventoryItem[]>();
            switch (type)
            {
                case InventoryType.RetainerPage1:
                    bag = RetainerBag1;
                    break;
                case InventoryType.RetainerPage2:
                    bag = RetainerBag2;
                    break;
                case InventoryType.RetainerPage3:
                    bag = RetainerBag3;
                    break;
                case InventoryType.RetainerPage4:
                    bag = RetainerBag4;
                    break;
                case InventoryType.RetainerPage5:
                    bag = RetainerBag5;
                    break;
                case InventoryType.RetainerCrystals:
                    bag = RetainerCrystals;
                    break;
                case InventoryType.RetainerGil:
                    bag = RetainerGil;
                    break;
                case InventoryType.RetainerMarket:
                    bag = RetainerMarket;
                    break;
                case InventoryType.RetainerEquippedItems:
                    bag = RetainerEquipped;
                    break;
            }

            if (bag.ContainsKey(retainerId)) return bag[retainerId];

            return Array.Empty<InventoryItem>();
        }

        public InventoryItem[] GetInventoryByType(InventoryType type)
        {
            switch (type)
            {
                case InventoryType.Inventory1:
                    return CharacterBag1;
                case InventoryType.Inventory2:
                    return CharacterBag2;
                case InventoryType.Inventory3:
                    return CharacterBag3;
                case InventoryType.Inventory4:
                    return CharacterBag4;
                case InventoryType.EquippedItems:
                    return CharacterEquipped;
                case InventoryType.Crystals:
                    return CharacterCrystals;
                case InventoryType.Currency:
                    return CharacterCurrency;
                case InventoryType.SaddleBag1:
                    return SaddleBag1;
                case InventoryType.SaddleBag2:
                    return SaddleBag2;
                case InventoryType.PremiumSaddleBag1:
                    return PremiumSaddleBag1;
                case InventoryType.PremiumSaddleBag2:
                    return PremiumSaddleBag2;
                case InventoryType.ArmoryMainHand:
                    return ArmouryMainHand;
                case InventoryType.ArmoryHead:
                    return ArmouryHead;
                case InventoryType.ArmoryBody:
                    return ArmouryBody;
                case InventoryType.ArmoryHands:
                    return ArmouryHands;
                case InventoryType.ArmoryLegs:
                    return ArmouryLegs;
                case InventoryType.ArmoryFeets:
                    return ArmouryFeet;
                case InventoryType.ArmoryOffHand:
                    return ArmouryOffHand;
                case InventoryType.ArmoryEar:
                    return ArmouryEars;
                case InventoryType.ArmoryNeck:
                    return ArmouryNeck;
                case InventoryType.ArmoryWrist:
                    return ArmouryWrists;
                case InventoryType.ArmoryRings:
                    return ArmouryRings;
                case InventoryType.ArmorySoulCrystal:
                    return ArmourySoulCrystals;
                case InventoryType.FreeCompanyPage1:
                    return FreeCompanyBag1;
                case InventoryType.FreeCompanyPage2:
                    return FreeCompanyBag2;
                case InventoryType.FreeCompanyPage3:
                    return FreeCompanyBag3;
                case InventoryType.FreeCompanyPage4:
                    return FreeCompanyBag4;
                case InventoryType.FreeCompanyPage5:
                    return FreeCompanyBag5;
                case InventoryType.FreeCompanyGil:
                    return FreeCompanyGil;
                case (InventoryType)Enums.InventoryType.FreeCompanyCurrency:
                    return FreeCompanyCurrency;
                case InventoryType.HousingInteriorStoreroom1:
                    return HousingInteriorStoreroom1;
                case InventoryType.HousingInteriorStoreroom2:
                    return HousingInteriorStoreroom2;
                case InventoryType.HousingInteriorStoreroom3:
                    return HousingInteriorStoreroom3;
                case InventoryType.HousingInteriorStoreroom4:
                    return HousingInteriorStoreroom4;
                case InventoryType.HousingInteriorStoreroom5:
                    return HousingInteriorStoreroom5;
                case InventoryType.HousingInteriorStoreroom6:
                    return HousingInteriorStoreroom6;
                case InventoryType.HousingInteriorStoreroom7:
                    return HousingInteriorStoreroom7;
                case InventoryType.HousingInteriorStoreroom8:
                    return HousingInteriorStoreroom8;
                case InventoryType.HousingInteriorPlacedItems1:
                    return HousingInteriorPlacedItems1;
                case InventoryType.HousingInteriorPlacedItems2:
                    return HousingInteriorPlacedItems2;
                case InventoryType.HousingInteriorPlacedItems3:
                    return HousingInteriorPlacedItems3;
                case InventoryType.HousingInteriorPlacedItems4:
                    return HousingInteriorPlacedItems4;
                case InventoryType.HousingInteriorPlacedItems5:
                    return HousingInteriorPlacedItems5;
                case InventoryType.HousingInteriorPlacedItems6:
                    return HousingInteriorPlacedItems6;
                case InventoryType.HousingInteriorPlacedItems7:
                    return HousingInteriorPlacedItems7;
                case InventoryType.HousingInteriorPlacedItems8:
                    return HousingInteriorPlacedItems8;
                case InventoryType.HousingExteriorAppearance:
                    return HousingExteriorAppearance;
                case InventoryType.HousingInteriorAppearance:
                    return HousingInteriorAppearance;
                case InventoryType.HousingExteriorPlacedItems:
                    return HousingExteriorPlacedItems;
                case InventoryType.HousingExteriorStoreroom:
                    return HousingExteriorStoreroom;
                case InventoryType.FreeCompanyCrystals:
                    return FreeCompanyCrystals;
                case (InventoryType)Enums.InventoryType.Armoire:
                    return Armoire;
                case (InventoryType)Enums.InventoryType.GlamourChest:
                    return GlamourChest;
            }

            return Array.Empty<InventoryItem>();
        }

        public bool IsBagLoaded(InventoryType type)
        {
            return _loadedInventories.Contains(type);
        }

        public void ClearRetainerCache(ulong retainerId)
        {
            if (InMemoryRetainers.ContainsKey(retainerId))
            {
                InMemoryRetainers[retainerId] = new HashSet<InventoryType>();
                RetainerBag1.Clear();
                RetainerBag2.Clear();
                RetainerBag3.Clear();
                RetainerBag4.Clear();
                RetainerBag5.Clear();
                RetainerEquipped.Clear();
                RetainerMarket.Clear();
                RetainerCrystals.Clear();
                RetainerGil.Clear();
                RetainerMarketPrices.Clear();
            }
            else
            {
                ClearCache();
            }
        }

        public void ClearFreeCompanyCache(ulong freeCompanyId)
        {
            if (InMemoryFreeCompanies.ContainsKey(freeCompanyId))
            {
                InMemoryFreeCompanies[freeCompanyId] = new HashSet<InventoryType>();
                Array.Clear(FreeCompanyBag1);
                Array.Clear(FreeCompanyBag2);
                Array.Clear(FreeCompanyBag3);
                Array.Clear(FreeCompanyBag4);
                Array.Clear(FreeCompanyBag5);
                Array.Clear(FreeCompanyCrystals);
                Array.Clear(FreeCompanyGil);
                Array.Clear(FreeCompanyCurrency);
            }
        }


        public void ClearCache()
        {
            _loadedInventories.Clear();
            InMemory.Clear();
            InMemoryRetainers.Clear();
            Array.Clear(CharacterBag1);
            Array.Clear(CharacterBag2);
            Array.Clear(CharacterBag3);
            Array.Clear(CharacterBag4);
            Array.Clear(CharacterEquipped);
            Array.Clear(CharacterCrystals);
            Array.Clear(CharacterCurrency);
            Array.Clear(SaddleBag1);
            Array.Clear(SaddleBag2);
            Array.Clear(PremiumSaddleBag1);
            Array.Clear(PremiumSaddleBag2);
            Array.Clear(ArmouryMainHand);
            Array.Clear(ArmouryHead);
            Array.Clear(ArmouryBody);
            Array.Clear(ArmouryHands);
            Array.Clear(ArmouryLegs);
            Array.Clear(ArmouryFeet);
            Array.Clear(ArmouryOffHand);
            Array.Clear(ArmouryEars);
            Array.Clear(ArmouryNeck);
            Array.Clear(ArmouryWrists);
            Array.Clear(ArmouryRings);
            Array.Clear(ArmourySoulCrystals);
            Array.Clear(FreeCompanyBag1);
            Array.Clear(FreeCompanyBag2);
            Array.Clear(FreeCompanyBag3);
            Array.Clear(FreeCompanyBag4);
            Array.Clear(FreeCompanyBag5);
            Array.Clear(FreeCompanyGil);
            Array.Clear(FreeCompanyCrystals);
            Array.Clear(FreeCompanyCurrency);
            Array.Clear(Armoire);
            Array.Clear(GlamourChest);
            RetainerBag1.Clear();
            RetainerBag2.Clear();
            RetainerBag3.Clear();
            RetainerBag4.Clear();
            RetainerBag5.Clear();
            RetainerEquipped.Clear();
            RetainerMarket.Clear();
            RetainerCrystals.Clear();
            RetainerGil.Clear();
            RetainerMarketPrices.Clear();
            GearSets.Clear();
            Array.Clear(GearSetsUsed);
            Array.Clear(GearSetNames);

            Array.Clear(HousingInteriorStoreroom1);
            Array.Clear(HousingInteriorStoreroom2);
            Array.Clear(HousingInteriorStoreroom3);
            Array.Clear(HousingInteriorStoreroom4);
            Array.Clear(HousingInteriorStoreroom5);
            Array.Clear(HousingInteriorStoreroom6);
            Array.Clear(HousingInteriorStoreroom7);
            Array.Clear(HousingInteriorStoreroom8);

            Array.Clear(HousingInteriorAppearance);

            Array.Clear(HousingInteriorPlacedItems1);
            Array.Clear(HousingInteriorPlacedItems2);
            Array.Clear(HousingInteriorPlacedItems3);
            Array.Clear(HousingInteriorPlacedItems4);
            Array.Clear(HousingInteriorPlacedItems5);
            Array.Clear(HousingInteriorPlacedItems6);
            Array.Clear(HousingInteriorPlacedItems7);
            Array.Clear(HousingInteriorPlacedItems8);

            Array.Clear(HousingExteriorAppearance);
            Array.Clear(HousingExteriorPlacedItems);
            Array.Clear(HousingExteriorStoreroom);


        }

        public HashSet<InventoryType> LoadedInventories => _loadedInventories;
        public HashSet<InventoryType> InMemory { get; } = new();
        public Dictionary<ulong, HashSet<InventoryType>> InMemoryRetainers { get; } = new();
        public Dictionary<ulong, HashSet<InventoryType>> InMemoryFreeCompanies { get; } = new();
        public InventoryItem[] CharacterBag1 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterBag2 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterBag3 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterBag4 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterEquipped { get; } = new InventoryItem[14];
        public InventoryItem[] CharacterCrystals { get; } = new InventoryItem[18];
        public InventoryItem[] CharacterCurrency { get; } = new InventoryItem[100];

        public InventoryItem[] SaddleBag1 { get; } = new InventoryItem[35];
        public InventoryItem[] SaddleBag2 { get; } = new InventoryItem[35];
        public InventoryItem[] PremiumSaddleBag1 { get; } = new InventoryItem[35];
        public InventoryItem[] PremiumSaddleBag2 { get; } = new InventoryItem[35];

        public InventoryItem[] ArmouryMainHand { get; } = new InventoryItem[50];
        public InventoryItem[] ArmouryHead { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryBody { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryHands { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryLegs { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryFeet { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryOffHand { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryEars { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryNeck { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryWrists { get; } = new InventoryItem[35];
        public InventoryItem[] ArmouryRings { get; } = new InventoryItem[50];
        public InventoryItem[] ArmourySoulCrystals { get; } = new InventoryItem[25];


        public InventoryItem[] FreeCompanyBag1 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag2 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag3 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag4 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag5 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyGil { get; } = new InventoryItem[11];
        public InventoryItem[] FreeCompanyCurrency { get; } = new InventoryItem[1];
        public InventoryItem[] FreeCompanyCrystals { get; } = new InventoryItem[18];

        public InventoryItem[] HousingInteriorStoreroom1 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom2 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom3 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom4 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom5 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom6 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom7 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorStoreroom8 { get; } = new InventoryItem[50];

        public InventoryItem[] HousingInteriorPlacedItems1 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems2 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems3 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems4 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems5 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems6 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems7 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingInteriorPlacedItems8 { get; } = new InventoryItem[50];
        public InventoryItem[] HousingExteriorAppearance { get; } = new InventoryItem[9];
        public InventoryItem[] HousingExteriorPlacedItems { get; } = new InventoryItem[40];

        public InventoryItem[] HousingExteriorStoreroom { get; } = new InventoryItem[40];
        public InventoryItem[] HousingInteriorAppearance { get; } = new InventoryItem[10];

        public InventoryItem[] Armoire { get; } = Array.Empty<InventoryItem>();
        public InventoryItem[] GlamourChest { get; } = new InventoryItem[800];

        public Dictionary<ulong, InventoryItem[]> RetainerBag1 { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerBag2 { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerBag3 { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerBag4 { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerBag5 { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerEquipped { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerMarket { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerCrystals { get; } = new();
        public Dictionary<ulong, InventoryItem[]> RetainerGil { get; } = new();
        public Dictionary<ulong, uint[]> RetainerMarketPrices { get; } = new();

        public Dictionary<byte, uint[]> GearSets { get; } = new();
        public bool[] GearSetsUsed { get; } = new bool[100];
        public string[] GearSetNames { get; } = new string[100];

        public HashSet<(byte, string)> GetGearSets(uint itemId)
        {
            HashSet<(byte, string)> gearSets = new();
            foreach (var gearSetKey in GearSets.Select(gearSet => gearSet.Key))
            {
                gearSets.Add((gearSetKey, GearSetNames[gearSetKey]));
            }

            return gearSets;
        }

        public Dictionary<uint, HashSet<(byte, string)>> GetGearSets()
        {
            Dictionary<uint, HashSet<(byte, string)>> gearSets = new();
            foreach (var gearSet in GearSets)
                for (var i = 0; i < gearSet.Value.Length; i++)
                {
                    if (!gearSets.ContainsKey(gearSet.Value[i]))
                        gearSets[gearSet.Value[i]] = new HashSet<(byte, string)>();

                    gearSets[gearSet.Value[i]].Add((gearSet.Key, GearSetNames[gearSet.Key]));
                }

            return gearSets;
        }

        private List<uint>? _currencyItemIds;

        public unsafe void ParseCharacterBags(InventorySortOrder currentSortOrder, BagChangeContainer changeSet)
        {
            var bag0 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1);
            var bag1 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2);
            var bag2 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3);
            var bag3 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4);
            var crystals = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Crystals);
            var currency = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
            if (_currencyItemIds == null)
            {
                _currencyItemIds = _itemSheet.Where(c => c.RowId is >= 20 and <= 60 && c.Base.FilterGroup == 16 || c.Base.ItemUICategory.RowId == 100 || c.RowId == 1).Select(c => c.RowId).ToList();
            }

            if (bag0 != null && bag1 != null && bag2 != null && bag3 != null && crystals != null && currency != null)
            {
                InMemory.Add(InventoryType.Inventory1);
                InMemory.Add(InventoryType.Inventory2);
                InMemory.Add(InventoryType.Inventory3);
                InMemory.Add(InventoryType.Inventory4);
                InMemory.Add(InventoryType.Crystals);
                InMemory.Add(InventoryType.Currency);
                var newBags1 = new InventoryItem[35];
                var newBags2 = new InventoryItem[35];
                var newBags3 = new InventoryItem[35];
                var newBags4 = new InventoryItem[35];
                var bagCount1 = 0;
                var bagCount2 = 0;
                var bagCount3 = 0;
                var bagCount4 = 0;

                //Sort ordering
                if (currentSortOrder.NormalInventories.ContainsKey("PlayerInventory"))
                {
                    var playerInventorySort = currentSortOrder.NormalInventories["PlayerInventory"];


                    for (var index = 0; index < playerInventorySort.Count; index++)
                    {
                        var sort = playerInventorySort[index];
                        InventoryContainer* currentBag;
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

                        if (sort.slotIndex >= currentBag->Size)
                        {
                            _pluginLog.Verbose("bag was too big UwU for player inventory");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    newBags1[bagCount1] = currentBag->Items[sort.slotIndex];
                                    bagCount1++;
                                    break;
                                case 1:
                                    newBags2[bagCount2] = currentBag->Items[sort.slotIndex];
                                    bagCount2++;
                                    break;
                                case 2:
                                    newBags3[bagCount3] = currentBag->Items[sort.slotIndex];
                                    bagCount3++;
                                    break;
                                case 3:
                                    newBags4[bagCount4] = currentBag->Items[sort.slotIndex];
                                    bagCount4++;
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }

                    for (var index = 0; index < newBags1.Length; index++)
                    {
                        var newBag = newBags1[index];
                        newBag.Slot = (short)index;
                        if (!CharacterBag1[index].IsSame(newBag))
                        {
                            CharacterBag1[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory1));
                        }
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        newBag.Slot = (short)index;
                        if (!CharacterBag2[index].IsSame(newBag))                        {
                            CharacterBag2[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory2));
                        }
                    }

                    for (var index = 0; index < newBags3.Length; index++)
                    {
                        var newBag = newBags3[index];
                        newBag.Slot = (short)index;
                        if (!CharacterBag3[index].IsSame(newBag))
                        {
                            CharacterBag3[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory3));
                        }
                    }

                    for (var index = 0; index < newBags4.Length; index++)
                    {
                        var newBag = newBags4[index];
                        newBag.Slot = (short)index;
                        if (!CharacterBag4[index].IsSame(newBag))
                        {
                            CharacterBag4[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory4));
                        }
                    }

                    for (var i = 0; i < crystals->Size; i++)
                    {
                        var item = crystals->Items[i];
                        item.Slot = (short)i;
                        if (!CharacterCrystals[i].IsSame(item))
                        {
                            CharacterCrystals[i] = item;
                            changeSet.Add(new BagChange(item, InventoryType.Crystals));
                        }
                    }

                    short slot = 0;
                    foreach (var currencyItemId in _currencyItemIds)
                    {
                        var itemCount = InventoryManager.Instance()->GetInventoryItemCount(currencyItemId, false, false, false);
                        if (itemCount != 0)
                        {
                            var fakeInventoryItem = new InventoryItem();
                            fakeInventoryItem.ItemId = currencyItemId;
                            fakeInventoryItem.Slot = slot;
                            fakeInventoryItem.Quantity = itemCount;
                            fakeInventoryItem.Container = InventoryType.Currency;
                            fakeInventoryItem.Flags = InventoryItem.ItemFlags.None;
                            fakeInventoryItem.GlamourId = 0;
                            if (!CharacterCurrency[slot].IsSame(fakeInventoryItem))
                            {
                                CharacterCurrency[slot] = fakeInventoryItem;
                                changeSet.Add(new BagChange(fakeInventoryItem, InventoryType.Currency));
                            }
                        }

                        slot++;
                    }
                }

            }
        }

        public unsafe void ParseSaddleBags(InventorySortOrder currentSortOrder, BagChangeContainer changeSet)
        {
            if (currentSortOrder.NormalInventories.ContainsKey("SaddleBag"))
            {
                var saddleBag0 =
                    InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag1);
                var saddleBag1 =
                    InventoryManager.Instance()->GetInventoryContainer(InventoryType.SaddleBag2);
                var saddleBagLeftSort = currentSortOrder.NormalInventories["SaddleBag"];

                if (saddleBag0 != null && saddleBag1 != null && _loadedInventories.Contains(InventoryType.SaddleBag1) &&
                    _loadedInventories.Contains(InventoryType.SaddleBag2))
                {
                    InMemory.Add(InventoryType.SaddleBag1);
                    InMemory.Add(InventoryType.SaddleBag2);
                    var newBags1 = new InventoryItem[35];
                    var newBags2 = new InventoryItem[35];
                    var bagCount1 = 0;
                    var bagCount2 = 0;
                    for (var index = 0; index < saddleBagLeftSort.Count; index++)
                    {
                        var sort = saddleBagLeftSort[index];

                        InventoryContainer* currentBag;
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

                        if (sort.slotIndex >= currentBag->Size)
                        {
                            _pluginLog.Verbose("bag was too big UwU for saddle bag");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    newBags1[bagCount1] = currentBag->Items[sort.slotIndex];
                                    bagCount1++;
                                    break;
                                case 1:
                                    newBags2[bagCount2] = currentBag->Items[sort.slotIndex];
                                    bagCount2++;
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }

                    for (var index = 0; index < newBags1.Length; index++)
                    {
                        var newBag = newBags1[index];
                        newBag.Slot = (short)index;
                        if (!SaddleBag1[index].IsSame(newBag))
                        {
                            SaddleBag1[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.SaddleBag1));
                        }
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        newBag.Slot = (short)index;
                        if (!SaddleBag2[index].IsSame(newBag))
                        {
                            SaddleBag2[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.SaddleBag2));
                        }
                    }
                }
            }
        }

        public unsafe void ParsePremiumSaddleBags(InventorySortOrder currentSortOrder, BagChangeContainer changeSet)
        {
            if (currentSortOrder.NormalInventories.ContainsKey("SaddleBagPremium"))
            {
                var saddleBag0 =
                    InventoryManager.Instance()->GetInventoryContainer(InventoryType.PremiumSaddleBag1);
                var saddleBag1 =
                    InventoryManager.Instance()->GetInventoryContainer(InventoryType.PremiumSaddleBag2);
                var saddleBagLeftSort = currentSortOrder.NormalInventories["SaddleBagPremium"];

                if (saddleBag0 != null && saddleBag1 != null &&
                    _loadedInventories.Contains(InventoryType.PremiumSaddleBag1) &&
                    _loadedInventories.Contains(InventoryType.PremiumSaddleBag2))
                {
                    InMemory.Add(InventoryType.PremiumSaddleBag1);
                    InMemory.Add(InventoryType.PremiumSaddleBag2);
                    var newBags1 = new InventoryItem[35];
                    var newBags2 = new InventoryItem[35];
                    var bagCount1 = 0;
                    var bagCount2 = 0;
                    for (var index = 0; index < saddleBagLeftSort.Count; index++)
                    {
                        var sort = saddleBagLeftSort[index];

                        InventoryContainer* currentBag;
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

                        if (sort.slotIndex >= currentBag->Size)
                        {
                            _pluginLog.Verbose("bag was too big UwU for saddle bag");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    newBags1[bagCount1] = currentBag->Items[sort.slotIndex];
                                    bagCount1++;
                                    break;
                                case 1:
                                    newBags2[bagCount2] = currentBag->Items[sort.slotIndex];
                                    bagCount2++;
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }

                    for (var index = 0; index < newBags1.Length; index++)
                    {
                        var newBag = newBags1[index];
                        newBag.Slot = (short)index;
                        if (!PremiumSaddleBag1[index].IsSame(newBag))
                        {
                            PremiumSaddleBag1[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.PremiumSaddleBag1));
                        }
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        newBag.Slot = (short)index;
                        if (!PremiumSaddleBag2[index].IsSame(newBag))
                        {
                            PremiumSaddleBag2[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.PremiumSaddleBag2));
                        }
                    }
                }
            }
        }

        public unsafe void ParseArmouryChest(InventorySortOrder currentSortOrder, BagChangeContainer changeSet)
        {
            foreach (var armoryChest in _armoryChestTypes)
            {
                InMemory.Add(armoryChest.Value);
                if (currentSortOrder.NormalInventories.ContainsKey(armoryChest.Key))
                {
                    var bagSpace = 35;
                    if (armoryChest.Value == InventoryType.ArmoryMainHand || armoryChest.Value == InventoryType.ArmoryRings) bagSpace = 50;
                    if (armoryChest.Value == InventoryType.ArmorySoulCrystal) bagSpace = 25;
                    var newBags = new InventoryItem[bagSpace];
                    var odrOrdering = currentSortOrder.NormalInventories[armoryChest.Key];
                    var gameOrdering = InventoryManager.Instance()->GetInventoryContainer(armoryChest.Value);


                    if (gameOrdering != null && gameOrdering->IsLoaded)
                        for (var index = 0; index < odrOrdering.Count; index++)
                        {
                            var sort = odrOrdering[index];

                            if (sort.slotIndex >= gameOrdering->Size)
                            {
                                _pluginLog.Verbose("bag was too big UwU for " + armoryChest.Key);
                            }
                            else
                            {
                                InventoryItem[]? bag = null;

                                switch (armoryChest.Value)
                                {
                                    case InventoryType.ArmoryBody:
                                        bag = ArmouryBody;
                                        break;
                                    case InventoryType.ArmoryEar:
                                        bag = ArmouryEars;
                                        break;
                                    case InventoryType.ArmoryFeets:
                                        bag = ArmouryFeet;
                                        break;
                                    case InventoryType.ArmoryHands:
                                        bag = ArmouryHands;
                                        break;
                                    case InventoryType.ArmoryHead:
                                        bag = ArmouryHead;
                                        break;
                                    case InventoryType.ArmoryLegs:
                                        bag = ArmouryLegs;
                                        break;
                                    case InventoryType.ArmoryNeck:
                                        bag = ArmouryNeck;
                                        break;
                                    case InventoryType.ArmoryRings:
                                        bag = ArmouryRings;
                                        break;
                                    case InventoryType.ArmoryWrist:
                                        bag = ArmouryWrists;
                                        break;
                                    case InventoryType.ArmoryMainHand:
                                        bag = ArmouryMainHand;
                                        break;
                                    case InventoryType.ArmoryOffHand:
                                        bag = ArmouryOffHand;
                                        break;
                                    case InventoryType.ArmorySoulCrystal:
                                        bag = ArmourySoulCrystals;
                                        break;
                                }

                                if (bag != null)
                                {
                                    newBags[index] = gameOrdering->Items[sort.slotIndex];
                                    newBags[index].Slot = (short)index;
                                    if (!bag[index].IsSame(newBags[index]))
                                    {
                                        bag[index] = newBags[index];
                                        changeSet.Add(new BagChange(newBags[index], armoryChest.Value));
                                    }
                                }
                            }
                        }
                    else
                        _pluginLog.Verbose("Could generate data for " + armoryChest.Value);
                }
                else
                {
                    _pluginLog.Verbose("Could not find sort order for" + armoryChest.Value);
                }
            }
        }

        public unsafe void ParseCharacterEquipped(BagChangeContainer changeSet)
        {
            var gearSet0 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
            if (gearSet0 != null && gearSet0->IsLoaded)
            {
                InMemory.Add(InventoryType.EquippedItems);
                for (var i = 0; i < gearSet0->Size; i++)
                {
                    var gearItem = gearSet0->Items[i];
                    gearItem.Slot = (short)i;
                    if (!gearItem.IsSame(CharacterEquipped[i]))
                    {
                        CharacterEquipped[i] = gearItem;
                        changeSet.Add(new BagChange(gearItem, InventoryType.EquippedItems));
                    }
                }
            }
        }

        public unsafe void ParseHouseBags(BagChangeContainer changeSet)
        {
            for (var b = 0; b < _houseBagTypes.Length; b++)
            {
                var bagType = _houseBagTypes[b];
                if (_loadedInventories.Contains(bagType))
                {
                    InMemory.Add(bagType);
                    var bag = InventoryManager.Instance()->GetInventoryContainer(bagType);
                    if (bag != null && bag->IsLoaded)
                    {
                        InventoryItem[]? housingItems = null;
                        switch (bagType)
                        {
                            case InventoryType.HousingInteriorPlacedItems1:
                                housingItems = HousingInteriorPlacedItems1;
                                break;
                            case InventoryType.HousingInteriorPlacedItems2:
                                housingItems = HousingInteriorPlacedItems2;
                                break;
                            case InventoryType.HousingInteriorPlacedItems3:
                                housingItems = HousingInteriorPlacedItems3;
                                break;
                            case InventoryType.HousingInteriorPlacedItems4:
                                housingItems = HousingInteriorPlacedItems4;
                                break;
                            case InventoryType.HousingInteriorPlacedItems5:
                                housingItems = HousingInteriorPlacedItems5;
                                break;
                            case InventoryType.HousingInteriorPlacedItems6:
                                housingItems = HousingInteriorPlacedItems6;
                                break;
                            case InventoryType.HousingInteriorPlacedItems7:
                                housingItems = HousingInteriorPlacedItems7;
                                break;
                            case InventoryType.HousingInteriorPlacedItems8:
                                housingItems = HousingInteriorPlacedItems8;
                                break;
                            case InventoryType.HousingInteriorStoreroom1:
                                housingItems = HousingInteriorStoreroom1;
                                break;
                            case InventoryType.HousingInteriorStoreroom2:
                                housingItems = HousingInteriorStoreroom2;
                                break;
                            case InventoryType.HousingInteriorStoreroom3:
                                housingItems = HousingInteriorStoreroom3;
                                break;
                            case InventoryType.HousingInteriorStoreroom4:
                                housingItems = HousingInteriorStoreroom4;
                                break;
                            case InventoryType.HousingInteriorStoreroom5:
                                housingItems = HousingInteriorStoreroom5;
                                break;
                            case InventoryType.HousingInteriorStoreroom6:
                                housingItems = HousingInteriorStoreroom6;
                                break;
                            case InventoryType.HousingInteriorStoreroom7:
                                housingItems = HousingInteriorStoreroom7;
                                break;
                            case InventoryType.HousingInteriorStoreroom8:
                                housingItems = HousingInteriorStoreroom8;
                                break;
                            case InventoryType.HousingInteriorAppearance:
                                housingItems = HousingInteriorAppearance;
                                break;
                            case InventoryType.HousingExteriorAppearance:
                                housingItems = HousingExteriorAppearance;
                                break;
                            case InventoryType.HousingExteriorPlacedItems:
                                housingItems = HousingExteriorPlacedItems;
                                break;
                            case InventoryType.HousingExteriorStoreroom:
                                housingItems = HousingExteriorStoreroom;
                                break;
                        }

                        if (housingItems != null)
                        {
                            for (var i = 0; i < bag->Size; i++)
                            {
                                var houseItem = bag->Items[i];
                                houseItem.Slot = (short)i;
                                if (!houseItem.IsSame(housingItems[i]))
                                {
                                    if (i >= 0 && i < housingItems.Length)
                                    {
                                        housingItems[i] = houseItem;
                                        changeSet.Add(new BagChange(houseItem, bagType));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public unsafe void ParseFreeCompanyBags(BagChangeContainer changeSet)
        {
            for (var b = 0; b < _freeCompanyBagTypes.Length; b++)
            {
                var bagType = _freeCompanyBagTypes[b];
                if (_loadedInventories.Contains(bagType))
                {
                    InMemory.Add(bagType);
                    var bag = InventoryManager.Instance()->GetInventoryContainer(bagType);
                    if (bag != null && bag->IsLoaded)
                    {
                        InventoryItem[]? fcItems = null;
                        switch (bagType)
                        {
                            case InventoryType.FreeCompanyPage1:
                                fcItems = FreeCompanyBag1;
                                break;
                            case InventoryType.FreeCompanyPage2:
                                fcItems = FreeCompanyBag2;
                                break;
                            case InventoryType.FreeCompanyPage3:
                                fcItems = FreeCompanyBag3;
                                break;
                            case InventoryType.FreeCompanyPage4:
                                fcItems = FreeCompanyBag4;
                                break;
                            case InventoryType.FreeCompanyPage5:
                                fcItems = FreeCompanyBag5;
                                break;
                            case InventoryType.FreeCompanyGil:
                                fcItems = FreeCompanyGil;
                                break;
                            case InventoryType.FreeCompanyCrystals:
                                fcItems = FreeCompanyCrystals;
                                break;
                        }

                        if (fcItems != null)
                            for (var i = 0; i < bag->Size; i++)
                            {
                                var fcItem = bag->Items[i];
                                fcItem.Slot = (short)i;
                                if (!fcItem.IsSame(fcItems[i]))
                                {
                                    fcItems[i] = fcItem;
                                    changeSet.Add(new BagChange(fcItem, bagType));
                                }
                            }
                    }
                }
            }

            if (_loadedInventories.Contains((InventoryType)Enums.InventoryType.FreeCompanyCurrency))
            {
                var hasAgent = false;
                var agentFreeCompany = AgentFreeCompany.Instance();
                if (agentFreeCompany != null && agentFreeCompany->IsAgentActive() && agentFreeCompany->AddonId != 0)
                {
                    hasAgent = true;
                }

                if (!hasAgent)
                {
                    var agentFreeCompanyShop = AgentModule.Instance()->GetAgentByInternalId(AgentId.FreeCompanyCreditShop);
                    if (agentFreeCompanyShop != null && agentFreeCompanyShop->IsAgentActive() && agentFreeCompanyShop->AddonId != 0)
                    {
                        hasAgent = true;
                    }
                }

                if (!hasAgent)
                {
                    _pluginLog.Verbose("Cannot scan free company currency as no agent has been loaded in.");
                    InMemory.Remove((InventoryType)Enums.InventoryType.FreeCompanyCurrency);
                    return;
                }
                var atkDataHolder = Framework.Instance()->UIModule->GetRaptureAtkModule()->AtkModule
                    .AtkArrayDataHolder;
                var fcHolder = atkDataHolder.GetNumberArrayData(51);
                var fcCredit = fcHolder->IntArray[9];
                var fcRank = fcHolder->IntArray[4];
                if (fcRank == 0)
                {
                    _pluginLog.Verbose("Cannot scan free company currency as data has not been loaded in.");
                    InMemory.Remove((InventoryType)Enums.InventoryType.FreeCompanyCurrency);
                    return;
                }
                var fakeCreditItem = new InventoryItem();
                fakeCreditItem.ItemId = 80;
                fakeCreditItem.Container = (InventoryType)Enums.InventoryType.FreeCompanyCurrency;
                fakeCreditItem.Quantity = fcCredit;
                fakeCreditItem.Slot = 0;
                fakeCreditItem.Flags = InventoryItem.ItemFlags.None;
                InMemory.Add((InventoryType)Enums.InventoryType.FreeCompanyCurrency);
                if (!fakeCreditItem.IsSame(FreeCompanyCurrency[0]) && (fakeCreditItem.Quantity != 0 || FreeCompanyCurrency[0].Quantity == 0))
                {
                    FreeCompanyCurrency[0] = fakeCreditItem;
                    changeSet.Add(new BagChange(fakeCreditItem,
                        (InventoryType)Enums.InventoryType.FreeCompanyCurrency));
                }
            }
        }

        public unsafe void ParseArmoire(BagChangeContainer changeSet)
        {
            if (!_loadedInventories.Contains((InventoryType)Enums.InventoryType.Armoire))
            {
                return;
            }

            var uiState = UIState.Instance();
            if (uiState == null)
            {
                return;
            }
            if (!uiState->Cabinet.IsCabinetLoaded()) return;

            InMemory.Add((InventoryType)Enums.InventoryType.Armoire);

            var index = 0;
            for (uint rowId = _cabinetSheet.StartRow; rowId < _cabinetSheet.StartRow + _cabinetSheet.Count; rowId++)
            {
                var itemId = _cabinetSheet[rowId].Item.RowId;
                var isInArmoire = _gameInterface.IsInArmoire(itemId);
                var armoireItem = new InventoryItem
                {
                    Slot = (short)index, ItemId = isInArmoire ? itemId : 0, Quantity = isInArmoire ? 1 : 0,
                    Flags = InventoryItem.ItemFlags.None
                };
                if (index >= 0 && index < Armoire.Length)
                {
                    if (!armoireItem.IsSame(Armoire[index]))
                    {
                        Armoire[index] = armoireItem;
                        //Push a custom inventory type
                        changeSet.Add(new BagChange(armoireItem, (InventoryType)Enums.InventoryType.Armoire));
                    }
                }
                else
                {
                    _pluginLog.Error($"Armoire under/overflowed, attempted to access {index} but armoire can only fit {Armoire.Length}");
                    break;
                }

                index++;
            }
        }

        //I don't like this solution but unless I go and hook the glamour chest network request(which I may do) this will have to do
        private bool _glamourAgentActive;
        private readonly TimeSpan _glamourAgentWait = TimeSpan.FromMilliseconds(2000);
        private DateTime? _glamourAgentOpened;
        public unsafe void ParseGlamourChest(BagChangeContainer changeSet)
        {
            var agents = Framework.Instance()->UIModule->GetAgentModule();
            var dresserAgent = (AgentMiragePrismPrismBox*)agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
            if (agents == null || dresserAgent == null || !dresserAgent->IsAgentActive())
            {
                _glamourAgentActive = false;
                return;
            }
            if (!_glamourAgentActive && _glamourAgentOpened == null)
            {
                _glamourAgentOpened = DateTime.Now + _glamourAgentWait;
            }

            if (_glamourAgentOpened >= DateTime.Now)
            {
                return;
            }

            _glamourAgentActive = true;
            _glamourAgentOpened = null;

            InMemory.Add((InventoryType)Enums.InventoryType.GlamourChest);
            HashSet<uint> currentSets = new();

            short index = 0;

            for (var i = 0; i < 8000; i++)
            {
                var chestItem = dresserAgent->Data->PrismBoxItems[i];
                var itemId = chestItem.ItemId;
                if (itemId >= 1_000_000)
                {
                    itemId -= 1_000_000;
                }

                if (_mirageSetLookup.ContainsKey(itemId))
                {
                    currentSets.Add(itemId);
                }
            }



            for (var i = 0; i < 8000; i++)
            {
                var chestItem = dresserAgent->Data->PrismBoxItems[i];
                var flags = InventoryItem.ItemFlags.None;
                var itemId = chestItem.ItemId;
                if (itemId >= 1_000_000)
                {
                    itemId -= 1_000_000;
                    flags = InventoryItem.ItemFlags.HighQuality;
                }

                var glamourItem = new InventoryItem
                {
                    Slot = (short)chestItem.Slot, ItemId = itemId, Quantity = itemId != 0 ? 1 : 0, Flags = flags, SpiritbondOrCollectability = (ushort)index
                };
                glamourItem.Stains[0] = chestItem.Stains[0];
                glamourItem.Stains[1] = chestItem.Stains[1];
                if (_mirageSetItemLookup.ContainsKey(itemId))
                {
                    var potentialSets = _mirageSetItemLookup[itemId];
                    foreach (var potentialSet in potentialSets)
                    {
                        if (currentSets.Contains(potentialSet))
                        {
                            glamourItem.GlamourId = potentialSet;
                        }
                    }
                }

                if (index >= 0 && index < GlamourChest.Length)
                {
                    if (!glamourItem.IsSame(GlamourChest[index], false))
                    {
                        GlamourChest[index] = glamourItem;
                        //Push a custom inventory type
                        changeSet.Add(new BagChange(glamourItem, (InventoryType)Enums.InventoryType.GlamourChest));
                    }
                }
                else
                {
                    _pluginLog.Verbose($"Glamour chest appears to be longer than {GlamourChest.Length}, hit {index}.");
                }

                index++;
            }
        }

        public unsafe void ParseRetainerBags(InventorySortOrder currentSortOrder, BagChangeContainer changeSet)
        {
            var currentRetainer = _characterMonitor.ActiveRetainerId;
            if (currentRetainer != 0 && _loadedInventories.Contains(InventoryType.RetainerPage1) &&
                _loadedInventories.Contains(InventoryType.RetainerPage2) &&
                _loadedInventories.Contains(InventoryType.RetainerPage3) &&
                _loadedInventories.Contains(InventoryType.RetainerPage4) &&
                _loadedInventories.Contains(InventoryType.RetainerPage5) &&
                _loadedInventories.Contains(InventoryType.RetainerPage6) &&
                _loadedInventories.Contains(InventoryType.RetainerPage7) &&
                _loadedInventories.Contains(InventoryType.RetainerEquippedItems) &&
                _loadedInventories.Contains(InventoryType.RetainerGil) &&
                _loadedInventories.Contains(InventoryType.RetainerCrystals) &&
                _loadedInventories.Contains(InventoryType.RetainerMarket)
               )
            {
                var marketOrder = _marketOrderService.GetCurrentOrder();

                if (!InMemoryRetainers.ContainsKey(currentRetainer))
                    InMemoryRetainers.Add(currentRetainer, new HashSet<InventoryType>());
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage1);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage2);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage3);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage4);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage5);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage6);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerPage7);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerEquippedItems);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerGil);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerCrystals);
                InMemoryRetainers[currentRetainer].Add(InventoryType.RetainerMarket);

                    if (!RetainerBag1.ContainsKey(currentRetainer))
                        RetainerBag1.Add(currentRetainer, new InventoryItem[35]);
                    if (!RetainerBag2.ContainsKey(currentRetainer))
                        RetainerBag2.Add(currentRetainer, new InventoryItem[35]);
                    if (!RetainerBag3.ContainsKey(currentRetainer))
                        RetainerBag3.Add(currentRetainer, new InventoryItem[35]);
                    if (!RetainerBag4.ContainsKey(currentRetainer))
                        RetainerBag4.Add(currentRetainer, new InventoryItem[35]);
                    if (!RetainerBag5.ContainsKey(currentRetainer))
                        RetainerBag5.Add(currentRetainer, new InventoryItem[35]);
                    if (!RetainerEquipped.ContainsKey(currentRetainer))
                        RetainerEquipped.Add(currentRetainer, new InventoryItem[14]);
                    if (!RetainerMarket.ContainsKey(currentRetainer))
                        RetainerMarket.Add(currentRetainer, new InventoryItem[20]);
                    if (!RetainerMarketPrices.ContainsKey(currentRetainer))
                        RetainerMarketPrices.Add(currentRetainer, new uint[20]);
                    if (!RetainerGil.ContainsKey(currentRetainer))
                        RetainerGil.Add(currentRetainer, new InventoryItem[1]);
                    if (!RetainerCrystals.ContainsKey(currentRetainer))
                        RetainerCrystals.Add(currentRetainer, new InventoryItem[18]);
                    //Actual inventories
                    var retainerBag1 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage1);
                    var retainerBag2 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage2);
                    var retainerBag3 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage3);
                    var retainerBag4 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage4);
                    var retainerBag5 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage5);
                    var retainerBag6 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage6);
                    var retainerBag7 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerPage7);
                    var retainerEquippedItems =
                        InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerEquippedItems);
                    var retainerMarketItems =
                        InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerMarket);
                    var retainerGil = InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerGil);
                    var retainerCrystal =
                        InventoryManager.Instance()->GetInventoryContainer(InventoryType.RetainerCrystals);

                    RetainerSortOrder retainerInventory;
                    //Sort ordering
                    if (currentSortOrder.RetainerInventories.ContainsKey(currentRetainer))
                    {
                         retainerInventory = currentSortOrder.RetainerInventories[currentRetainer];
                    }
                    else
                    {
                        retainerInventory = RetainerSortOrder.NoOdrOrder;
                    }

                    for (var i = 0; i < retainerEquippedItems->Size; i++)
                    {
                        var retainerItem = retainerEquippedItems->Items[i];
                        retainerItem.Slot = (short)i;
                        if (!retainerItem.IsSame(RetainerEquipped[currentRetainer][i]))
                        {
                            RetainerEquipped[currentRetainer][i] = retainerItem;
                            changeSet.Add(new BagChange(retainerItem, InventoryType.RetainerEquippedItems));
                        }
                    }


                    var retainerGilItem = retainerGil->Items[0];
                    retainerGilItem.Slot = 0;
                    if (!retainerGilItem.IsSame(RetainerGil[currentRetainer][0]))
                    {
                        RetainerGil[currentRetainer][0] = retainerGilItem;
                        changeSet.Add(new BagChange(retainerGilItem, InventoryType.RetainerGil));
                    }


                    for (var i = 0; i < retainerCrystal->Size; i++)
                    {
                        var retainerItem = retainerCrystal->Items[i];
                        retainerItem.Slot = (short)i;
                        if (!retainerItem.IsSame(RetainerCrystals[currentRetainer][i]))
                        {
                            RetainerCrystals[currentRetainer][i] = retainerItem;
                            changeSet.Add(new BagChange(retainerItem, InventoryType.RetainerCrystals));
                        }
                    }

                    var retainerMarketCopy = new InventoryItem[20];

                    if (marketOrder != null)
                    {

                        for (var i = 0; i < retainerMarketItems->Size; i++)
                        {
                            if (marketOrder.TryGetValue(i, out var value))
                            {
                                retainerMarketCopy[value] = retainerMarketItems->Items[i];
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < retainerMarketItems->Size; i++)
                        {
                            retainerMarketCopy[i] = retainerMarketItems->Items[i];
                        }

                        retainerMarketCopy = _marketOrderService.SortByBackupRetainerMarketOrder(retainerMarketCopy.ToList()).ToArray();
                    }

                    retainerMarketCopy = retainerMarketCopy.ToArray();
                    for (var i = 0; i < retainerMarketCopy.Length; i++)
                    {
                        var retainerItem = retainerMarketCopy[i];
                        if (_cachedRetainerMarketPrices.ContainsKey(currentRetainer))
                        {
                            var cachedPrice = _cachedRetainerMarketPrices[currentRetainer][retainerItem.Slot];
                            retainerItem.Slot = (short)i;
                            if (!retainerItem.IsSame(RetainerMarket[currentRetainer][i]) ||
                                cachedPrice != RetainerMarketPrices[currentRetainer][i])
                            {
                                RetainerMarket[currentRetainer][i] = retainerItem;
                                RetainerMarketPrices[currentRetainer][i] = cachedPrice;
                                changeSet.Add(new BagChange(retainerItem, InventoryType.RetainerMarket));
                            }
                        }
                    }
                    //Probably some way we can calculate the order then just update that


                    var newBags1 = new InventoryItem[25];
                    var newBags2 = new InventoryItem[25];
                    var newBags3 = new InventoryItem[25];
                    var newBags4 = new InventoryItem[25];
                    var newBags5 = new InventoryItem[25];
                    var newBags6 = new InventoryItem[25];
                    var newBags7 = new InventoryItem[25];
                    var bagCount1 = 0;
                    var bagCount2 = 0;
                    var bagCount3 = 0;
                    var bagCount4 = 0;
                    var bagCount5 = 0;
                    var bagCount6 = 0;
                    var bagCount7 = 0;

                    for (var index = 0; index < retainerInventory.InventoryCoords.Count; index++)
                    {
                        var sort = retainerInventory.InventoryCoords[index];
                        InventoryContainer* currentBag;
                        switch (sort.containerIndex)
                        {
                            case 0:
                                currentBag = retainerBag1;
                                break;
                            case 1:
                                currentBag = retainerBag2;
                                break;
                            case 2:
                                currentBag = retainerBag3;
                                break;
                            case 3:
                                currentBag = retainerBag4;
                                break;
                            case 4:
                                currentBag = retainerBag5;
                                break;
                            case 5:
                                currentBag = retainerBag6;
                                break;
                            case 6:
                                currentBag = retainerBag7;
                                break;
                            default:
                                continue;
                        }

                        if (sort.slotIndex >= currentBag->Size)
                        {
                            _pluginLog.Verbose("bag was too big UwU retainer");
                        }
                        else
                        {
                            var sortedBagIndex = index / 25;
                            switch (sortedBagIndex)
                            {
                                case 0:
                                    newBags1[bagCount1] = currentBag->Items[sort.slotIndex];
                                    bagCount1++;
                                    break;
                                case 1:
                                    newBags2[bagCount2] = currentBag->Items[sort.slotIndex];
                                    bagCount2++;
                                    break;
                                case 2:
                                    newBags3[bagCount3] = currentBag->Items[sort.slotIndex];
                                    bagCount3++;
                                    break;
                                case 3:
                                    newBags4[bagCount4] = currentBag->Items[sort.slotIndex];
                                    bagCount4++;
                                    break;
                                case 4:
                                    newBags5[bagCount5] = currentBag->Items[sort.slotIndex];
                                    bagCount5++;
                                    break;
                                case 5:
                                    newBags6[bagCount6] = currentBag->Items[sort.slotIndex];
                                    bagCount6++;
                                    break;
                                case 6:
                                    newBags7[bagCount7] = currentBag->Items[sort.slotIndex];
                                    bagCount7++;
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }

                    var retainerBags = new List<InventoryType>
                    {
                        InventoryType.RetainerPage1, InventoryType.RetainerPage2, InventoryType.RetainerPage3,
                        InventoryType.RetainerPage4, InventoryType.RetainerPage5
                    };
                    var absoluteIndex = 0;
                    for (var index = 0; index < newBags1.Length; index++)
                    {
                        var newItem = newBags1[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newItem.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newItem.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newItem.Slot].IsSame(newItem))
                            {
                                bag[newItem.Slot] = newItem;
                                changeSet.Add(new BagChange(newItem, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newBag.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newBag.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newBag.Slot].IsSame(newBag))
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }

                    for (var index = 0; index < newBags3.Length; index++)
                    {
                        var newBag = newBags3[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newBag.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newBag.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newBag.Slot].IsSame(newBag))
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }

                    for (var index = 0; index < newBags4.Length; index++)
                    {
                        var newBag = newBags4[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newBag.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newBag.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newBag.Slot].IsSame(newBag))
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }

                    for (var index = 0; index < newBags5.Length; index++)
                    {
                        var newBag = newBags5[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newBag.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newBag.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newBag.Slot].IsSame(newBag))
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }

                    for (var index = 0; index < newBags6.Length; index++)
                    {
                        var newBag = newBags6[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newBag.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newBag.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newBag.Slot].IsSame(newBag))
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }

                    for (var index = 0; index < newBags7.Length; index++)
                    {
                        var newBag = newBags7[index];
                        var sortedBagIndex = absoluteIndex / 35;
                        if (sortedBagIndex >= 0 && retainerBags.Count > sortedBagIndex)
                        {
                            newBag.Slot = (short)(absoluteIndex - sortedBagIndex * 35);
                            if (retainerBags.Count > sortedBagIndex) newBag.Container = retainerBags[sortedBagIndex];

                            var bag = GetInventoryByType(currentRetainer, retainerBags[sortedBagIndex]);
                            if (!bag[newBag.Slot].IsSame(newBag))
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }
            }
        }

        public unsafe bool ParseGearSets(BagChangeContainer changeSet)
        {
            bool gearSetsChanged = false;
            var gearSetModule = RaptureGearsetModule.Instance();
            if (gearSetModule == null)
            {
                return false;
            }

            for (byte i = 0; i < gearSetModule->Entries.Length; i++)
            {
                var gearSet = gearSetModule->Entries[i];
                if (gearSet.Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
                {
                    if (GearSetsUsed[i] != true)
                    {
                        GearSetsUsed[i] = true;
                        gearSetsChanged = true;
                    }

                    var gearSetName = gearSet.NameString;
                    GearSetNames[i] = gearSetName;



                    var gearSetItems = new[]
                    {
                        gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.MainHand), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.OffHand), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Head), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Body), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Hands),
                        gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Legs), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Feet), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Ears), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Neck), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.Wrists), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.RingRight),
                        gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.RingLeft), gearSet.GetItem(RaptureGearsetModule.GearsetItemIndex.SoulStone)
                    };
                    if (!GearSets.ContainsKey(i))
                    {
                        GearSets.Add(i, new uint[gearSetItems.Length]);
                    }
                    for (var index = 0; index < gearSetItems.Length; index++)
                    {
                        var gearSetItem = gearSetItems[index];
                        var itemId = gearSetItem.ItemId;
                        if (GearSets[i][index] != itemId)
                        {
                            gearSetsChanged = true;
                            GearSets[i][index] = itemId;
                        }
                    }
                }
                else
                {
                    if (GearSetsUsed[i] != false)
                    {
                        gearSetsChanged = true;
                        GearSetsUsed[i] = false;
                    }
                }
            }

            return gearSetsChanged;
        }

        private bool _disposed;
        private Dictionary<string, InventoryType> _armoryChestTypes = new()
        {
            { "ArmouryMainHand", InventoryType.ArmoryMainHand },
            { "ArmouryHead", InventoryType.ArmoryHead },
            { "ArmouryBody", InventoryType.ArmoryBody },
            { "ArmouryHands", InventoryType.ArmoryHands },
            { "ArmouryLegs", InventoryType.ArmoryLegs },
            { "ArmouryFeet", InventoryType.ArmoryFeets },
            { "ArmouryOffHand", InventoryType.ArmoryOffHand },
            { "ArmouryEars", InventoryType.ArmoryEar },
            { "ArmouryNeck", InventoryType.ArmoryNeck },
            { "ArmouryWrists", InventoryType.ArmoryWrist },
            { "ArmouryRings", InventoryType.ArmoryRings },
            { "ArmourySoulCrystals", InventoryType.ArmorySoulCrystal }
        };

        private InventoryType[] _houseBagTypes = {
            InventoryType.HousingInteriorPlacedItems1,
            InventoryType.HousingInteriorPlacedItems2,
            InventoryType.HousingInteriorPlacedItems3,
            InventoryType.HousingInteriorPlacedItems4,
            InventoryType.HousingInteriorPlacedItems5,
            InventoryType.HousingInteriorPlacedItems6,
            InventoryType.HousingInteriorPlacedItems7,
            InventoryType.HousingInteriorPlacedItems8,
            InventoryType.HousingInteriorStoreroom1,
            InventoryType.HousingInteriorStoreroom2,
            InventoryType.HousingInteriorStoreroom3,
            InventoryType.HousingInteriorStoreroom4,
            InventoryType.HousingInteriorStoreroom5,
            InventoryType.HousingInteriorStoreroom6,
            InventoryType.HousingInteriorStoreroom7,
            InventoryType.HousingInteriorStoreroom8,
            InventoryType.HousingExteriorAppearance,
            InventoryType.HousingInteriorAppearance,
            InventoryType.HousingExteriorPlacedItems,
            InventoryType.HousingExteriorStoreroom,
        };

        private readonly InventoryType[] _freeCompanyBagTypes = {
            InventoryType.FreeCompanyPage1, InventoryType.FreeCompanyPage2, InventoryType.FreeCompanyPage3,
            InventoryType.FreeCompanyPage4, InventoryType.FreeCompanyPage5, InventoryType.FreeCompanyGil,
            InventoryType.FreeCompanyCrystals
        };

        private readonly Dictionary<uint,HashSet<uint>> _mirageSetLookup;
        private readonly Dictionary<uint,HashSet<uint>> _mirageSetItemLookup;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                _pluginLog.Verbose("Disposing {type} ({this})", GetType().Name, this);
                _running = false;
                _framework.Update -= FrameworkOnUpdate;
                _containerInfoNetworkHook?.Dispose();
                _itemMarketBoardInfoHook?.Dispose();
                _containerInfoNetworkHook = null;
                _itemMarketBoardInfoHook = null;
                _clientState.Logout -= ClientStateOnLogout;
                _characterMonitor.OnActiveRetainerChanged -= CharacterMonitorOnOnActiveRetainerChanged;
                _characterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
                _characterMonitor.OnActiveFreeCompanyChanged -= CharacterMonitorOnOnActiveFreeCompanyChanged;
                _characterMonitor.OnActiveHouseChanged -= CharacterMonitorOnOnActiveHouseChanged;
                _odrScanner.OnSortOrderChanged -= SortOrderChanged;
                _gameUiManager.UiVisibilityChanged -= GameUiManagerOnUiManagerVisibilityChanged;
            }
            _disposed = true;
        }

        ~InventoryScanner()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                _pluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }

    public class BagChange
    {
        public BagChange(InventoryItem inventoryItem, InventoryType inventoryType)
        {
            Item = inventoryItem;
            InventoryType = inventoryType;
        }
        public InventoryType InventoryType { get; }
        public InventoryItem Item { get; }
    }
}