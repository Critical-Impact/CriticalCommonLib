using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Data;
using Dalamud.Plugin;
using Lumina;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services
{
    public static class ExcelCache
    {
        private static Dictionary<uint, Item> _itemCache;
        private static Dictionary<uint, EventItem> _eventItemCache;
        private static Dictionary<uint, ItemUICategory> _itemUiCategory;
        private static  Dictionary<uint, ItemSearchCategory> _itemSearchCategory;
        private static  Dictionary<uint, ItemSortCategory> _itemSortCategory;
        private static  Dictionary<uint, EquipSlotCategory> _equipSlotCategories;
        private static HashSet<uint> _gilShopBuyable; 
        private static  DataManager _dataManager;
        private static GameData _gameData;
        private static bool _itemUiCategoriesFullyLoaded ;
        private static bool _itemUiSearchFullyLoaded ;
        private static bool _sellableItemsCalculated ;
        private static bool _initialised = false;

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

        public static Dictionary<uint, Item> ItemCache
        {
            get => _itemCache ?? new Dictionary<uint, Item>();
            set => _itemCache = value;
        }

        public static HashSet<uint> GilShopBuyable
        {
            get => _gilShopBuyable;
            set => _gilShopBuyable = value;
        }

        public static void Initialise(DataManager dataManager)
        {
            ItemCache = new();
            EventItemCache = new();
            EquipSlotCategories = new();
            SearchCategory = new();
            SortCategory = new();
            ItemUiCategory = new();
            GilShopBuyable = new ();
            _itemUiCategoriesFullyLoaded = false;
            _itemUiSearchFullyLoaded = false;
            _sellableItemsCalculated = false;
            _dataManager = dataManager;
            _initialised = true;
        }

        public static void Initialise(GameData gameData)
        {
            ItemCache = new();
            EventItemCache = new();
            EquipSlotCategories = new();
            SearchCategory = new();
            SortCategory = new();
            ItemUiCategory = new();
            GilShopBuyable = new ();
            _itemUiCategoriesFullyLoaded = false;
            _itemUiSearchFullyLoaded = false;
            _sellableItemsCalculated = false;
            _gameData = gameData;
            _initialised = true;
        }

        public static void Destroy()
        {
            _itemUiCategoriesFullyLoaded = false;
            _itemUiSearchFullyLoaded = false;
            _sellableItemsCalculated = false;
            _initialised = false;
        }

        public static ExcelSheet< T > GetSheet< T >() where T : ExcelRow
        {
            if (_dataManager != null)
            {
                return _dataManager.Excel.GetSheet<T>();
            }
            else
            {
                return _gameData.GetExcelSheet<T>();
            }
        }
        
        public static Item GetItem(uint itemId)
        {
            if (!ItemCache.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<Item>().GetRow(itemId);
                ItemCache[itemId] = item;
            }
            return ItemCache[itemId];
        }
        
        public static EventItem GetEventItem(uint itemId)
        {
            if (!EventItemCache.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<EventItem>().GetRow(itemId);
                EventItemCache[itemId] = item;
            }
            return EventItemCache[itemId];
        }

        public static  ItemUICategory GetItemUICategory(uint itemId)
        {
            if (!ItemUiCategory.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<ItemUICategory>().GetRow(itemId);
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

        public static  ItemSearchCategory GetItemSearchCategory(uint itemId)
        {
            if (!SearchCategory.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<ItemSearchCategory>().GetRow(itemId);
                SearchCategory[itemId] = item;
            }
            return SearchCategory[itemId];
        }

        public static  ItemSortCategory GetItemSortCategory(uint itemId)
        {
            if (!SortCategory.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<ItemSortCategory>().GetRow(itemId);
                SortCategory[itemId] = item;
            }
            return SortCategory[itemId];
        }

        public static  EquipSlotCategory GetEquipSlotCategory(uint itemId)
        {
            if (!EquipSlotCategories.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<EquipSlotCategory>().GetRow(itemId);
                EquipSlotCategories[itemId] = item;
            }
            return EquipSlotCategories[itemId];
        }

        public static void CalculateGilShopItems()
        {
            if (!_sellableItemsCalculated)
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