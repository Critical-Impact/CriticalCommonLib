using System;
using System.Collections.Generic;
using CriticalCommonLib.Models;
using FFXIVClientStructs.FFXIV.Client.Game;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Services
{
    public interface IInventoryScanner : IDisposable
    {
        void Enable();
        event InventoryScanner.BagsChangedDelegate? BagsChanged;
        event InventoryScanner.ContainerInfoReceivedDelegate? ContainerInfoReceived;
        void ParseBags();
        void LogBagChanges(List<BagChange> bagChanges);
        InventoryItem[] GetInventoryByType(ulong retainerId, InventoryType type);
        InventoryItem[] GetInventoryByType(InventoryType type);
        void ClearRetainerCache(ulong retainerId);
        void ClearCache();
        HashSet<InventoryType> InMemory { get; }
        Dictionary<ulong, HashSet<InventoryType>> InMemoryRetainers { get; }
        InventoryItem[] CharacterBag1 { get; }
        InventoryItem[] CharacterBag2 { get; }
        InventoryItem[] CharacterBag3 { get; }
        InventoryItem[] CharacterBag4 { get; }
        InventoryItem[] CharacterEquipped { get; }
        InventoryItem[] CharacterCrystals { get; }
        InventoryItem[] CharacterCurrency { get; }
        InventoryItem[] SaddleBag1 { get; }
        InventoryItem[] SaddleBag2 { get; }
        InventoryItem[] PremiumSaddleBag1 { get; }
        InventoryItem[] PremiumSaddleBag2 { get; }
        InventoryItem[] ArmouryMainHand { get; }
        InventoryItem[] ArmouryHead { get; }
        InventoryItem[] ArmouryBody { get; }
        InventoryItem[] ArmouryHands { get; }
        InventoryItem[] ArmouryLegs { get; }
        InventoryItem[] ArmouryFeet { get; }
        InventoryItem[] ArmouryOffHand { get; }
        InventoryItem[] ArmouryEars { get; }
        InventoryItem[] ArmouryNeck { get; }
        InventoryItem[] ArmouryWrists { get; }
        InventoryItem[] ArmouryRings { get; }
        InventoryItem[] ArmourySoulCrystals { get; }
        InventoryItem[] FreeCompanyBag1 { get; }
        InventoryItem[] FreeCompanyBag2 { get; }
        InventoryItem[] FreeCompanyBag3 { get; }
        InventoryItem[] FreeCompanyBag4 { get; }
        InventoryItem[] FreeCompanyBag5 { get; }
        InventoryItem[] FreeCompanyGil { get; }
        InventoryItem[] FreeCompanyCrystals { get; }
        InventoryItem[] Armoire { get; }
        InventoryItem[] GlamourChest { get; }
        Dictionary<ulong, InventoryItem[]> RetainerBag1 { get; }
        Dictionary<ulong, InventoryItem[]> RetainerBag2 { get; }
        Dictionary<ulong, InventoryItem[]> RetainerBag3 { get; }
        Dictionary<ulong, InventoryItem[]> RetainerBag4 { get; }
        Dictionary<ulong, InventoryItem[]> RetainerBag5 { get; }
        Dictionary<ulong, InventoryItem[]> RetainerEquipped { get; }
        Dictionary<ulong, InventoryItem[]> RetainerMarket { get; }
        Dictionary<ulong, InventoryItem[]> RetainerCrystals { get; }
        Dictionary<ulong, InventoryItem[]> RetainerGil { get; }
        Dictionary<ulong, uint[]> RetainerMarketPrices { get; }
        Dictionary<byte, uint[]> GearSets { get; }
        bool[] GearSetsUsed { get; }
        string[] GearSetNames { get; }
        HashSet<(byte, string)> GetGearSets(uint itemId);
        Dictionary<uint, HashSet<(byte, string)>> GetGearSets();
        unsafe void ParseCharacterBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseSaddleBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParsePremiumSaddleBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseArmouryChest(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseCharacterEquipped(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseFreeCompanyBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseArmoire(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseGlamourChest(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseRetainerBags(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
        unsafe void ParseGearSets(InventorySortOrder currentSortOrder, List<BagChange> changeSet);
    }
}