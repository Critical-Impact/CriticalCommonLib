using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.GameStructs;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services.Ui;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Services
{
    public class InventoryScanner : IInventoryScanner
    {
        private bool _running;
        private readonly ICharacterMonitor _characterMonitor;
        private readonly IGameUiManager _gameUiManager;
        private IGameInterface _gameInterface;
        private OdrScanner _odrScanner;

        public InventoryScanner(ICharacterMonitor characterMonitor, IGameUiManager gameUiManager,
            IGameInterface gameInterface, OdrScanner odrScanner)
        {
            SignatureHelper.Initialise(this);
            _containerInfoNetworkHook?.Enable();
            _itemMarketBoardInfoHook?.Enable();
            _gameUiManager = gameUiManager;
            _characterMonitor = characterMonitor;
            _gameInterface = gameInterface;
            _odrScanner = odrScanner;
            _gameUiManager.UiVisibilityChanged += GameUiManagerOnUiManagerVisibilityChanged;
            _characterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
            _characterMonitor.OnActiveRetainerChanged += CharacterMonitorOnOnActiveRetainerChanged;
            Armoire = new InventoryItem[Service.ExcelCache.GetCabinetSheet().Count()];
            Task.Run(() => ParseBags());
        }

        private void GameUiManagerOnUiManagerVisibilityChanged(WindowName windowName, bool? isWindowVisible)
        {
            if (windowName is WindowName.InventoryBuddy or WindowName.InventoryBuddy2 && isWindowVisible.HasValue &&
                isWindowVisible.Value)
            {
                _loadedInventories.Add(InventoryType.SaddleBag1);
                _loadedInventories.Add(InventoryType.SaddleBag2);
                _loadedInventories.Add(InventoryType.PremiumSaddleBag1);
                _loadedInventories.Add(InventoryType.PremiumSaddleBag2);
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
                PluginLog.Debug("Character has been cleared, clearing cache");
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
        [Signature("4C 8B C2 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 44 8B 09", DetourName = nameof(ContainerInfoDetour), UseFlags = SignatureUseFlags.Hook)]
        private readonly Hook<ContainerInfoNetworkData>? _containerInfoNetworkHook = null;

        [Signature(
            "E9 ?? ?? ?? ?? 48 8B D3 8B CE 48 8B 7C 24 ?? 48 8B 5C 24 ?? 48 83 C4 50 5E E9 ?? ?? ?? ?? 48 8D 53 10",
            DetourName = nameof(ItemMarketBoardInfoDetour))]
        private readonly Hook<ItemMarketBoardInfoData>? _itemMarketBoardInfoHook = null;

        private readonly HashSet<InventoryType> _loadedInventories = new();
        private readonly uint[] _cachedRetainerMarketPrices = new uint[20];


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
                        PluginLog.Debug("Container update " + containerInfo.containerId.ToString());
                        var inventoryType = (InventoryType)containerInfo.containerId;
                        //Delay just in case the items haven't loaded.
                        Service.Framework.RunOnTick(() =>
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
                PluginLog.Error(e, "shits broke yo");
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
                    if (Enum.IsDefined(typeof(InventoryType), containerInfo.containerId) &&
                        containerInfo.containerId != 0)
                        _cachedRetainerMarketPrices[containerInfo.slot] = containerInfo.unitPrice;
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "shits broke yo");
            }


            return _itemMarketBoardInfoHook!.Original(seq, a3);
        }

        public void ParseBags()
        {
            if (_disposed)
            {
                return;
            }
            if (Service.ClientState.LocalPlayer != null && _running)
            {
                var changeSet = new List<BagChange>();
                var inventorySortOrder = _odrScanner.SortOrder;
                if(inventorySortOrder != null)
                {
                    ParseCharacterBags(inventorySortOrder, changeSet);
                    ParseSaddleBags(inventorySortOrder, changeSet);
                    ParsePremiumSaddleBags(inventorySortOrder, changeSet);
                    ParseArmouryChest(inventorySortOrder, changeSet);
                    ParseCharacterEquipped(inventorySortOrder, changeSet);
                    ParseFreeCompanyBags(inventorySortOrder, changeSet);
                    ParseArmoire(inventorySortOrder, changeSet);
                    ParseGlamourChest(inventorySortOrder, changeSet);
                    ParseRetainerBags(inventorySortOrder, changeSet);
                    ParseGearSets(inventorySortOrder, changeSet);
                }


                if (changeSet.Count != 0)
                {
                    Service.Framework.RunOnFrameworkThread(() => LogBagChanges(changeSet));
                    Service.Framework.RunOnFrameworkThread(() => BagsChanged?.Invoke(changeSet));
                }
            }

            try
            {
                Service.Framework.RunOnTick(() => Task.Run(ParseBags), TimeSpan.FromMilliseconds(500));
            }
            catch (Exception e)
            {
                Service.Framework.RunOnFrameworkThread(() => PluginLog.Error("The inventory scanner has crashed. Details below:"));
                Service.Framework.RunOnFrameworkThread(() => PluginLog.Error(e.ToString()));
                Service.Framework.RunOnFrameworkThread(() => PluginLog.Error("Attempting to restart the scanner in 20 seconds."));
                Service.Framework.RunOnTick(() => Task.Run(ParseBags), TimeSpan.FromMilliseconds(20000));
            }
        }

        public void LogBagChanges(List<BagChange> bagChanges)
        {
            foreach (var bag in bagChanges)
            {
                PluginLog.Debug(bag.Item.ItemID.ToString() + " - " + bag.InventoryType.ToString() + " - " +
                              bag.Item.Slot + " - " + bag.Item.HashCode());
            }
            PluginLog.Debug("Summary: " + bagChanges.Count + " total changes to bags.");
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
                case InventoryType.FreeCompanyCrystals:
                    return FreeCompanyCrystals;
                case (InventoryType)2500:
                    return Armoire;
                case (InventoryType)2501:
                    return GlamourChest;
            }

            return Array.Empty<InventoryItem>();
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
        }

        public HashSet<InventoryType> InMemory { get; } = new();
        public Dictionary<ulong, HashSet<InventoryType>> InMemoryRetainers { get; } = new();
        public InventoryItem[] CharacterBag1 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterBag2 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterBag3 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterBag4 { get; } = new InventoryItem[35];
        public InventoryItem[] CharacterEquipped { get; } = new InventoryItem[14];
        public InventoryItem[] CharacterCrystals { get; } = new InventoryItem[18];
        public InventoryItem[] CharacterCurrency { get; } = new InventoryItem[11];

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
        public InventoryItem[] ArmourySoulCrystals { get; } = new InventoryItem[23];


        public InventoryItem[] FreeCompanyBag1 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag2 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag3 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag4 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyBag5 { get; } = new InventoryItem[50];
        public InventoryItem[] FreeCompanyGil { get; } = new InventoryItem[11];
        public InventoryItem[] FreeCompanyCrystals { get; } = new InventoryItem[18];

        public InventoryItem[] Armoire { get; }
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

        public unsafe void ParseCharacterBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var bag0 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1);
            var bag1 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2);
            var bag2 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3);
            var bag3 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4);
            var crystals = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Crystals);
            var currency = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
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
                            PluginLog.Verbose("bag was too big UwU for player inventory");
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
                        if (CharacterBag1[index].HashCode() != newBag.HashCode())
                        {
                            CharacterBag1[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory1));
                        }
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        newBag.Slot = (short)index;
                        if (CharacterBag2[index].HashCode() != newBag.HashCode())
                        {
                            CharacterBag2[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory2));
                        }
                    }

                    for (var index = 0; index < newBags3.Length; index++)
                    {
                        var newBag = newBags3[index];
                        newBag.Slot = (short)index;
                        if (CharacterBag3[index].HashCode() != newBag.HashCode())
                        {
                            CharacterBag3[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory3));
                        }
                    }

                    for (var index = 0; index < newBags4.Length; index++)
                    {
                        var newBag = newBags4[index];
                        newBag.Slot = (short)index;
                        if (CharacterBag4[index].HashCode() != newBag.HashCode())
                        {
                            CharacterBag4[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.Inventory4));
                        }
                    }

                    for (var i = 0; i < crystals->Size; i++)
                    {
                        var item = crystals->Items[i];
                        item.Slot = (short)i;
                        if (item.HashCode() != CharacterCrystals[i].HashCode())
                        {
                            CharacterCrystals[i] = item;
                            changeSet.Add(new BagChange(item, InventoryType.Crystals));
                        }
                    }

                    for (var i = 0; i < currency->Size; i++)
                    {
                        var item = currency->Items[i];
                        item.Slot = (short)i;
                        if (item.HashCode() != CharacterCurrency[i].HashCode())
                        {
                            CharacterCurrency[i] = item;
                            changeSet.Add(new BagChange(item, InventoryType.Currency));
                        }
                    }
                }
            }
        }

        public unsafe void ParseSaddleBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
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
                            PluginLog.Verbose("bag was too big UwU for saddle bag");
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
                        if (SaddleBag1[index].HashCode() != newBag.HashCode())
                        {
                            SaddleBag1[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.SaddleBag1));
                        }
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        newBag.Slot = (short)index;
                        if (SaddleBag2[index].HashCode() != newBag.HashCode())
                        {
                            SaddleBag2[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.SaddleBag2));
                        }
                    }
                }
            }
        }

        public unsafe void ParsePremiumSaddleBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
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
                            PluginLog.Verbose("bag was too big UwU for saddle bag");
                        }
                        else
                        {
                            var sortedBagIndex = index / 35;
                            List<InventoryItem> currentSortBag;
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
                        if (PremiumSaddleBag1[index].HashCode() != newBag.HashCode())
                        {
                            PremiumSaddleBag1[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.PremiumSaddleBag1));
                        }
                    }

                    for (var index = 0; index < newBags2.Length; index++)
                    {
                        var newBag = newBags2[index];
                        newBag.Slot = (short)index;
                        if (PremiumSaddleBag2[index].HashCode() != newBag.HashCode())
                        {
                            PremiumSaddleBag2[index] = newBag;
                            changeSet.Add(new BagChange(newBag, InventoryType.PremiumSaddleBag2));
                        }
                    }
                }
            }
        }

        public unsafe void ParseArmouryChest(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var inventoryTypes = new Dictionary<string, InventoryType>();
            inventoryTypes.Add("ArmouryMainHand", InventoryType.ArmoryMainHand);
            inventoryTypes.Add("ArmouryHead", InventoryType.ArmoryHead);
            inventoryTypes.Add("ArmouryBody", InventoryType.ArmoryBody);
            inventoryTypes.Add("ArmouryHands", InventoryType.ArmoryHands);
            inventoryTypes.Add("ArmouryLegs", InventoryType.ArmoryLegs);
            inventoryTypes.Add("ArmouryFeet", InventoryType.ArmoryFeets);
            inventoryTypes.Add("ArmouryOffHand", InventoryType.ArmoryOffHand);
            inventoryTypes.Add("ArmouryEars", InventoryType.ArmoryEar);
            inventoryTypes.Add("ArmouryNeck", InventoryType.ArmoryNeck);
            inventoryTypes.Add("ArmouryWrists", InventoryType.ArmoryWrist);
            inventoryTypes.Add("ArmouryRings", InventoryType.ArmoryRings);
            inventoryTypes.Add("ArmourySoulCrystals", InventoryType.ArmorySoulCrystal);

            foreach (var armoryChest in inventoryTypes)
            {
                InMemory.Add(armoryChest.Value);
                if (currentSortOrder.NormalInventories.ContainsKey(armoryChest.Key))
                {
                    var bagSpace = 35;
                    if (armoryChest.Value == InventoryType.ArmoryMainHand || armoryChest.Value == InventoryType.ArmoryRings) bagSpace = 50;
                    if (armoryChest.Value == InventoryType.ArmorySoulCrystal) bagSpace = 23;
                    var newBags = new InventoryItem[bagSpace];
                    var odrOrdering = currentSortOrder.NormalInventories[armoryChest.Key];
                    var gameOrdering = InventoryManager.Instance()->GetInventoryContainer(armoryChest.Value);


                    if (gameOrdering != null && gameOrdering->Loaded != 0)
                        for (var index = 0; index < odrOrdering.Count; index++)
                        {
                            var sort = odrOrdering[index];

                            if (sort.slotIndex >= gameOrdering->Size)
                            {
                                PluginLog.Verbose("bag was too big UwU for " + armoryChest.Key);
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
                                    if (bag[index].HashCode() != newBags[index].HashCode())
                                    {
                                        bag[index] = newBags[index];
                                        changeSet.Add(new BagChange(newBags[index], armoryChest.Value));
                                    }
                                }
                            }
                        }
                    else
                        PluginLog.Verbose("Could generate data for " + armoryChest.Value);
                }
                else
                {
                    PluginLog.Verbose("Could not find sort order for" + armoryChest.Value);
                }
            }
        }

        public unsafe void ParseCharacterEquipped(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var gearSet0 = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
            if (gearSet0 != null && gearSet0->Loaded != 0)
            {
                InMemory.Add(InventoryType.EquippedItems);
                for (var i = 0; i < gearSet0->Size; i++)
                {
                    var gearItem = gearSet0->Items[i];
                    gearItem.Slot = (short)i;
                    if (gearItem.HashCode() != CharacterEquipped[i].HashCode())
                    {
                        CharacterEquipped[i] = gearItem;
                        changeSet.Add(new BagChange(gearItem, InventoryType.EquippedItems));
                    }
                }
            }
        }

        public unsafe void ParseFreeCompanyBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var bags = new[]
            {
                InventoryType.FreeCompanyPage1, InventoryType.FreeCompanyPage2, InventoryType.FreeCompanyPage3,
                InventoryType.FreeCompanyPage4, InventoryType.FreeCompanyPage5, InventoryType.FreeCompanyGil,
                InventoryType.FreeCompanyCrystals
            };

            for (var b = 0; b < bags.Length; b++)
            {
                var bagType = bags[b];
                if (_loadedInventories.Contains(bagType))
                {
                    InMemory.Add(bagType);
                    var bag = InventoryManager.Instance()->GetInventoryContainer(bagType);
                    if (bag != null && bag->Loaded != 0)
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
                                if (fcItem.HashCode() != fcItems[i].HashCode())
                                {
                                    fcItems[i] = fcItem;
                                    changeSet.Add(new BagChange(fcItem, bagType));
                                }
                            }
                    }
                }
            }
        }

        public unsafe void ParseArmoire(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var uiState = UIState.Instance();
            if (uiState == null)
            {
                return;
            }
            if (!uiState->Cabinet.IsCabinetLoaded()) return;
            InMemory.Add((InventoryType)2500);

            var index = 0;
            foreach (var row in Service.ExcelCache.GetCabinetSheet())
            {
                var itemId = row.Item.Row;
                var isInArmoire = _gameInterface.IsInArmoire(itemId);
                var armoireItem = new InventoryItem
                {
                    Slot = (short)index, ItemID = isInArmoire ? itemId : 0, Quantity = isInArmoire ? 1u : 0u,
                    Flags = InventoryItem.ItemFlags.None
                };
                if (armoireItem.HashCode() != Armoire[index].HashCode())
                {
                    Armoire[index] = armoireItem;
                    //Push a custom inventory type
                    changeSet.Add(new BagChange(armoireItem, (InventoryType)2500));
                }

                index++;
            }
        }

        //I don't like this solution but unless I go and hook the glamour chest network request(which I may do) this will have to do
        private bool _glamourAgentActive;
        private readonly TimeSpan _glamourAgentWait = TimeSpan.FromMilliseconds(500);
        private DateTime? _glamourAgentOpened;
        public unsafe void ParseGlamourChest(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var agents = Framework.Instance()->GetUiModule()->GetAgentModule();
            var dresserAgent = agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
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
            
            InMemory.Add((InventoryType)2501);

            var itemsStart = *(IntPtr*)((IntPtr)dresserAgent + 40) + 40;
            if (itemsStart == IntPtr.Zero) return;
            for (var i = 0; i < 800; i++)
            {
                var glamItem = *(GlamourItem*)(itemsStart + i * 136);
                var flags = InventoryItem.ItemFlags.None;
                var itemId = glamItem.ItemId;
                var index = (short)glamItem.Index;
                if (itemId >= 1_000_000)
                {
                    itemId -= 1_000_000;
                    flags = InventoryItem.ItemFlags.HQ;
                }

                //Spiritbond becomes i because we need both indexes and this was the best way I could think of doing this.
                var glamourItem = new InventoryItem
                {
                    Slot = index, ItemID = itemId, Quantity = itemId != 0 ? 1u : 0u, Flags = flags,
                    Stain = glamItem.StainId, Spiritbond = (ushort)i
                };
                if (glamourItem.HashCode(false) != GlamourChest[i].HashCode(false))
                {
                    GlamourChest[i] = glamourItem;
                    //Push a custom inventory type
                    changeSet.Add(new BagChange(glamourItem, (InventoryType)2501));
                }
            }
        }

        public unsafe void ParseRetainerBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var currentRetainer = _characterMonitor.ActiveRetainer;
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
                        if (retainerItem.HashCode() != RetainerEquipped[currentRetainer][i].HashCode())
                        {
                            RetainerEquipped[currentRetainer][i] = retainerItem;
                            changeSet.Add(new BagChange(retainerItem, InventoryType.RetainerEquippedItems));
                        }
                    }


                    var retainerGilItem = retainerGil->Items[0];
                    retainerGilItem.Slot = 0;
                    if (retainerGilItem.HashCode() != RetainerGil[currentRetainer][0].HashCode())
                    {
                        RetainerGil[currentRetainer][0] = retainerGilItem;
                        changeSet.Add(new BagChange(retainerGilItem, InventoryType.RetainerGil));
                    }


                    for (var i = 0; i < retainerCrystal->Size; i++)
                    {
                        var retainerItem = retainerCrystal->Items[i];
                        retainerItem.Slot = (short)i;
                        if (retainerItem.HashCode() != RetainerCrystals[currentRetainer][i].HashCode())
                        {
                            RetainerCrystals[currentRetainer][i] = retainerItem;
                            changeSet.Add(new BagChange(retainerItem, InventoryType.RetainerCrystals));
                        }
                    }

                    var retainerMarketCopy = new InventoryItem[20];
                    for (var i = 0; i < retainerMarketItems->Size; i++)
                        retainerMarketCopy[i] = retainerMarketItems->Items[i];

                    retainerMarketCopy = retainerMarketCopy.ToList().SortByRetainerMarketOrder().ToArray();
                    for (var i = 0; i < retainerMarketCopy.Length; i++)
                    {
                        var retainerItem = retainerMarketCopy[i];
                        var cachedPrice = _cachedRetainerMarketPrices[retainerItem.Slot];
                        retainerItem.Slot = (short)i;
                        if (retainerItem.HashCode() != RetainerMarket[currentRetainer][i].HashCode() ||
                            cachedPrice != RetainerMarketPrices[currentRetainer][i])
                        {
                            RetainerMarket[currentRetainer][i] = retainerItem;
                            RetainerMarketPrices[currentRetainer][i] = cachedPrice;
                            changeSet.Add(new BagChange(retainerItem, InventoryType.RetainerMarket));
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
                            PluginLog.Verbose("bag was too big UwU retainer");
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
                            if (bag[newItem.Slot].HashCode() != newItem.HashCode())
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
                            if (bag[newBag.Slot].HashCode() != newBag.HashCode())
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
                            if (bag[newBag.Slot].HashCode() != newBag.HashCode())
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
                            if (bag[newBag.Slot].HashCode() != newBag.HashCode())
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
                            if (bag[newBag.Slot].HashCode() != newBag.HashCode())
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
                            if (bag[newBag.Slot].HashCode() != newBag.HashCode())
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
                            if (bag[newBag.Slot].HashCode() != newBag.HashCode())
                            {
                                bag[newBag.Slot] = newBag;
                                changeSet.Add(new BagChange(newBag, retainerBags[sortedBagIndex]));
                            }
                        }

                        absoluteIndex++;
                    }
            }
        }

        public unsafe void ParseGearSets(InventorySortOrder currentSortOrder, List<BagChange> changeSet)
        {
            var gearSetModule = RaptureGearsetModule.Instance();
            if (gearSetModule == null)
            {
                return;
            }
            for (byte i = 0; i < 100; i++)
            {
                if (!GearSets.ContainsKey(i)) GearSets.Add(i, new uint[13]);
                var gearSet = gearSetModule->Gearset[i];
                if (gearSet != null && gearSet->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
                {
                    GearSetsUsed[i] = true;
                    var gearSetName = MemoryHelper.ReadSeStringNullTerminated((IntPtr)gearSet->Name).ToString();
                    GearSetNames[i] = gearSetName;

                    var gearSetItems = new[]
                    {
                        gearSet->MainHand, gearSet->OffHand, gearSet->Head, gearSet->Body, gearSet->Hands,
                        gearSet->Legs, gearSet->Feet, gearSet->Ears, gearSet->Neck, gearSet->Wrists, gearSet->RingRight,
                        gearSet->RightLeft, gearSet->SoulStone
                    };
                    for (var index = 0; index < gearSetItems.Length; index++)
                    {
                        var gearSetItem = gearSetItems[index];
                        var itemId = gearSetItem.ItemID;
                        GearSets[i][index] = itemId;
                    }
                }
                else
                {
                    GearSetsUsed[i] = false;
                }
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
                _running = false;
                _containerInfoNetworkHook?.Dispose();
                _itemMarketBoardInfoHook?.Dispose();
                _characterMonitor.OnActiveRetainerChanged -= CharacterMonitorOnOnActiveRetainerChanged;
                _characterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
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
                PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
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