using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Data;
using Lumina;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services
{
    public static class ExcelCache
    {
        private static Dictionary<uint, Item> _itemCache = new();
        private static Dictionary<uint, EventItem> _eventItemCache = new();
        private static Dictionary<uint, ItemUICategory> _itemUiCategory = new();
        private static  Dictionary<uint, ItemSearchCategory> _itemSearchCategory = new();
        private static  Dictionary<uint, ItemSortCategory> _itemSortCategory = new();
        private static  Dictionary<uint, EquipSlotCategory> _equipSlotCategories = new();
        private static  Dictionary<uint, GatheringItem> _gatheringItems = new();
        private static  Dictionary<uint, GatheringItemPoint> _gatheringItemPoints = new();
        private static  Dictionary<uint, GatheringPoint> _gatheringPoints = new();
        private static  Dictionary<uint, GatheringPointTransient> _gatheringPointsTransients = new();
        private static  Dictionary<uint, EquipRaceCategory> _equipRaceCategories = new();
        private static  Dictionary<uint, Recipe> _recipeCache = new();
        private static  Dictionary<uint, uint> _gatheringItemPointLinks = new();
        private static  Dictionary<uint, uint> _gatheringItemsLinks = new();
        private static  Dictionary<uint, string> _addonNames = new();
        private static Dictionary<uint, HashSet<uint>> _recipeLookupTable = new();
        private static Dictionary<uint, HashSet<uint>> _craftLookupTable = new();
        private static Dictionary<uint, HashSet<uint>> _classJobCategoryLookup = new();
        private static Dictionary<uint, uint> _craftLevesItemLookup = new();
        private static Dictionary<uint, uint> _companyCraftSequenceByItemIdLookup = new();
        private static HashSet<uint> _gilShopBuyable = new(); 
        private static HashSet<uint> _armoireItems = new(); 
        private static  DataManager? _dataManager;
        private static GameData? _gameData;
        private static bool _itemUiCategoriesFullyLoaded ;
        private static bool _itemUiSearchFullyLoaded ;
        private static bool _sellableItemsCalculated ;
        private static bool _gatheringItemLinksCalculated ;
        private static bool _gatheringItemPointLinksCalculated ;
        private static bool _recipeLookUpCalculated ;
        private static bool _companyCraftSequenceCalculated ;
        private static bool _classJobCategoryLookupCalculated ;
        private static bool _craftLevesItemLookupCalculated ;
        private static bool _allItemsLoaded ;
        private static bool _armoireLoaded;
        private static bool _initialised = false;
        
        //Key is the class job category and the hashset contains a list of class jobs
        public static Dictionary<uint, HashSet<uint>> ClassJobCategoryLookup
        {
            get => _classJobCategoryLookup ?? new Dictionary<uint, HashSet<uint>>();
            set => _classJobCategoryLookup = value;
        }
        public static Dictionary<uint, ItemUICategory> ItemUiCategory
        {
            get => _itemUiCategory ?? new Dictionary<uint, ItemUICategory>();
            set => _itemUiCategory = value;
        }

        public static Dictionary<uint, ItemSearchCategory> SearchCategory
        {
            get => _itemSearchCategory?? new Dictionary<uint, ItemSearchCategory>();
            set => _itemSearchCategory = value;
        }

        public static Dictionary<uint, ItemSortCategory> SortCategory
        {
            get => _itemSortCategory ?? new Dictionary<uint, ItemSortCategory>();
            set => _itemSortCategory = value;
        }

        public static Dictionary<uint, EquipSlotCategory> EquipSlotCategories
        {
            get => _equipSlotCategories ?? new Dictionary<uint, EquipSlotCategory>();
            set => _equipSlotCategories = value;
        }

        public static Dictionary<uint, EventItem> EventItemCache
        {
            get => _eventItemCache ?? new Dictionary<uint, EventItem>();
            set => _eventItemCache = value;
        }

        public static Dictionary<uint, Recipe> RecipeCache
        {
            get => _recipeCache ?? new Dictionary<uint, Recipe>();
            set => _recipeCache = value;
        }

        public static Dictionary<uint, Item> ItemCache
        {
            get => _itemCache ?? new Dictionary<uint, Item>();
            set => _itemCache = value;
        }

        public static Dictionary<uint, EquipRaceCategory> EquipRaceCategories
        {
            get => _equipRaceCategories ?? new Dictionary<uint, EquipRaceCategory>();
            set => _equipRaceCategories = value;
        }

        public static HashSet<uint> GilShopBuyable
        {
            get => _gilShopBuyable;
            set => _gilShopBuyable = value;
        }

        public static Dictionary<uint, GatheringItem> GatheringItems
        {
            get => _gatheringItems;
            set => _gatheringItems = value;
        }

        public static Dictionary<uint, uint> GatheringItemsLinks
        {
            get => _gatheringItemsLinks;
            set => _gatheringItemsLinks = value;
        }

        public static Dictionary<uint, GatheringItemPoint> GatheringItemPoints
        {
            get => _gatheringItemPoints;
            set => _gatheringItemPoints = value;
        }

        public static Dictionary<uint, uint> GatheringItemPointLinks
        {
            get => _gatheringItemPointLinks;
            set => _gatheringItemPointLinks = value;
        }

        public static Dictionary<uint, GatheringPoint> GatheringPoints
        {
            get => _gatheringPoints;
            set => _gatheringPoints = value;
        }

        public static Dictionary<uint, GatheringPointTransient> GatheringPointsTransients
        {
            get => _gatheringPointsTransients;
            set => _gatheringPointsTransients = value;
        }

        //Lookup of each recipe available for each item
        public static Dictionary<uint, HashSet<uint>> RecipeLookupTable
        {
            get => _recipeLookupTable;
            set => _recipeLookupTable = value;
        }

        //Dictionary of every item that an item can craft
        public static Dictionary<uint, HashSet<uint>> CraftLookupTable
        {
            get => _craftLookupTable;
            set => _craftLookupTable = value;
        }

        public static Dictionary<uint, string> AddonNames
        {
            get => _addonNames;
            set => _addonNames = value;
        }

        public static Dictionary<uint, uint> CraftLevesItemLookup
        {
            get => _craftLevesItemLookup;
            set => _craftLevesItemLookup = value;
        }

        public static Dictionary<uint, uint> CompanyCraftSequenceByItemIdLookup
        {
            get => _companyCraftSequenceByItemIdLookup;
            set => _companyCraftSequenceByItemIdLookup = value;
        }

        public static string GetAddonName(uint addonId)
        {
            if (AddonNames.ContainsKey(addonId))
            {
                return AddonNames[addonId];
            }

            var addonSheet = GetSheet<Addon>();
            var addonRow = addonSheet.GetRow(addonId);
            if (addonRow != null)
            {
                AddonNames.Add(addonId, addonRow.Text);
                return addonRow.Text;
            }

            return "";
        }

        public static bool CanCraftItem(uint rowId)
        {
            if (!_recipeLookUpCalculated)
            {
                CalculateRecipeLookup();
            }

            return _recipeLookupTable.ContainsKey(rowId);
        }

        public static bool IsCraftItem(uint rowId)
        {
            if (!_recipeLookUpCalculated)
            {
                CalculateRecipeLookup();
            }

            return CraftLookupTable.ContainsKey(rowId) && CraftLookupTable[rowId].Count != 0;
        }

        public static bool IsArmoireItem(uint rowId)
        {
            if (!_armoireLoaded)
            {
                CalculateArmoireItems();
            }

            return _armoireItems.Contains(rowId);
        }

        public static void Initialise()
        {
            ItemCache = new();
            EventItemCache = new();
            EquipRaceCategories = new();
            EquipSlotCategories = new();
            SearchCategory = new();
            SortCategory = new();
            ItemUiCategory = new();
            GatheringItems = new();
            GatheringItemPoints = new();
            GilShopBuyable = new ();
            GatheringItemPointLinks = new ();
            GatheringItemsLinks = new ();
            GatheringPoints = new ();
            GatheringPointsTransients = new ();
            RecipeLookupTable = new ();
            RecipeCache = new ();
            ClassJobCategoryLookup = new();
            CraftLevesItemLookup = new();
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _sellableItemsCalculated = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _armoireLoaded = false;
            _dataManager = Service.Data;
            _initialised = true;
        }

        public static void Initialise(GameData gameData)
        {
            ItemCache = new();
            EventItemCache = new();
            EquipRaceCategories = new();
            EquipSlotCategories = new();
            SearchCategory = new();
            SortCategory = new();
            ItemUiCategory = new();
            GatheringItems = new();
            GilShopBuyable = new ();
            GatheringItemPoints = new ();
            GatheringItemPointLinks = new ();
            GatheringItemsLinks = new ();
            GatheringPoints = new ();
            GatheringPointsTransients = new ();
            RecipeCache = new ();
            ClassJobCategoryLookup = new ();
            CraftLevesItemLookup = new ();
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _sellableItemsCalculated = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _gameData = gameData;
            _initialised = true;
        }

        public static void Destroy()
        {
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _sellableItemsCalculated = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _initialised = false;
        }

        public static bool IsItemCraftLeve(uint itemId)
        {
            CalculateCraftLevesItemLookup();
            if (_craftLevesItemLookup.ContainsKey(itemId))
            {
                return true;
            }

            return false;
        }

        public static CraftLeve? GetCraftLevel(uint itemId)
        {
            CalculateCraftLevesItemLookup();
            if (_craftLevesItemLookup.ContainsKey(itemId))
            {
                return ExcelCache.GetSheet<CraftLeve>().GetRow(_craftLevesItemLookup[itemId]);
            }

            return null;
        }
        
        public static void CalculateCraftLevesItemLookup()
        {
            if (!_craftLevesItemLookupCalculated && _initialised)
            {
                _craftLevesItemLookupCalculated = true;
                foreach (var craftLeve in ExcelCache.GetSheet<CraftLeve>())
                {
                    foreach (var item in craftLeve.UnkData3)
                    {
                        if (!CraftLevesItemLookup.ContainsKey((uint)item.Item))
                        {
                            CraftLevesItemLookup.Add((uint)item.Item, craftLeve.RowId);
                        }
                    }
                }
            }
        }

        public static ExcelSheet< T > GetSheet< T >() where T : ExcelRow
        {
            if (_dataManager != null)
            {
                return _dataManager.Excel.GetSheet<T>()!;
            }
            else if(_gameData != null)
            {
                return _gameData.GetExcelSheet<T>()!;
            }

            throw new Exception("You must initialise the cache with a data manager instance or game data instance");
        }
        
        public static List<Item> GetItems()
        {
            List<Item> items = new List<Item>(); 
            if (!_allItemsLoaded)
            {
                _itemCache = ExcelCache.GetSheet<Item>().ToDictionary(c => c.RowId);
                _allItemsLoaded = true;
            }
            foreach (var lookup in _itemCache)
            {
                items.Add(lookup.Value);
            }
            return items;
        }

        public static EquipRaceCategory? GetEquipRaceCategory(uint equipRaceCategoryId)
        {
            if (!EquipRaceCategories.ContainsKey(equipRaceCategoryId))
            {
                var equipRaceCategory = ExcelCache.GetSheet<EquipRaceCategory>().GetRow(equipRaceCategoryId);
                if (equipRaceCategory == null)
                {
                    return null;
                }
                EquipRaceCategories[equipRaceCategoryId] = equipRaceCategory;
            }
            return EquipRaceCategories[equipRaceCategoryId];
        }

        public static TripleTriadCard? GetTripleTriadCard(uint cardId)
        {
            return GetSheet<TripleTriadCard>().GetRow(cardId);
        }
        
        public static Item? GetItem(uint itemId)
        {
            if (!ItemCache.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<Item>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                ItemCache[itemId] = item;
            }
            return ItemCache[itemId];
        }
        
        public static GatheringItem? GetGatheringItem(uint itemId)
        {
            if (!GatheringItems.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringItem>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                GatheringItems[itemId] = item;
            }
            return GatheringItems[itemId];
        }
        
        public static GatheringItemPoint? GetGatheringItemPoint(uint itemId)
        {
            if (!GatheringItemPoints.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringItemPoint>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                GatheringItemPoints[itemId] = item;
            }
            return GatheringItemPoints[itemId];
        }
        
        public static GatheringPointTransient? GetGatheringPointTransient(uint itemId)
        {
            if (!GatheringPointsTransients.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringPointTransient>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                GatheringPointsTransients[itemId] = item;
            }
            return GatheringPointsTransients[itemId];
        }
        
        public static GatheringPoint? GetGatheringPoint(uint itemId)
        {
            if (!GatheringPoints.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringPoint>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                GatheringPoints[itemId] = item;
            }
            return GatheringPoints[itemId];
        }
        
        public static GatheringItem? GetGatheringItemByItemId(uint itemId)
        {
            if (!_gatheringItemLinksCalculated)
            {
                CalculateGatheringItemLinks();
            }

            if (GatheringItemsLinks.ContainsKey(itemId))
            {
                return GetGatheringItem(GatheringItemsLinks[itemId]);
            }
            return null;
        }

        public static bool CanBeGathered(uint itemId)
        {
            if (!_gatheringItemLinksCalculated)
            {
                CalculateGatheringItemLinks();
            }

            return GatheringItemsLinks.ContainsKey(itemId);
        }
        
        public static EventItem? GetEventItem(uint itemId)
        {
            if (!EventItemCache.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<EventItem>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                EventItemCache[itemId] = item;
            }
            return EventItemCache[itemId];
        }
        
        public static Recipe? GetRecipe(uint recipeId)
        {
            if (!RecipeCache.ContainsKey(recipeId))
            {
                var recipe = ExcelCache.GetSheet<Recipe>().GetRow(recipeId);
                if (recipe == null)
                {
                    return null;
                }
                RecipeCache[recipeId] = recipe;
            }
            return RecipeCache[recipeId];
        }

        private static Dictionary<uint,uint> GetFlattenedItemRecipeLoop(Dictionary<uint,uint> itemIds, uint itemId, uint quantity)
        {
            var recipes = ExcelCache.GetItemRecipes(itemId);
            foreach (var recipe in recipes)
            {
                foreach (var ingredient in recipe.UnkData5)
                {
                    if (ingredient.ItemIngredient != 0)
                    {
                        if (!itemIds.ContainsKey((uint) ingredient.ItemIngredient))
                        {
                            itemIds.Add((uint) ingredient.ItemIngredient, 0);
                        }

                        itemIds[(uint) ingredient.ItemIngredient] += ingredient.AmountIngredient * quantity;
                        
                        if (ExcelCache.CanCraftItem((uint) ingredient.ItemIngredient))
                        {
                            GetFlattenedItemRecipeLoop(itemIds, (uint)ingredient.ItemIngredient, quantity);
                        }
                    }
                }
            }

            if (recipes.Count == 0)
            {
                if (!_companyCraftSequenceCalculated)
                {
                    CalculateCompanyCraftSequenceByItemId();
                }
                if (CompanyCraftSequenceByItemIdLookup.ContainsKey(itemId))
                {
                    //Might need to split into parts at some point
                    var companyCraftSequence = GetSheet<CompanyCraftSequence>()
                        .GetRow(CompanyCraftSequenceByItemIdLookup[itemId]);
                    if (companyCraftSequence != null)
                    {
                        foreach (var lazyPart in companyCraftSequence.CompanyCraftPart)
                        {
                            var part = lazyPart.Value;
                            if (part != null)
                            {
                                foreach (var lazyProcess in part.CompanyCraftProcess)
                                {
                                    var process = lazyProcess.Value;
                                    if (process != null)
                                    {
                                        foreach (var supplyItem in process.UnkData0)
                                        {
                                            var actualItem = GetSheet<CompanyCraftSupplyItem>()
                                                .GetRow(supplyItem.SupplyItem);
                                            if (actualItem != null)
                                            {
                                                if (actualItem.Item.Row != 0)
                                                {
                                                    if (!itemIds.ContainsKey(actualItem.Item.Row))
                                                    {
                                                        itemIds.Add(actualItem.Item.Row, 0);
                                                    }

                                                    itemIds[actualItem.Item.Row] += (uint) supplyItem.SetQuantity *
                                                        supplyItem.SetsRequired * quantity;
                                                    
                                                    GetFlattenedItemRecipeLoop(itemIds, actualItem.Item.Row, quantity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return itemIds;
        }

        private static Dictionary<uint, Dictionary<uint, uint>>
            flattenedRecipes = new Dictionary<uint, Dictionary<uint, uint>>(); 

        public static Dictionary<uint,uint> GetFlattenedItemRecipe(uint itemId, bool includeSelf = false, uint quantity = 1)
        {
            if (flattenedRecipes.ContainsKey(itemId))
            {
                if (includeSelf)
                {
                    var flattenedItemRecipe = flattenedRecipes[itemId].ToDictionary(c => c.Key, c => c.Value);
                    flattenedItemRecipe.Add(itemId, quantity);
                    return flattenedItemRecipe;
                }
                return flattenedRecipes[itemId];
            }

            var flattenedItemRecipeLoop = GetFlattenedItemRecipeLoop(new Dictionary<uint, uint>(), itemId, quantity);
            flattenedRecipes.Add(itemId, flattenedItemRecipeLoop);
            if (includeSelf)
            {
                var flattenedItemRecipe = flattenedRecipes[itemId].ToDictionary(c => c.Key, c => c.Value);
                flattenedItemRecipe.Add(itemId, 1);
            }
            return flattenedItemRecipeLoop;
        }

        public static bool IsCompanyCraft(uint itemId)
        {
            if (itemId == 0)
            {
                return false;
            }
            if (!_companyCraftSequenceCalculated)
            {
                CalculateCompanyCraftSequenceByItemId();
            }

            return CompanyCraftSequenceByItemIdLookup.ContainsKey(itemId);
        }

        public static CompanyCraftSequence? GetCompanyCraftSequenceByItemId(uint itemId)
        {
            if (itemId == 0)
            {
                return null;
            }
            if (!_companyCraftSequenceCalculated)
            {
                CalculateCompanyCraftSequenceByItemId();
            }

            if (CompanyCraftSequenceByItemIdLookup.ContainsKey(itemId))
            {
                return ExcelCache.GetSheet<CompanyCraftSequence>()
                    .GetRow(CompanyCraftSequenceByItemIdLookup[itemId]);
            }

            return null;
        }

        public static void CalculateCompanyCraftSequenceByItemId()
        {
            if (!_companyCraftSequenceCalculated && _initialised)
            {
                _companyCraftSequenceCalculated = true;
                foreach (var companyCraftSequence in ExcelCache.GetSheet<CompanyCraftSequence>())
                {
                    if(!CompanyCraftSequenceByItemIdLookup.ContainsKey(companyCraftSequence.ResultItem.Row))
                    {
                        CompanyCraftSequenceByItemIdLookup.Add(companyCraftSequence.ResultItem.Row, companyCraftSequence.RowId);
                    }
                }
            }
        }
        
        public static List<Recipe> GetItemRecipes(uint itemId)
        {
            if (itemId == 0)
            {
                return new List<Recipe>();
            }
            if (!_recipeLookUpCalculated)
            {
                CalculateRecipeLookup();
            }
            List<Recipe> recipes = new List<Recipe>(); 
            if (RecipeLookupTable.ContainsKey(itemId))
            {
                foreach (var lookup in RecipeLookupTable[itemId])
                {
                    var recipe = GetRecipe(lookup);
                    if (recipe != null)
                    {
                        recipes.Add(recipe);
                    }
                }
            }

            return recipes;
        }

        public static ItemUICategory? GetItemUICategory(uint itemId)
        {
            if (!ItemUiCategory.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<ItemUICategory>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                ItemUiCategory[itemId] = item;
            }
            return ItemUiCategory[itemId];
        }

        public static Dictionary<uint,ItemUICategory> GetAllItemUICategories()
        {
            if (!_itemUiCategoriesFullyLoaded)
            {
                _itemUiCategory = ExcelCache.GetSheet<ItemUICategory>().ToDictionary(c => c.RowId);
                _itemUiCategoriesFullyLoaded = true;
            }

            return _itemUiCategory;
        }

        public static Dictionary<uint,ItemSearchCategory> GetAllItemSearchCategories()
        {
            if (!_itemUiSearchFullyLoaded)
            {
                _itemSearchCategory = ExcelCache.GetSheet<ItemSearchCategory>().ToDictionary(c => c.RowId);
                _itemUiSearchFullyLoaded = true;
            }

            return _itemSearchCategory;
        }

        public static  ItemSearchCategory? GetItemSearchCategory(uint itemId)
        {
            if (!SearchCategory.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<ItemSearchCategory>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                SearchCategory[itemId] = item;
            }
            return SearchCategory[itemId];
        }

        public static ItemSortCategory? GetItemSortCategory(uint itemId)
        {
            if (!SortCategory.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<ItemSortCategory>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                SortCategory[itemId] = item;
            }
            return SortCategory[itemId];
        }

        public static EquipSlotCategory? GetEquipSlotCategory(uint itemId)
        {
            if (!EquipSlotCategories.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<EquipSlotCategory>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }
                EquipSlotCategories[itemId] = item;
            }
            return EquipSlotCategories[itemId];
        }

        public static void CalculateGilShopItems()
        {
            if (!_sellableItemsCalculated && _initialised)
            {
                _sellableItemsCalculated = true;
                foreach (var gilShopItem in ExcelCache.GetSheet<GilShopItem>())
                {
                    if(!GilShopBuyable.Contains(gilShopItem.Item.Row))
                    {
                        GilShopBuyable.Add(gilShopItem.Item.Row);
                    }
                }
            }
        }

        public static void CalculateRecipeLookup()
        {
            if (!_recipeLookUpCalculated && _initialised)
            {
                _recipeLookUpCalculated = true;
                foreach (var recipe in ExcelCache.GetSheet<Recipe>())
                {
                    if(!RecipeLookupTable.ContainsKey(recipe.ItemResult.Row))
                    {
                        RecipeLookupTable.Add(recipe.ItemResult.Row, new HashSet<uint>());
                    }

                    RecipeLookupTable[recipe.ItemResult.Row].Add(recipe.RowId);
                    foreach (var item in recipe.UnkData5)
                    {
                        if (!CraftLookupTable.ContainsKey((uint) item.ItemIngredient))
                        {
                            CraftLookupTable.Add((uint)item.ItemIngredient, new HashSet<uint>());
                        }

                        var hashSet = CraftLookupTable[(uint) item.ItemIngredient];
                        if (!hashSet.Contains(recipe.ItemResult.Row))
                        {
                            hashSet.Add(recipe.ItemResult.Row);
                        }
                    }
                }
            }
        }

        public static void CalculateArmoireItems()
        {
            if (!_armoireLoaded && _initialised)
            {
                _armoireLoaded = true;
                foreach (var armoireItem in ExcelCache.GetSheet<Cabinet>())
                {
                    if(!_armoireItems.Contains(armoireItem.Item.Row))
                    {
                        _armoireItems.Add(armoireItem.Item.Row);
                    }
                }
            }
        }

        public static void CalculateGatheringItemPointLinks()
        {
            if (!_gatheringItemPointLinksCalculated && _initialised)
            {
                _gatheringItemPointLinksCalculated = true;
                foreach (var gatheringItemPoint in ExcelCache.GetSheet<GatheringItemPoint>())
                {
                    if(!GatheringItemPointLinks.ContainsKey(gatheringItemPoint.RowId))
                    {
                        GatheringItemPointLinks.Add(gatheringItemPoint.RowId, gatheringItemPoint.GatheringPoint.Row);
                    }
                }
            }
        }

        public static void CalculateGatheringItemLinks()
        {
            if (!_gatheringItemLinksCalculated && _initialised)
            {
                _gatheringItemLinksCalculated = true;
                foreach (var gatheringItem in ExcelCache.GetSheet<GatheringItem>())
                {
                    if(!GatheringItemsLinks.ContainsKey((uint)gatheringItem.Item))
                    {
                        GatheringItemsLinks.Add((uint)gatheringItem.Item, gatheringItem.RowId);
                    }
                }
            }
        }

        public static bool IsItemEquippableBy(uint classJobCategory, uint classJobId)
        {
            CalculateClassJobCategoryLookup();
            if (!_classJobCategoryLookup.ContainsKey(classJobCategory))
            {
                return false;
            }

            if (!_classJobCategoryLookup[classJobCategory].Contains(classJobId))
            {
                return false;
            }

            return true;
        }

        public static void CalculateClassJobCategoryLookup()
        {
            if (!_classJobCategoryLookupCalculated && _initialised)
            {
                var classJobMap = new Dictionary<string, uint>();
                foreach (var classJob in ExcelCache.GetSheet<ClassJob>())
                {
                    if (!classJobMap.ContainsKey(classJob.Abbreviation))
                    {
                        classJobMap[classJob.Abbreviation] = classJob.RowId;
                    }
                }
                _classJobCategoryLookupCalculated = true;
                var classJobCategoryMap = new Dictionary<uint, HashSet<uint>>();
                var propertyInfos = typeof(ClassJobCategory).GetProperties().Where(c => c.PropertyType == typeof(bool)).ToList();
                
                foreach (var classJobCategory in ExcelCache.GetSheet<ClassJobCategory>())
                {
                    //Dont hate me
                    var map = new HashSet<uint>();
                    foreach (PropertyInfo prop in propertyInfos)
                    {
                        var parsed = prop.GetValue(classJobCategory, null);
                        if (parsed is bool b && (bool?) b == true)
                        {
                            if (classJobMap.ContainsKey(prop.Name))
                            {
                                var classJobRowId = classJobMap[prop.Name];
                                if (!map.Contains(classJobRowId))
                                {
                                    map.Add(classJobRowId);
                                }
                            }
                        }
                    }

                    classJobCategoryMap[classJobCategory.RowId] = map;
                }

                _classJobCategoryLookup = classJobCategoryMap;
            }
        }

        public static bool IsItemAvailableAtTimedNode(uint itemId)
        {
            if (!_initialised)
            {
                return false;
            }

            if (!_gatheringItemLinksCalculated)
            {
                CalculateGatheringItemLinks();
            }

            if (!_gatheringItemPointLinksCalculated)
            {
                CalculateGatheringItemPointLinks();
            }

            if (GatheringItemsLinks.ContainsKey(itemId))
            {
                var gatheringItemId = GatheringItemsLinks[itemId];
                if (GatheringItemPointLinks.ContainsKey(gatheringItemId))
                {
                    var gatheringPointId = GatheringItemPointLinks[gatheringItemId];
                    var gatheringPointTransient = GetGatheringPointTransient(gatheringPointId);
                    if (gatheringPointTransient != null)
                    {
                        return gatheringPointTransient.GatheringRarePopTimeTable.Row != 0;
                    }
                }
            }

            return false;
        }

        public static bool IsItemGilShopBuyable(uint itemId)
        {
            if (!_initialised)
            {
                return false;
            }
            if (!_sellableItemsCalculated)
            {
                CalculateGilShopItems();
            }
            return GilShopBuyable.Contains(itemId);
        }
    }
}