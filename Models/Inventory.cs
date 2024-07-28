using System;
using System.Collections.Generic;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Models;

public class Inventory
{
    private ulong _characterId;
    private CharacterType _ownerType;
    
    //Character
    public InventoryItem?[]? CharacterBag1 { get; private set;}
    public InventoryItem?[]? CharacterBag2 { get; private set;}
    public InventoryItem?[]? CharacterBag3 { get; private set;}
    public InventoryItem?[]? CharacterBag4 { get; private set;}
    public InventoryItem?[]? CharacterEquipped { get; private set;}
    public InventoryItem?[]? CharacterCrystals { get; private set;}
    public InventoryItem?[]? CharacterCurrency { get; private set;}
    public InventoryItem?[]? SaddleBag1 { get; private set;}
    public InventoryItem?[]? SaddleBag2 { get; private set;}
    public InventoryItem?[]? PremiumSaddleBag1 { get; private set;}
    public InventoryItem?[]? PremiumSaddleBag2 { get; private set;}
    public InventoryItem?[]? ArmouryMainHand { get; private set;}
    public InventoryItem?[]? ArmouryHead { get; private set;}
    public InventoryItem?[]? ArmouryBody { get; private set;}
    public InventoryItem?[]? ArmouryHands { get; private set;}
    public InventoryItem?[]? ArmouryLegs { get; private set;}
    public InventoryItem?[]? ArmouryFeet { get; private set;}
    public InventoryItem?[]? ArmouryOffHand { get; private set;}
    public InventoryItem?[]? ArmouryEars { get; private set;}
    public InventoryItem?[]? ArmouryNeck { get; private set;}
    public InventoryItem?[]? ArmouryWrists { get; private set;}
    public InventoryItem?[]? ArmouryRings { get; private set;}
    public InventoryItem?[]? ArmourySoulCrystals { get; private set;}
    public InventoryItem?[]? Armoire { get; private set;}
    public InventoryItem?[]? GlamourChest { get; private set;}
    
    //Free Company
    public InventoryItem?[]? FreeCompanyBag1 { get; private set;}
    public InventoryItem?[]? FreeCompanyBag2 { get; private set;}
    public InventoryItem?[]? FreeCompanyBag3 { get; private set;}
    public InventoryItem?[]? FreeCompanyBag4 { get; private set;}
    public InventoryItem?[]? FreeCompanyBag5 { get; private set;}
    public InventoryItem?[]? FreeCompanyGil { get; private set;}
    public InventoryItem?[]? FreeCompanyCurrency { get; private set;}
    public InventoryItem?[]? FreeCompanyCrystals { get; private set;}
    
    //Retainers
    public InventoryItem?[]? RetainerBag1 { get; private set;}
    public InventoryItem?[]? RetainerBag2 { get; private set;}
    public InventoryItem?[]? RetainerBag3 { get; private set;}
    public InventoryItem?[]? RetainerBag4 { get; private set;}
    public InventoryItem?[]? RetainerBag5 { get; private set;}
    public InventoryItem?[]? RetainerEquipped { get; private set;}
    public InventoryItem?[]? RetainerMarket { get; private set;}
    public InventoryItem?[]? RetainerCrystals { get; private set;}
    public InventoryItem?[]? RetainerGil { get; private set;}
    
    //Housing
    public InventoryItem?[]? HousingInteriorStoreroom1 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom2 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom3 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom4 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom5 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom6 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom7 { get; private set;}
    public InventoryItem?[]? HousingInteriorStoreroom8 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems1 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems2 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems3 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems4 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems5 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems6 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems7 { get; private set;}
    public InventoryItem?[]? HousingInteriorPlacedItems8 { get; private set;}
    public InventoryItem?[]? HousingExteriorAppearance { get; private set;}
    public InventoryItem?[]? HousingExteriorPlacedItems { get; private set;}
    public InventoryItem?[]? HousingExteriorStoreroom { get; private set;}
    public InventoryItem?[]? HousingInteriorAppearance { get; private set;}

    public ulong CharacterId => _characterId;

    public Inventory(CharacterType ownerType, ulong characterId)
    {
        _ownerType = ownerType;
        _characterId = characterId;
        SetupInventories();
    }

    /// <summary>
    /// Load a set of items into the inventory, whether it be from a CSV or from memory.
    /// </summary>
    /// <param name="items">The items to load in</param>
    /// <param name="sortedType">Which </param>
    /// <param name="sortedCategory"></param>
    /// <param name="initialLoad"></param>
    /// <param name="inventoryChanges"></param>
    /// <param name="postConvertHook">Allows for a item to be modified after it has been converted, but before it's added to the inventory</param>
    /// <returns></returns>
    public List<InventoryChange> LoadGameItems(FFXIVClientStructs.FFXIV.Client.Game.InventoryItem[] items, InventoryType sortedType, InventoryCategory sortedCategory, bool initialLoad = false, List<InventoryChange>? inventoryChanges = null, Action<InventoryItem, int>? postConvertHook = null)
    {
        var inventory = GetInventoryByType(sortedType);
        if (inventory == null)
        {
            Service.Log.Error("Failed to find somewhere to put the items in the bag " + sortedType.ToString() + " for character " + CharacterId);
            return new List<InventoryChange>();
        }

        if (inventoryChanges == null)
        {
            inventoryChanges = new List<InventoryChange>();
        }

        for (var index = 0; index < items.Length; index++)
        {
            var newItem = InventoryItem.FromMemoryInventoryItem(items[index]);
            newItem.SortedContainer = sortedType;
            newItem.SortedCategory = sortedCategory;
            newItem.RetainerId = CharacterId;
            newItem.SortedSlotIndex = index;
            postConvertHook?.Invoke(newItem, index);
            if (inventory[newItem.SortedSlotIndex] == null)
            {
                inventory[newItem.SortedSlotIndex] = newItem;
                inventoryChanges.Add(new InventoryChange(null, newItem, sortedType, initialLoad));
            }
            else
            {
                var existingItem = inventory[newItem.SortedSlotIndex];
                if (existingItem != null && !existingItem.IsSame(newItem))
                {
                    inventory[newItem.SortedSlotIndex] = newItem;
                    inventoryChanges.Add(new InventoryChange(existingItem, newItem, sortedType, initialLoad));
                }
            }
        }

        return inventoryChanges;
    }

    /// <summary>
    /// Load a set of items into the inventory, whether it be from a CSV or from memory.
    /// </summary>
    /// <param name="items">The items to load in</param>
    /// <param name="sortedType">Which </param>
    /// <param name="sortedCategory"></param>
    /// <param name="initialLoad"></param>
    /// <returns></returns>
    public List<InventoryChange>? LoadItems(InventoryItem[] items, InventoryType sortedType, InventoryCategory sortedCategory, bool initialLoad = false)
    {
        var inventory = GetInventoryByType(sortedType);
        if (inventory == null)
        {
            Service.Log.Error("Failed to find somewhere to put the items in the bag " + sortedType.ToString() + " for character " + CharacterId);
            return null;
        }

        List<InventoryChange> inventoryChanges = new List<InventoryChange>();
        
        for (var index = 0; index < items.Length; index++)
        {
            var newItem = items[index];
            newItem.SortedContainer = sortedType;
            newItem.SortedCategory = sortedCategory;
            newItem.RetainerId = CharacterId;
            newItem.SortedSlotIndex = index;
            if (inventory[newItem.SortedSlotIndex] == null)
            {
                inventory[newItem.SortedSlotIndex] = newItem;
                inventoryChanges.Add(new InventoryChange(null, newItem, sortedType, initialLoad));
            }
            else
            {
                var existingItem = inventory[newItem.SortedSlotIndex];
                if (existingItem != null && !existingItem.IsSame(newItem))
                {
                    inventoryChanges.Add(new InventoryChange(existingItem, newItem, sortedType, initialLoad));
                }
            }
        }

        return inventoryChanges;
    }

    /// <summary>
    /// Helper method to add a single item to an inventory, with automatic detection of inventory type and category.
    /// </summary>
    /// <param name="item"></param>
    public void AddItem(InventoryItem item)
    {
        var sortedType = item.SortedContainer;
        var sortedCategory = item.SortedCategory;
        var inventory = GetInventoryByType(sortedType);
        if (inventory == null)
        {
            Service.Log.Error("Failed to find somewhere to put the items in the bag " + sortedType.ToString() + " for character " + CharacterId);
            return;
        }

        List<InventoryChange> inventoryChanges = new List<InventoryChange>();
        
        item.SortedContainer = sortedType;
        item.SortedCategory = sortedCategory;
        item.RetainerId = CharacterId;
        inventory[item.SortedSlotIndex] = item;
    }

    /// <summary>
    /// Load a set of items into the inventory from an external source, these are assumed to have the correct container and category.
    /// </summary>
    /// <param name="items">The items to load in</param>
    /// <param name="initialLoad"></param>
    /// <returns></returns>
    public List<InventoryChange>? LoadItems(InventoryItem[] items, bool initialLoad = false)
    {

        List<InventoryChange> inventoryChanges = new List<InventoryChange>();
        
        for (var index = 0; index < items.Length; index++)
        {
            var newItem = items[index];
            var inventory = GetInventoryByType(newItem.SortedContainer);
            if (inventory == null)
            {
                Service.Log.Error("Failed to find somewhere to put the items in the bag " + newItem.SortedContainer + " for character " + CharacterId);
                return null;
            }

            if (newItem.SortedSlotIndex >= 0 && newItem.SortedSlotIndex < inventory.Length)
            {
                if (inventory[newItem.SortedSlotIndex] == null)
                {
                    inventory[newItem.SortedSlotIndex] = newItem;
                    inventoryChanges.Add(new InventoryChange(null, newItem, newItem.SortedContainer, initialLoad));
                }
                else
                {
                    var existingItem = inventory[newItem.SortedSlotIndex];
                    if (existingItem != null && !existingItem.IsSame(newItem))
                    {
                        inventoryChanges.Add(new InventoryChange(existingItem, newItem, newItem.SortedContainer,
                            initialLoad));
                    }
                }
            }
        }

        return inventoryChanges;
    }
    
    public InventoryItem?[]? GetInventoryByType(InventoryType type)
    {
        switch (type)
        {
            case InventoryType.Bag0:
                return CharacterBag1;
            case InventoryType.Bag1:
                return CharacterBag2;
            case InventoryType.Bag2:
                return CharacterBag3;
            case InventoryType.Bag3:
                return CharacterBag4;
            case InventoryType.GearSet0:
                return CharacterEquipped;
            case InventoryType.Crystal:
                return CharacterCrystals;
            case InventoryType.Currency:
                return CharacterCurrency;
            case InventoryType.SaddleBag0:
                return SaddleBag1;
            case InventoryType.SaddleBag1:
                return SaddleBag2;
            case InventoryType.PremiumSaddleBag0:
                return PremiumSaddleBag1;
            case InventoryType.PremiumSaddleBag1:
                return PremiumSaddleBag2;
            case InventoryType.ArmoryMain:
                return ArmouryMainHand;
            case InventoryType.ArmoryHead:
                return ArmouryHead;
            case InventoryType.ArmoryBody:
                return ArmouryBody;
            case InventoryType.ArmoryHand:
                return ArmouryHands;
            case InventoryType.ArmoryLegs:
                return ArmouryLegs;
            case InventoryType.ArmoryFeet:
                return ArmouryFeet;
            case InventoryType.ArmoryOff:
                return ArmouryOffHand;
            case InventoryType.ArmoryEar:
                return ArmouryEars;
            case InventoryType.ArmoryNeck:
                return ArmouryNeck;
            case InventoryType.ArmoryWrist:
                return ArmouryWrists;
            case InventoryType.ArmoryRing:
                return ArmouryRings;
            case InventoryType.ArmorySoulCrystal:
                return ArmourySoulCrystals;
            case InventoryType.FreeCompanyBag0:
                return FreeCompanyBag1;
            case InventoryType.FreeCompanyBag1:
                return FreeCompanyBag2;
            case InventoryType.FreeCompanyBag2:
                return FreeCompanyBag3;
            case InventoryType.FreeCompanyBag3:
                return FreeCompanyBag4;
            case InventoryType.FreeCompanyBag4:
                return FreeCompanyBag5;
            case InventoryType.FreeCompanyGil:
                return FreeCompanyGil;
            case InventoryType.FreeCompanyCurrency:
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
            case InventoryType.FreeCompanyCrystal:
                return FreeCompanyCrystals;
            case InventoryType.Armoire:
                return Armoire;
            case InventoryType.GlamourChest:
                return GlamourChest;
            case InventoryType.RetainerBag0:
                return RetainerBag1;
            case InventoryType.RetainerBag1:
                return RetainerBag2;
            case InventoryType.RetainerBag2:
                return RetainerBag3;
            case InventoryType.RetainerBag3:
                return RetainerBag4;
            case InventoryType.RetainerBag4:
                return RetainerBag5;
            case InventoryType.RetainerCrystal:
                return RetainerCrystals;
            case InventoryType.RetainerGil:
                return RetainerGil;
            case InventoryType.RetainerMarket:
                return RetainerMarket;
            case InventoryType.RetainerEquippedGear:
                return RetainerEquipped;
            //Not used
            case InventoryType.UNKNOWN_2008:
            case InventoryType.FreeCompanyBag5:
            case InventoryType.FreeCompanyBag6:
            case InventoryType.FreeCompanyBag7:
            case InventoryType.FreeCompanyBag8:
            case InventoryType.FreeCompanyBag9:
            case InventoryType.FreeCompanyBag10:
            case InventoryType.GearSet1:
            case InventoryType.Mail:
            case InventoryType.KeyItem:
            case InventoryType.HandIn:
            case InventoryType.DamagedGear:
            case InventoryType.ArmoryWaist:
            case InventoryType.Examine:
            case InventoryType.RetainerBag5:
            case InventoryType.RetainerBag6:
                return null;
        }
        Service.Log.Error("InventoryType " + type + " has no mapped inventory field.");
        return null;
    }

    public List<InventoryItem> GetItemsByCategory(InventoryCategory category)
    {
        var mergedItems = new List<InventoryItem>();
        var types = category.GetTypes();
        foreach (var type in types)
        {
            var items = GetInventoryByType(type);
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        mergedItems.Add(item);
                    }
                }
            }
        }

        return mergedItems;
    }

    public List<InventoryItem> GetItemsByType(InventoryType type)
    {
        var mergedItems = new List<InventoryItem>();
        
        var items = GetInventoryByType(type);
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    mergedItems.Add(item);
                }
            }
        }

        return mergedItems;
    }

    public List<InventoryItem?[]> GetAllInventories()
    {
        List<InventoryItem?[]> inventories = new List<InventoryItem?[]>();
        var possibleValues = Enum.GetValues<InventoryType>();
        foreach (var possibleValue in possibleValues)
        {
            var bag = GetInventoryByType(possibleValue);
            if (bag != null)
            {
                inventories.Add(bag);
            }
        }

        return inventories;
    }

    public Dictionary<InventoryType,InventoryItem?[]> GetAllInventoriesByType()
    {
        Dictionary<InventoryType,InventoryItem?[]> inventories = new Dictionary<InventoryType,InventoryItem?[]>();
        var possibleValues = Enum.GetValues<InventoryType>();
        foreach (var possibleValue in possibleValues)
        {
            var bag = GetInventoryByType(possibleValue);
            if (bag != null)
            {
                inventories.Add(possibleValue, bag);
            }
        }

        return inventories;
    }

    public Dictionary<InventoryCategory,List<InventoryItem>> GetAllInventoriesByCategory()
    {
        Dictionary<InventoryCategory,List<InventoryItem>> inventories = new Dictionary<InventoryCategory,List<InventoryItem>>();
        var categories = Enum.GetValues<InventoryCategory>();
        foreach (var category in categories)
        {
            var types = category.GetTypes();
            foreach (var type in types)
            {
                var bag = GetInventoryByType(type);
                if (bag != null)
                {
                    inventories.TryAdd(category, new List<InventoryItem>());
                    foreach (var item in bag)
                    {
                        if (item != null)
                        {
                            inventories[category].Add(item);
                        }
                    }
                }
            }
        }

        return inventories;
    }

    public void FillSlots()
    {
        var possibleValues = Enum.GetValues<InventoryType>();
        foreach (var possibleValue in possibleValues)
        {
            var bag = GetInventoryByType(possibleValue);
            if (bag != null)
            {
                for (var index = 0; index < bag.Length; index++)
                {
                    var item = bag[index];
                    if (item == null)
                    {
                        var inventoryItem = new InventoryItem();
                        inventoryItem.SortedSlotIndex = index;
                        inventoryItem.Container = possibleValue;
                        inventoryItem.SortedCategory = possibleValue.ToInventoryCategory();
                        inventoryItem.SortedContainer = inventoryItem.Container;
                        inventoryItem.Condition = 0;
                        bag[index] = inventoryItem;
                        
                    }
                }
            }
        }
    }

    public void ClearInventories()
    {
        SetupInventories();
    }
    private void SetupInventories()
    {
        if (_ownerType == CharacterType.Character)
        {
            CharacterBag1 = new InventoryItem[35];
            CharacterBag2 = new InventoryItem[35];
            CharacterBag3 = new InventoryItem[35];
            CharacterBag4 = new InventoryItem[35];
            CharacterEquipped = new InventoryItem[14];
            CharacterCrystals = new InventoryItem[18];
            CharacterCurrency = new InventoryItem[100];
            SaddleBag1 = new InventoryItem[35];
            SaddleBag2 = new InventoryItem[35];
            PremiumSaddleBag1 = new InventoryItem[35];
            PremiumSaddleBag2 = new InventoryItem[35];
            ArmouryMainHand = new InventoryItem[50];
            ArmouryHead = new InventoryItem[35];
            ArmouryBody = new InventoryItem[35];
            ArmouryHands = new InventoryItem[35];
            ArmouryLegs = new InventoryItem[35];
            ArmouryFeet = new InventoryItem[35];
            ArmouryOffHand = new InventoryItem[35];
            ArmouryEars = new InventoryItem[35];
            ArmouryNeck = new InventoryItem[35];
            ArmouryWrists = new InventoryItem[35];
            ArmouryRings = new InventoryItem[50];
            ArmourySoulCrystals = new InventoryItem[25];
            Armoire = new InventoryItem[Service.ExcelCache.CabinetSize];
            GlamourChest = new InventoryItem[Service.ExcelCache.GlamourChestSize];
        }
        else if (_ownerType == CharacterType.Housing)
        {
            HousingInteriorStoreroom1 = new InventoryItem[50];
            HousingInteriorStoreroom2 = new InventoryItem[50];
            HousingInteriorStoreroom3 = new InventoryItem[50];
            HousingInteriorStoreroom4 = new InventoryItem[50];
            HousingInteriorStoreroom5 = new InventoryItem[50];
            HousingInteriorStoreroom6 = new InventoryItem[50];
            HousingInteriorStoreroom7 = new InventoryItem[50];
            HousingInteriorStoreroom8 = new InventoryItem[50];
            HousingInteriorPlacedItems1 = new InventoryItem[50];
            HousingInteriorPlacedItems2 = new InventoryItem[50];
            HousingInteriorPlacedItems3 = new InventoryItem[50];
            HousingInteriorPlacedItems4 = new InventoryItem[50];
            HousingInteriorPlacedItems5 = new InventoryItem[50];
            HousingInteriorPlacedItems6 = new InventoryItem[50];
            HousingInteriorPlacedItems7 = new InventoryItem[50];
            HousingInteriorPlacedItems8 = new InventoryItem[50];
            HousingExteriorAppearance = new InventoryItem[9];
            HousingExteriorPlacedItems = new InventoryItem[40];
            HousingExteriorStoreroom = new InventoryItem[40];
            HousingInteriorAppearance = new InventoryItem[10];
        }
        else if (_ownerType == CharacterType.Retainer)
        {
            RetainerBag1 = new InventoryItem[35];
            RetainerBag2 = new InventoryItem[35];
            RetainerBag3 = new InventoryItem[35];
            RetainerBag4 = new InventoryItem[35];
            RetainerBag5 = new InventoryItem[35];
            RetainerEquipped = new InventoryItem[14];
            RetainerMarket = new InventoryItem[20];
            RetainerGil = new InventoryItem[1];
            RetainerCrystals = new InventoryItem[18];
        }
        else if (_ownerType == CharacterType.FreeCompanyChest)
        {
            FreeCompanyBag1 = new InventoryItem[50];
            FreeCompanyBag2 = new InventoryItem[50];
            FreeCompanyBag3 = new InventoryItem[50];
            FreeCompanyBag4 = new InventoryItem[50];
            FreeCompanyBag5 = new InventoryItem[50];
            FreeCompanyGil = new InventoryItem[11];
            FreeCompanyCurrency = new InventoryItem[1];
            FreeCompanyCrystals = new InventoryItem[18];
        }
    }
}