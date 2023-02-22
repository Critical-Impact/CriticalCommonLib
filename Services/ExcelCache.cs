using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Collections;
using CriticalCommonLib.Extensions;
using Dalamud.Data;
using Lumina;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using CriticalCommonLib.Sheets;
using Dalamud.Logging;
using Lumina.Data;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache : IDisposable
    {
        private readonly DataManager? _dataManager;
        private readonly GameData? _gameData;
        
        private Dictionary<uint, EventItem> _eventItemCache;
        private Dictionary<uint, ItemUICategory> _itemUiCategory;
        private Dictionary<uint, ItemSearchCategory> _itemSearchCategory;
        private Dictionary<uint, ItemSortCategory> _itemSortCategory;
        private Dictionary<uint, EquipSlotCategory> _equipSlotCategories;
        private Dictionary<uint, EquipRaceCategory> _equipRaceCategories;
        private Dictionary<uint, Recipe> _recipeCache;
        private Dictionary<uint, HashSet<uint>> _classJobCategoryLookup;
        private readonly HashSet<uint> _armoireItems;
        private bool _itemUiCategoriesFullyLoaded;
        private bool _itemUiSearchFullyLoaded;
        private bool _recipeLookUpCalculated;
        private bool _companyCraftSequenceCalculated;
        private bool _classJobCategoryLookupCalculated;
        private bool _craftLevesItemLookupCalculated;
        private bool _armoireLoaded;
        private ENpcCollection _eNpcCollection;
        private ShopCollection _shopCollection;

        /// <summary>
        ///     Dictionary of each gc scrip shop and it's associated gc scrip shop items
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GcScripShopItemToGcScripCategories { get; private set; }

        
        /// <summary>
        ///     Dictionary of each gc scrip shop category and it's associated grand company
        /// </summary>
        public Dictionary<uint, uint> GcScripShopCategoryGrandCompany { get; private set; }
        
        /// <summary>
        ///     Dictionary of each gc ID matched to a gc shop 
        /// </summary>
        public Dictionary<uint, uint> GcShopGrandCompany { get; private set; }
        
        /// <summary>
        ///     Dictionary of gc scrip shop items and their associated items
        /// </summary>
        public Dictionary<(uint, uint), uint> GcScripShopToItem { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and associated scrip shop items
        /// </summary>
        public Dictionary<uint, HashSet<(uint, uint)>> ItemGcScripShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and associated fcc shops
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemFccShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of gil shop IDs and their associated item IDs
        /// </summary>
        public Dictionary<uint,HashSet<uint>> GilShopItemLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated gil shop IDs
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemGilShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of gil shop IDs and their associated gil shop item ids
        /// </summary>
        public Dictionary<uint,HashSet<uint>> GilShopGilShopItemLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated special shop IDs where the item is the result
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemSpecialShopResultLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated special shop IDs where the item is the cost
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemSpecialShopCostLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of all reward items and their associated cost items(currencies)
        /// </summary>
        public Dictionary<uint,HashSet<(uint,uint)>> SpecialShopItemRewardCostLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of all special shop cost items and their associated reward items(currencies)
        /// </summary>
        public Dictionary<uint,HashSet<(uint,uint)>> SpecialShopItemCostRewardLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and their associated gathering item IDs
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemGatheringItem { get; private set; }
        
        /// <summary>
        ///     Dictionary of item IDs and it's associated gathering types 
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemGatheringTypes { get; private set; }
        
        /// <summary>
        ///     Dictionary of each gathering item and it's associated points
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringItemToGatheringItemPoint { get; private set; }

        /// <summary>
        ///     Dictionary of each gathering item point and it's associated gathering point base
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringItemPointToGatheringPointBase  { get; private set; }

        /// <summary>
        ///     Dictionary of each gathering item point and it's associated gathering point base
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringPointBaseToGatheringPoint  { get; private set; }

        /// <summary>
        ///     Dictionary of each gathering item base to it's gathering type
        /// </summary>
        public Dictionary<uint, uint> GatheringPointBaseToGatheringType { get; private set; }

        /// <summary>
        ///     Dictionary of each item and it's associated aquarium fish(if applicable)
        /// </summary>
        public Dictionary<uint, uint> ItemToAquariumFish { get; private set; }
        
        /// <summary>
        ///     Dictionary of each result item when handing in an inspection item for HWD and it's required items + amount
        /// </summary>
        public Dictionary<uint, (uint, uint)> HwdInspectionResults { get; private set; }
        
        /// <summary>
        ///     Dictionary of all the shops referenced in the topic select sheet and their associated actual shop ids
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ShopToShopCollectionLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each item and it's related fish parameter
        /// </summary>
        public Dictionary<uint, uint> FishParameters { get; private set; }
        
        /// <summary>
        ///     Dictionary of each tomestone ID and it's related item
        /// </summary>
        public Dictionary<uint, uint> TomestoneLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each inclusion shop and it's categories
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopToCategoriesLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each inclusion shop category and it's associated shop
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopCategoryToShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each inclusion shop category and it's associated shop series
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopCategoryToShopSeriesLookup { get; private set; }

        public Dictionary<uint, uint> ItemToCabinetCategory
        {
            get
            {
                if (_itemToCabinetCategory == null)
                {
                    _itemToCabinetCategory = this.GetCabinetSheet().Where(c => c.Item.Row != 0)
                        .ToDictionary(c => c.Item.Row, c => c.Category.Value?.Category.Row ?? 0);
                }
                return _itemToCabinetCategory;
            }
            private set => _itemToCabinetCategory = value;
        }

        /// <summary>
        ///     Caches all the items so we don't have to enumerate each frame
        /// </summary>
        public Dictionary<uint, ItemEx> AllItems
        {
            get
            {
                return _allItems ??= GetItemExSheet().ToCache();
            }
        }

        private Dictionary<uint, ItemEx>? _allItems;
        
        public HashSet<uint> GetCurrencies(uint minimumEntries = 0)
        {
            return ItemSpecialShopCostLookup.Where(c => minimumEntries == 0 || c.Value.Count >= minimumEntries).Select(c => c.Key).ToHashSet();
        }

        /// <summary>
        ///     Dictionary of items and their associated recipes
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemRecipes
        {
            get => _itemRecipes ??= GetRecipeExSheet().ToColumnLookup(c => c.ItemResult.Row, c => c.RowId);
        }
        private Dictionary<uint, HashSet<uint>>? _itemRecipes;
        
        


        private readonly Dictionary<uint, Dictionary<uint, uint>>
            flattenedRecipes;

        private ConcurrentDictionary<uint, HashSet<uint>>? _itemToRetainerTaskNormalLookup;
        private Dictionary<uint,uint>? _retainerTaskToRetainerNormalLookup;

        //Key is the class job category and the hashset contains a list of class jobs
        public Dictionary<uint, HashSet<uint>> ClassJobCategoryLookup
        {
            get => _classJobCategoryLookup ?? new Dictionary<uint, HashSet<uint>>();
            set => _classJobCategoryLookup = value;
        }

        public Dictionary<uint, ItemUICategory> ItemUiCategory
        {
            get => _itemUiCategory ?? new Dictionary<uint, ItemUICategory>();
            set => _itemUiCategory = value;
        }

        public Dictionary<uint, ItemSearchCategory> SearchCategory
        {
            get => _itemSearchCategory ?? new Dictionary<uint, ItemSearchCategory>();
            set => _itemSearchCategory = value;
        }

        public Dictionary<uint, ItemSortCategory> SortCategory
        {
            get => _itemSortCategory ?? new Dictionary<uint, ItemSortCategory>();
            set => _itemSortCategory = value;
        }

        public Dictionary<uint, EquipSlotCategory> EquipSlotCategories
        {
            get => _equipSlotCategories ?? new Dictionary<uint, EquipSlotCategory>();
            set => _equipSlotCategories = value;
        }

        public Dictionary<uint, EventItem> EventItemCache
        {
            get => _eventItemCache ?? new Dictionary<uint, EventItem>();
            set => _eventItemCache = value;
        }

        public Dictionary<uint, Recipe> RecipeCache
        {
            get => _recipeCache ?? new Dictionary<uint, Recipe>();
            set => _recipeCache = value;
        }

        public Dictionary<uint, EquipRaceCategory> EquipRaceCategories
        {
            get => _equipRaceCategories ?? new Dictionary<uint, EquipRaceCategory>();
            set => _equipRaceCategories = value;
        }

        //Lookup of each recipe available for each item
        public Dictionary<uint, HashSet<uint>> RecipeLookupTable { get; set; }

        //Dictionary of every item that an item can craft
        public Dictionary<uint, HashSet<uint>> CraftLookupTable { get; set; }

        public Dictionary<uint, string> AddonNames { get; set; }

        public Dictionary<uint, uint> CraftLevesItemLookup { get; set; }

        public Dictionary<uint, uint> CompanyCraftSequenceByResultItemIdLookup { get; set; }
        
        public List<DungeonBoss> DungeonBosses { get; set; } 
        public List<DungeonBossDrop> DungeonBossDrops { get; set; } 
        public List<DungeonBossChest> DungeonBossChests { get; set; } 
        public List<ItemSupplement> ItemSupplements { get; set; }
        public List<SubmarineDrop> SubmarineDrops { get; set; }
        public List<AirshipDrop> AirshipDrops { get; set; }
        public List<DungeonChestItem> DungeonChestItems { get; set; }
        public List<DungeonDrop> DungeonDrops { get; set; }
        public List<DungeonChest> DungeonChests { get; set; }
        public List<MobSpawnPosition> MobSpawns { get; set; }
        
        public List<MobDrop> MobDrops { get; set; }

        private Dictionary<uint, List<ItemSupplement>>? _sourceSupplements;
        private Dictionary<uint, List<ItemSupplement>>? _useSupplements;
        public List<ItemSupplement>? GetSupplementSources(uint sourceItemId)
        {
            if (_sourceSupplements == null)
            {
                _sourceSupplements = ItemSupplements.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_sourceSupplements.ContainsKey(sourceItemId))
            {
                return _sourceSupplements[sourceItemId];
            }

            return null;
        }
        public List<ItemSupplement>? GetSupplementUses(uint useItemId)
        {
            if (_useSupplements == null)
            {
                _useSupplements = ItemSupplements.GroupBy(c => c.SourceItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_useSupplements.ContainsKey(useItemId))
            {
                return _useSupplements[useItemId];
            }

            return null;
        }


        private Dictionary<uint, List<SubmarineDrop>>? _submarineDrops;
        public List<SubmarineDrop>? GetSubmarineDrops(uint itemId)
        {
            if (_submarineDrops == null)
            {
                _submarineDrops = SubmarineDrops.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_submarineDrops.ContainsKey(itemId))
            {
                return _submarineDrops[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<AirshipDrop>>? _airshipDrops;
        public List<AirshipDrop>? GetAirshipDrops(uint itemId)
        {
            if (_airshipDrops == null)
            {
                _airshipDrops = AirshipDrops.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_airshipDrops.ContainsKey(itemId))
            {
                return _airshipDrops[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<DungeonChestItem>>? _dungeonChestItems;
        public List<DungeonChestItem>? GetDungeonChestItems(uint itemId)
        {
            if (_dungeonChestItems == null)
            {
                _dungeonChestItems = DungeonChestItems.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_dungeonChestItems.ContainsKey(itemId))
            {
                return _dungeonChestItems[itemId];
            }

            return null;
        }

        private Dictionary<uint, DungeonChest>? _dungeonChests;
        public DungeonChest? GetDungeonChest(uint chestId)
        {
            if (_dungeonChests == null)
            {
                _dungeonChests = DungeonChests.ToDictionary(c => c.RowId, c => c);
            }

            if (_dungeonChests.ContainsKey(chestId))
            {
                return _dungeonChests[chestId];
            }

            return null;
        }

        private Dictionary<uint, DungeonBoss>? _dungeonBosses;
        public DungeonBoss? GetDungeonBoss(uint bossId)
        {
            if (_dungeonBosses == null)
            {
                _dungeonBosses = DungeonBosses.ToDictionary(c => c.RowId, c => c);
            }

            if (_dungeonBosses.ContainsKey(bossId))
            {
                return _dungeonBosses[bossId];
            }

            return null;
        }

        private Dictionary<uint, List<DungeonBossDrop>>? _dungeonBossDrops;
        public List<DungeonBossDrop>? GetDungeonBossDrops(uint itemId)
        {
            if (_dungeonBossDrops == null)
            {
                _dungeonBossDrops = DungeonBossDrops.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_dungeonBossDrops.ContainsKey(itemId))
            {
                return _dungeonBossDrops[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<DungeonBossChest>>? _dungeonBossChests;
        public List<DungeonBossChest>? GetDungeonBossChests(uint itemId)
        {
            if (_dungeonBossChests == null)
            {
                _dungeonBossChests = DungeonBossChests.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_dungeonBossChests.ContainsKey(itemId))
            {
                return _dungeonBossChests[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<DungeonDrop>>? _dungeonDrops;
        public List<DungeonDrop>? GetDungeonDrops(uint itemId)
        {
            if (_dungeonDrops == null)
            {
                _dungeonDrops = DungeonDrops.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_dungeonDrops.ContainsKey(itemId))
            {
                return _dungeonDrops[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<MobDrop>>? _mobDrops;
        public List<MobDrop>? GetMobDrops(uint itemId)
        {
            if (_mobDrops == null)
            {
                _mobDrops = MobDrops.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_mobDrops.ContainsKey(itemId))
            {
                return _mobDrops[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<MobSpawnPosition>>? _mobSpawns;
        public List<MobSpawnPosition>? GetMobSpawns(uint bNpcNameId)
        {
            if (_mobSpawns == null)
            {
                _mobSpawns = MobSpawns.GroupBy(c => c.BNpcNameId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_mobSpawns.ContainsKey(bNpcNameId))
            {
                return _mobSpawns[bNpcNameId];
            }

            return null;
        }

        public ConcurrentDictionary<uint, HashSet<uint>> ItemToRetainerTaskNormalLookup
        {
            get
            {
                return _itemToRetainerTaskNormalLookup ??= new ConcurrentDictionary<uint, HashSet<uint>>(GetSheet<RetainerTaskNormal>()
                    .GroupBy(c => c.Item.Row)
                    .ToDictionary(c => c.Key, c => c.Select(t => t.RowId).ToHashSet()));
            }
        }
        
        public Dictionary<uint,uint> RetainerTaskToRetainerNormalLookup
        {
            get
            {
                return _retainerTaskToRetainerNormalLookup ??= GetSheet<RetainerTask>().ToSingleLookup(c => c.Task, c => c.RowId);
            }
        }

        public ENpcCollection ENpcCollection => _eNpcCollection;
        public ShopCollection ShopCollection => _shopCollection;

        public string GetAddonName(uint addonId)
        {
            if (AddonNames.ContainsKey(addonId)) return AddonNames[addonId];

            var addonSheet = GetSheet<Addon>();
            var addonRow = addonSheet.GetRow(addonId);
            if (addonRow != null)
            {
                AddonNames.Add(addonId, addonRow.Text);
                return addonRow.Text;
            }

            return "";
        }

        public bool CanCraftItem(uint rowId)
        {
            if (!_recipeLookUpCalculated) CalculateRecipeLookup();

            return RecipeLookupTable.ContainsKey(rowId);
        }

        public bool IsCraftItem(uint rowId)
        {
            if (!_recipeLookUpCalculated) CalculateRecipeLookup();

            return CraftLookupTable.ContainsKey(rowId) && CraftLookupTable[rowId].Count != 0;
        }

        public bool IsArmoireItem(uint rowId)
        {
            if (!_armoireLoaded) CalculateArmoireItems();

            return _armoireItems.Contains(rowId);
        }

        private ExcelCache()
        {
            _eventItemCache = new Dictionary<uint, EventItem>();
            _itemUiCategory = new Dictionary<uint, ItemUICategory>();
            _itemSearchCategory = new Dictionary<uint, ItemSearchCategory>();
            _itemSortCategory = new Dictionary<uint, ItemSortCategory>();
            _equipSlotCategories = new Dictionary<uint, EquipSlotCategory>();
            _equipRaceCategories = new Dictionary<uint, EquipRaceCategory>();
            _recipeCache = new Dictionary<uint, Recipe>();
            _classJobCategoryLookup = new Dictionary<uint, HashSet<uint>>();
            _armoireItems = new HashSet<uint>();
            flattenedRecipes = new Dictionary<uint, Dictionary<uint, uint>>();
            RecipeLookupTable = new Dictionary<uint, HashSet<uint>>();
            CraftLookupTable = new Dictionary<uint, HashSet<uint>>();
            AddonNames = new Dictionary<uint, string>();
            CraftLevesItemLookup = new Dictionary<uint, uint>();
            CompanyCraftSequenceByResultItemIdLookup = new Dictionary<uint, uint>();
            EventItemCache = new Dictionary<uint, EventItem>();
            EquipRaceCategories = new Dictionary<uint, EquipRaceCategory>();
            EquipSlotCategories = new Dictionary<uint, EquipSlotCategory>();
            SearchCategory = new Dictionary<uint, ItemSearchCategory>();
            SortCategory = new Dictionary<uint, ItemSortCategory>();
            ItemUiCategory = new Dictionary<uint, ItemUICategory>();
            GatheringItems = new Dictionary<uint, GatheringItem>();
            GatheringItemPoints = new Dictionary<uint, GatheringItemPoint>();
            GatheringItemPointLinks = new Dictionary<uint, uint>();
            GatheringItemsLinks = new Dictionary<uint, uint>();
            GatheringPoints = new Dictionary<uint, GatheringPoint>();
            GatheringPointsTransients = new Dictionary<uint, GatheringPointTransient>();
            RecipeLookupTable = new Dictionary<uint, HashSet<uint>>();
            RecipeCache = new Dictionary<uint, Recipe>();
            ClassJobCategoryLookup = new Dictionary<uint, HashSet<uint>>();
            CraftLevesItemLookup = new Dictionary<uint, uint>();
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _armoireLoaded = false;
        }

        public GameData GameData => _dataManager == null ? _gameData! : _dataManager.GameData;

        public Language Language => GameData.Options.DefaultExcelLanguage;

        public ExcelCache(DataManager dataManager) : this()
        {
            _dataManager = dataManager;
            Service.ExcelCache = this;
            LoadCsvs();
            //RetainerTaskEx.Run(() => RetainerTaskEx.Run(CalculateLookups));
            CalculateLookups();
        }

        public ExcelCache(GameData gameData) : this()
        {
            _gameData = gameData;
            Service.ExcelCache = this;
            //Need to fix this, basically stop the entire loading of the plugin until it's done then fire an event
            LoadCsvs();
            CalculateLookups();
            //RetainerTaskEx.Run(() => RetainerTaskEx.Run(CalculateLookups));
        }
        
        private void LoadCsvs()
        {
            DungeonBosses = LoadCsv<DungeonBoss>(CsvLoader.DungeonBossResourceName, "Dungeon Boss");
            DungeonBossChests = LoadCsv<DungeonBossChest>(CsvLoader.DungeonBossChestResourceName, "Dungeon Boss Chests");
            DungeonBossDrops = LoadCsv<DungeonBossDrop>(CsvLoader.DungeonBossDropResourceName, "Dungeon Boss Drops");
            DungeonChestItems = LoadCsv<DungeonChestItem>(CsvLoader.DungeonChestItemResourceName, "Dungeon Chest Items");
            DungeonChests = LoadCsv<DungeonChest>(CsvLoader.DungeonChestResourceName, "Dungeon Chests");
            DungeonDrops = LoadCsv<DungeonDrop>(CsvLoader.DungeonDropItemResourceName, "Dungeon Chest Items");
            ItemSupplements = LoadCsv<ItemSupplement>(CsvLoader.ItemSupplementResourceName, "Item Supplement");
            MobDrops = LoadCsv<MobDrop>(CsvLoader.MobDropResourceName, "Mob Drops");
            SubmarineDrops = LoadCsv<SubmarineDrop>(CsvLoader.SubmarineDropResourceName, "Submarine Drops");
            AirshipDrops = LoadCsv<AirshipDrop>(CsvLoader.AirshipDropResourceName, "Airship Drops");
            MobSpawns = LoadCsv<MobSpawnPosition>(CsvLoader.MobSpawnResourceName, "Mob Spawns");
        }

        private List<T> LoadCsv<T>(string resourceName, string title) where T : ICsv, new()
        {
            var list = CsvLoader.LoadResource<T>(resourceName, out var success);
            if (success)
            {
                return list;
            }
            PluginLog.Error("Failed to load " + title);
            return new List<T>();
        }
        
        public bool FinishedLoading { get; private set; }

        private void CalculateLookups()
        {
            GcScripShopCategoryGrandCompany = GetSheet<GCScripShopCategory>().ToSingleLookup(c => c.RowId, c => c.GrandCompany.Row);
            GcShopGrandCompany = GetSheet<GCShop>().ToSingleLookup(c => c.GrandCompany.Row, c => c.RowId);
            GcScripShopItemToGcScripCategories = GetSheet<GCScripShopItem>().ToColumnLookup(c => c.RowId, c => c.SubRowId);
            GcScripShopToItem = GetSheet<GCScripShopItem>().ToSingleTupleLookup(c => (c.RowId, c.SubRowId), c => c.Item.Row);
            ItemGcScripShopLookup = GetSheet<GCScripShopItem>().ToColumnLookupTuple(c => c.Item.Row, c => (c.RowId, c.SubRowId));
            GilShopItemLookup =
                GetSheet<GilShopItem>().ToColumnLookup(c => c.RowId, c => c.Item.Row);
            ItemGilShopLookup =
                GetSheet<GilShopItem>().ToColumnLookup(c => c.Item.Row, c => c.RowId);
            GilShopGilShopItemLookup =
                GetSheet<GilShopItem>().ToColumnLookup(c => c.RowId, c => c.SubRowId);
            ItemGatheringItem =
                GetSheet<GatheringItem>().ToColumnLookup(c => (uint)c.Item, c => c.RowId);
            ItemGatheringTypes =
                GetSheet<GatheringPointBase>().ToColumnLookup(c => c.Item, c => c.GatheringType.Row);
            GatheringItemToGatheringItemPoint =
                GetSheet<GatheringItemPoint>().ToColumnLookup(c => c.RowId, c => c.GatheringPoint.Row);
            GatheringItemPointToGatheringPointBase =
                GetSheet<GatheringPoint>().ToColumnLookup(c => c.RowId, c => c.GatheringPointBase.Row);
            GatheringPointBaseToGatheringType =
                GetSheet<GatheringPointBase>().ToSingleLookup(c => c.RowId, c => c.GatheringType.Row, true, false);
            GatheringPointBaseToGatheringPoint = GetSheet<GatheringPoint>().ToColumnLookup(c => c.GatheringPointBase.Row, c => c.RowId);
            ShopToShopCollectionLookup =
                GetSheet<TopicSelect>()
                    .ToDictionary(c => c.RowId, c => c.Shop.Distinct().Select(d => d).Where(d => d!= 0).ToHashSet());
            InclusionShopToCategoriesLookup =
                GetSheet<InclusionShop>().ToDictionary(c => c.RowId, c => c.Category.Select(d => d.Row).Where(e => e != 0).Distinct().ToHashSet());
            InclusionShopCategoryToShopLookup =
                GetSheet<InclusionShopSeries>().ToColumnLookup(c => c.RowId, c => c.SpecialShop.Row);
            InclusionShopCategoryToShopSeriesLookup =
                GetSheet<InclusionShopSeries>().ToColumnLookup(c => c.RowId, c => c.SpecialShop.Row);
            FishParameters = GetSheet<FishParameter>().ToSingleLookup(c => (uint)c.Item, c => c.RowId);
            TomestoneLookup = GetSheet<TomestonesItem>().ToSingleLookup(c => c.RowId, c => c.Item.Row);
            ItemToAquariumFish = GetSheet<AquariumFish>().ToSingleLookup(c => c.Item.Row, c => c.RowId);
            Dictionary<uint, (uint, uint)> inspectionResults = new Dictionary<uint, (uint, uint)>();
            foreach (var inspection in GetSheet<HWDGathererInspectionEx>())
            {
                inspectionResults = inspection.GenerateInspectionResults(inspectionResults);
            }

            HwdInspectionResults = inspectionResults;
            //Special case for special shops because square can't pick a lane
            var itemSpecialShopResults = new Dictionary<uint, HashSet<uint>>();
            var itemSpecialShopCosts = new Dictionary<uint, HashSet<uint>>();
            var specialShopItemRewardCostLookup = new Dictionary<uint, HashSet<(uint, uint)>>();
            var specialShopItemCostRewardLookup = new Dictionary<uint, HashSet<(uint,uint)>>();
            foreach (var sheet in GetSheet<SpecialShopEx>())
            {
                foreach (var listing in sheet.ShopListings.ToList())
                {
                    foreach (var item in listing.Rewards)
                    {
                        if (item.ItemEx.Row == 34850)
                        {
                            var a = "";
                        }
                        if (!itemSpecialShopResults.ContainsKey(item.ItemEx.Row))
                        {
                            itemSpecialShopResults.Add(item.ItemEx.Row, new HashSet<uint>());
                        }

                        itemSpecialShopResults[item.ItemEx.Row].Add(sheet.RowId);
                        
                        if (!specialShopItemRewardCostLookup.ContainsKey(item.ItemEx.Row))
                        {
                            specialShopItemRewardCostLookup.Add(item.ItemEx.Row, new HashSet<(uint, uint)>());
                        }
                        foreach (var costItem in listing.Costs)
                        {
                            specialShopItemRewardCostLookup[item.ItemEx.Row].Add(((uint)costItem.Count,costItem.ItemEx.Row));
                        }
                        
                    }

                    foreach (var item in listing.Costs)
                    {
                        if (!itemSpecialShopCosts.ContainsKey(item.ItemEx.Row))
                        {
                            itemSpecialShopCosts.Add(item.ItemEx.Row, new HashSet<uint>());
                        }

                        itemSpecialShopCosts[item.ItemEx.Row].Add(sheet.RowId);
                        
                        if (!specialShopItemCostRewardLookup.ContainsKey(item.ItemEx.Row))
                        {
                            specialShopItemCostRewardLookup.Add(item.ItemEx.Row, new HashSet<(uint,uint)>());
                        }
                        foreach (var rewardItem in listing.Rewards)
                        {
                            specialShopItemCostRewardLookup[item.ItemEx.Row].Add(((uint)item.Count, rewardItem.ItemEx.Row));
                        }
                    }
                }
            }

            ItemFccShopLookup = new Dictionary<uint, HashSet<uint>>();
            foreach (var sheet in GetSheet<FccShopEx>())
            {
                foreach (var item in sheet.Items)
                {
                    if (!ItemFccShopLookup.ContainsKey(item.Row))
                    {
                        ItemFccShopLookup.Add(item.Row, new HashSet<uint>());
                    }

                    ItemFccShopLookup[item.Row].Add(sheet.RowId);
                }
            }

            ItemSpecialShopResultLookup = itemSpecialShopResults;
            ItemSpecialShopCostLookup = itemSpecialShopCosts;
            SpecialShopItemRewardCostLookup = specialShopItemRewardCostLookup;
            SpecialShopItemCostRewardLookup = specialShopItemCostRewardLookup;
                
            _eNpcCollection = new ENpcCollection();
            _shopCollection = new ShopCollection();
            FinishedLoading = true;
        }



        public void Destroy()
        {
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _recipeLookUpCalculated = false;
            _companyCraftSequenceCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _vendorLocationsCalculated = false;
        }

        public bool IsItemCraftLeve(uint itemId)
        {
            CalculateCraftLevesItemLookup();
            if (CraftLevesItemLookup.ContainsKey(itemId)) return true;

            return false;
        }

        public CraftLeve? GetCraftLevel(uint itemId)
        {
            CalculateCraftLevesItemLookup();
            if (CraftLevesItemLookup.ContainsKey(itemId))
                return GetSheet<CraftLeve>().GetRow(CraftLevesItemLookup[itemId]);

            return null;
        }

        public void CalculateCraftLevesItemLookup()
        {
            if (!_craftLevesItemLookupCalculated)
            {
                _craftLevesItemLookupCalculated = true;
                foreach (var craftLeve in GetSheet<CraftLeve>())
                foreach (var item in craftLeve.UnkData3)
                    if (!CraftLevesItemLookup.ContainsKey((uint)item.Item))
                        CraftLevesItemLookup.Add((uint)item.Item, craftLeve.RowId);
            }
        }
        public ExcelSheet<T> GetSheet<T>() where T : ExcelRow
        {
            if (_dataManager != null)
                return _dataManager.Excel.GetSheet<T>()!;
            if (_gameData != null) return _gameData.GetExcelSheet<T>()!;

            throw new Exception("You must initialise the cache with a data manager instance or game data instance");
        }

        public T? GetFile<T>(string path) where T : FileResource
        {
            if (_dataManager != null)
                return _dataManager.GetFile<T>(path)!;
            if (_gameData != null) return _gameData.GetFile<T>(path)!;

            throw new Exception("You must initialise the cache with a data manager instance or game data instance");
        }

        public EventItem? GetEventItem(uint itemId)
        {
            if (!EventItemCache.ContainsKey(itemId))
            {
                var item = GetSheet<EventItem>().GetRow(itemId);
                if (item == null) return null;

                EventItemCache[itemId] = item;
            }

            return EventItemCache[itemId];
        }

        public Recipe? GetRecipe(uint recipeId)
        {
            if (!RecipeCache.ContainsKey(recipeId))
            {
                var recipe = GetRecipeExSheet().GetRow(recipeId);
                if (recipe == null) return null;

                RecipeCache[recipeId] = recipe;
            }

            return RecipeCache[recipeId];
        }

        private Dictionary<uint, uint> GetFlattenedItemRecipeLoop(Dictionary<uint, uint> itemIds, uint itemId,
            uint quantity, int maxDepth = -1)
        {
            var recipes = GetItemRecipes(itemId);
            foreach (var recipe in recipes)
            {
                foreach (var ingredient in recipe.UnkData5)
                {
                    if (ingredient.ItemIngredient == 0 || ingredient.AmountIngredient == 0) continue;
                    if (!itemIds.ContainsKey((uint)ingredient.ItemIngredient))
                        itemIds.Add((uint)ingredient.ItemIngredient, 0);

                    itemIds[(uint)ingredient.ItemIngredient] += ingredient.AmountIngredient * quantity;

                    if (CanCraftItem((uint)ingredient.ItemIngredient))
                        GetFlattenedItemRecipeLoop(itemIds, (uint)ingredient.ItemIngredient, quantity, maxDepth);
                }
            }

            if (recipes.Count == 0)
            {
                if (!_companyCraftSequenceCalculated) CalculateCompanyCraftSequenceByResultItemId();

                if (CompanyCraftSequenceByResultItemIdLookup.ContainsKey(itemId))
                {
                    //Might need to split into parts at some point
                    var companyCraftSequence = GetCompanyCraftSequenceSheet()
                        .GetRow(CompanyCraftSequenceByResultItemIdLookup[itemId]);
                    if (companyCraftSequence != null)
                        foreach (var lazyPart in companyCraftSequence.CompanyCraftPart)
                        {
                            var part = lazyPart.Value;
                            if (part != null)
                                foreach (var lazyProcess in part.CompanyCraftProcess)
                                {
                                    var process = lazyProcess.Value;
                                    if (process != null)
                                        foreach (var supplyItem in process.UnkData0)
                                        {
                                            var actualItem = GetCompanyCraftSupplyItemSheet()
                                                .GetRow(supplyItem.SupplyItem);
                                            if (actualItem != null)
                                                if (actualItem.Item.Row != 0 && supplyItem.SetQuantity != 0)
                                                {
                                                    if (!itemIds.ContainsKey(actualItem.Item.Row))
                                                        itemIds.Add(actualItem.Item.Row, 0);

                                                    itemIds[actualItem.Item.Row] += (uint)supplyItem.SetQuantity *
                                                        supplyItem.SetsRequired * quantity;

                                                    GetFlattenedItemRecipeLoop(itemIds, actualItem.Item.Row, quantity, maxDepth);
                                                }
                                        }
                                }
                        }
                }
            }

            return itemIds;
        }

        
        public Dictionary<uint, uint> GetFlattenedItemRecipe(uint itemId, bool includeSelf = false,
            uint quantity = 1, int maxDepth = -1)
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

            var flattenedItemRecipeLoop = GetFlattenedItemRecipeLoop(new Dictionary<uint, uint>(), itemId, quantity, maxDepth);
            flattenedRecipes.Add(itemId, flattenedItemRecipeLoop);
            if (includeSelf)
            {
                var flattenedItemRecipe = flattenedRecipes[itemId].ToDictionary(c => c.Key, c => c.Value);
                flattenedItemRecipe.Add(itemId, 1);
            }

            return flattenedItemRecipeLoop;
        }

        public bool IsCompanyCraft(uint itemId)
        {
            if (itemId == 0) return false;

            if (!_companyCraftSequenceCalculated) CalculateCompanyCraftSequenceByResultItemId();

            return CompanyCraftSequenceByResultItemIdLookup.ContainsKey(itemId);
        }

        public CompanyCraftSequence? GetCompanyCraftSequenceByItemId(uint itemId)
        {
            if (itemId == 0) return null;

            if (!_companyCraftSequenceCalculated) CalculateCompanyCraftSequenceByResultItemId();

            if (CompanyCraftSequenceByResultItemIdLookup.ContainsKey(itemId))
                return GetCompanyCraftSequenceSheet()
                    .GetRow(CompanyCraftSequenceByResultItemIdLookup[itemId]);

            return null;
        }

        public void CalculateCompanyCraftSequenceByResultItemId()
        {
            if (!_companyCraftSequenceCalculated)
            {
                _companyCraftSequenceCalculated = true;
                foreach (var companyCraftSequence in GetCompanyCraftSequenceSheet())
                    if (!CompanyCraftSequenceByResultItemIdLookup.ContainsKey(companyCraftSequence.ResultItem.Row))
                        CompanyCraftSequenceByResultItemIdLookup.Add(companyCraftSequence.ResultItem.Row,
                            companyCraftSequence.RowId);
            }
        }

        public void CalculateCompanyCraftSequenceByRequiredItemId()
        {
            if (!_companyCraftSequenceCalculated)
            {
                _companyCraftSequenceCalculated = true;
                Dictionary<uint, uint> itemIds = new Dictionary<uint, uint>();
                foreach (var companyCraftSequence in GetCompanyCraftSequenceSheet())
                {
                    var parts = companyCraftSequence.CompanyCraftPart;
                    foreach (var part in parts)
                    {
                        if (part.Value != null)
                        {
                            var processes = part.Value.CompanyCraftProcess;
                            foreach (var process in processes)
                            {
                                if (process.Value != null)
                                {
                                    var supplyItems = process.Value.UnkData0;
                                    foreach (var supplyItem in supplyItems)
                                    {
                                        var actualItem = GetCompanyCraftSupplyItemSheet()
                                            .GetRow(supplyItem.SupplyItem);
                                        if (actualItem != null)
                                        {
                                            if (actualItem.Item.Row != 0 && supplyItem.SetQuantity != 0)
                                            {
                                                if (!itemIds.ContainsKey(actualItem.Item.Row))
                                                    itemIds.Add(actualItem.Item.Row, 0);

                                                itemIds[actualItem.Item.Row] += (uint)supplyItem.SetQuantity *
                                                    supplyItem.SetsRequired;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //TODO: FINISH ME
                    //if (!CompanyCraftSequenceByRequiredItemIdLookup.ContainsKey(companyCraftSequence.ResultItem.Row))
                    //    CompanyCraftSequenceByRequiredItemIdLookup.Add(companyCraftSequence.ResultItem.Row,
                    //        companyCraftSequence.RowId);
                }
            }
        }

        public List<Recipe> GetItemRecipes(uint itemId)
        {
            if (itemId == 0) return new List<Recipe>();

            if (!_recipeLookUpCalculated) CalculateRecipeLookup();

            var recipes = new List<Recipe>();
            if (RecipeLookupTable.ContainsKey(itemId))
                foreach (var lookup in RecipeLookupTable[itemId])
                {
                    var recipe = GetRecipe(lookup);
                    if (recipe != null) recipes.Add(recipe);
                }

            return recipes;
        }

        public Dictionary<uint, ItemUICategory> GetAllItemUICategories()
        {
            if (!_itemUiCategoriesFullyLoaded)
            {
                _itemUiCategory = GetSheet<ItemUICategory>().ToDictionary(c => c.RowId);
                _itemUiCategoriesFullyLoaded = true;
            }

            return _itemUiCategory;
        }

        public Dictionary<uint, ItemSearchCategory> GetAllItemSearchCategories()
        {
            if (!_itemUiSearchFullyLoaded)
            {
                _itemSearchCategory = GetSheet<ItemSearchCategory>().ToDictionary(c => c.RowId);
                _itemUiSearchFullyLoaded = true;
            }

            return _itemSearchCategory;
        }

        public void CalculateRecipeLookup()
        {
            if (!_recipeLookUpCalculated)
            {
                _recipeLookUpCalculated = true;
                foreach (var recipe in GetRecipeExSheet())
                {
                    if (recipe.ItemResult.Row != 0)
                    {
                        if (!RecipeLookupTable.ContainsKey(recipe.ItemResult.Row))
                            RecipeLookupTable.Add(recipe.ItemResult.Row, new HashSet<uint>());

                        RecipeLookupTable[recipe.ItemResult.Row].Add(recipe.RowId);
                        foreach (var item in recipe.UnkData5)
                        {
                            if (item.ItemIngredient != 0)
                            {
                                if (!CraftLookupTable.ContainsKey((uint)item.ItemIngredient))
                                    CraftLookupTable.Add((uint)item.ItemIngredient, new HashSet<uint>());

                                var hashSet = CraftLookupTable[(uint)item.ItemIngredient];
                                hashSet.Add(recipe.ItemResult.Row);
                            }
                        }
                    }
                }
            }
        }

        public void CalculateArmoireItems()
        {
            if (!_armoireLoaded)
            {
                _armoireLoaded = true;
                foreach (var armoireItem in GetSheet<Cabinet>())
                {
                    _armoireItems.Add(armoireItem.Item.Row);
                }
            }
        }

        public bool IsItemEquippableBy(uint classJobCategory, uint classJobId)
        {
            CalculateClassJobCategoryLookup();
            if (!_classJobCategoryLookup.ContainsKey(classJobCategory)) return false;

            if (!_classJobCategoryLookup[classJobCategory].Contains(classJobId)) return false;

            return true;
        }

        public void CalculateClassJobCategoryLookup()
        {
            if (!_classJobCategoryLookupCalculated)
            {
                var classJobMap = new Dictionary<string, uint>();
                foreach (var classJob in GetSheet<ClassJob>())
                    if (!classJobMap.ContainsKey(classJob.Abbreviation))
                        classJobMap[classJob.Abbreviation] = classJob.RowId;

                _classJobCategoryLookupCalculated = true;
                var classJobCategoryMap = new Dictionary<uint, HashSet<uint>>();
                var propertyInfos = typeof(ClassJobCategory).GetProperties().Where(c => c.PropertyType == typeof(bool))
                    .ToList();

                foreach (var classJobCategory in GetSheet<ClassJobCategory>())
                {
                    //Dont hate me
                    var map = new HashSet<uint>();
                    foreach (var prop in propertyInfos)
                    {
                        var parsed = prop.GetValue(classJobCategory, null);
                        if (parsed is bool b && (bool?)b == true)
                            if (classJobMap.ContainsKey(prop.Name))
                            {
                                var classJobRowId = classJobMap[prop.Name];
                                map.Add(classJobRowId);
                            }
                    }

                    classJobCategoryMap[classJobCategory.RowId] = map;
                }

                _classJobCategoryLookup = classJobCategoryMap;
            }
        }

        public bool IsItemAvailableAtTimedNode(uint itemId)
        {
            if (!_gatheringItemLinksCalculated) CalculateGatheringItemLinks();

            if (!_gatheringItemPointLinksCalculated) CalculateGatheringItemPointLinks();

            if (GatheringItemsLinks.ContainsKey(itemId))
            {
                var gatheringItemId = GatheringItemsLinks[itemId];
                if (GatheringItemPointLinks.ContainsKey(gatheringItemId))
                {
                    var gatheringPointId = GatheringItemPointLinks[gatheringItemId];
                    var gatheringPointTransient = GetGatheringPointTransient(gatheringPointId);
                    if (gatheringPointTransient != null)
                        return gatheringPointTransient.GatheringRarePopTimeTable.Row != 0;
                }
            }

            return false;
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
            }
            _disposed = true;         
        }
        
        ~ExcelCache()
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

        public ExcelSheet<ENpcBase> GetENpcBaseSheet()
        {
            return _enpcBaseSheet ??= GetSheet<ENpcBase>();
        }

        public ExcelSheet<BNpcNameEx> GetBNpcNameExSheet()
        {
            return _bNpcNameSheet ??= GetSheet<BNpcNameEx>();
        }

        public ExcelSheet<BNpcBaseEx> GetBNpcBaseExSheet()
        {
            return _bNpcBaseSheet ??= GetSheet<BNpcBaseEx>();
        }

        public ExcelSheet<PlaceName> GetPlaceNameSheet()
        {
            return _placeNameSheet ??= GetSheet<PlaceName>();
        }

        public ExcelSheet<ENpcResident> GetENpcResidentSheet()
        {
            return _enpcResidentSheet ??= GetSheet<ENpcResident>();
        }

        public ExcelSheet<TerritoryTypeEx> GetTerritoryTypeExSheet()
        {
            return _territoryTypeExSheet ??= GetSheet<TerritoryTypeEx>();
        }

        public ExcelSheet<GilShopEx> GetGilShopExSheet()
        {
            return _gilShopExSheet ??= GetSheet<GilShopEx>();
        }

        public ExcelSheet<CustomTalk> GetCustomTalkSheet()
        {
            return _customTalkSheet ??= GetSheet<CustomTalk>();
        }

        public ExcelSheet<GCShopEx> GetGCShopExSheet()
        {
            return _gcShopExSheet ??= GetSheet<GCShopEx>();
        }

        public ExcelSheet<SpecialShopEx> GetSpecialShopExSheet()
        {
            return _specialShopExSheet ??= GetSheet<SpecialShopEx>();
        }

        public ExcelSheet<CompanyCraftSupplyItemEx> GetCompanyCraftSupplyItemSheet()
        {
            return _companyCraftSupplyItemSheetEx ??= GetSheet<CompanyCraftSupplyItemEx>();
        }

        public ExcelSheet<ClassJob> GetClassJobSheet()
        {
            return _classJobSheet ??= GetSheet<ClassJob>();
        }

        public ExcelSheet<ItemUICategory> GetItemUICategorySheet()
        {
            return _itemUiCategorySheet ??= GetSheet<ItemUICategory>();
        }

        public ExcelSheet<ItemSearchCategory> GetItemSearchCategorySheet()
        {
            return _itemSearchCategorySheet ??= GetSheet<ItemSearchCategory>();
        }

        public ExcelSheet<EquipSlotCategory> GetEquipSlotCategorySheet()
        {
            return _equipSlotCategorySheet ??= GetSheet<EquipSlotCategory>();
        }

        public ExcelSheet<ItemSortCategory> GetItemSortCategorySheet()
        {
            return _itemSortCategorySheet ??= GetSheet<ItemSortCategory>();
        }
        
        public ExcelSheet<ItemEx> GetItemExSheet()
        {
            return _itemExSheet ??= GetSheet<ItemEx>();
        }
        
        public ExcelSheet<AirshipExplorationPoint> GetAirshipExplorationPointSheet()
        {
            return _airshipExplorationPointSheet ??= GetSheet<AirshipExplorationPoint>();
        }
        
        public ExcelSheet<SubmarineExploration> GetSubmarineExplorationSheet()
        {
            return _submarineExplorationSheet ??= GetSheet<SubmarineExploration>();
        }

        public ExcelSheet<CabinetCategory> GetCabinetCategorySheet()
        {
            return _cabinetCategorySheet ??= GetSheet<CabinetCategory>();
        }
        
        public ExcelSheet<RecipeEx> GetRecipeExSheet()
        {
            return _recipeExSheet ??= GetSheet<RecipeEx>();
        }
        
        public ExcelSheet<PreHandler> GetPreHandlerSheet()
        {
            return _preHandlerSheet ??= GetSheet<PreHandler>();
        }

        public ExcelSheet<TopicSelect> GetTopicSelectSheet()
        {
            return _topicSelectSheet ??= GetSheet<TopicSelect>();
        }

        public ExcelSheet<GatheringPointEx> GetGatheringPointExSheet()
        {
            return _gatheringPointExSheet ??= GetSheet<GatheringPointEx>();
        }

        public ExcelSheet<GatheringItemEx> GetGatheringItemExSheet()
        {
            return _gatheringItemExSheet ??= GetSheet<GatheringItemEx>();
        }

        public ExcelSheet<TripleTriadCard> GetTripleTriadCardSheet()
        {
            return _tripleTriadCardSheet ??= GetSheet<TripleTriadCard>();
        }

        public ExcelSheet<Cabinet> GetCabinetSheet()
        {
            return _cabinetSheet ??= GetSheet<Cabinet>();
        }

        public ExcelSheet<EquipRaceCategoryEx> GetEquipRaceCategoryExSheet()
        {
            return _equipRaceCategoryExSheet ??= GetSheet<EquipRaceCategoryEx>();
        }

        public ExcelSheet<EquipSlotCategoryEx> GetEquipSlotCategoryExSheet()
        {
            return _equipSlotCategoryExSheet ??= GetSheet<EquipSlotCategoryEx>();
        }

        public ExcelSheet<ClassJobCategoryEx> GetClassJobCategoryExSheet()
        {
            return _classJobCategoryExSheet ??= GetSheet<ClassJobCategoryEx>();
        }

        public ExcelSheet<RetainerTaskNormalEx> GetRetainerTaskNormalExSheet()
        {
            return _retainerTaskNormalExSheet ??= GetSheet<RetainerTaskNormalEx>();
        }
        
        private ExcelSheet<CompanyCraftSequence> GetCompanyCraftSequenceSheet()
        {
            return _companyCraftSequenceSheet ??= GetSheet<CompanyCraftSequence>();
        }

        private ExcelSheet<GatheringItem> GetGatheringItemSheet()
        {
            return _gatheringItemSheet ??= GetSheet<GatheringItem>();
        }

        private ExcelSheet<GatheringItemPoint> GetGatheringItemPointSheet()
        {
            return _gatheringItemPointSheet ??= GetSheet<GatheringItemPoint>();
        }

        private ExcelSheet<LevelEx> GetLevelExSheet()
        {
            return _levelExSheet ??= GetSheet<LevelEx>();
        }
        
        private ExcelSheet<GatheringPointTransient> GetGatheringPointTransientSheet()
        {
            return _gatheringPointTransientSheet ??= GetSheet<GatheringPointTransient>();
        }
        
        public ExcelSheet<UIColor> GetUIColorSheet()
        {
            return _uiColorSheet ??= GetSheet<UIColor>();
        }
        
        public ExcelSheet<Race> GetRaceSheet()
        {
            return _raceSheet ??= GetSheet<Race>();
        }
        
        public ExcelSheet<Stain> GetStainSheet()
        {
            return _stainSheet ??= GetSheet<Stain>();
        }
        
        public ExcelSheet<World> GetWorldSheet()
        {
            return _worldSheet ??= GetSheet<World>();
        }
        
        public ExcelSheet<ContentFinderConditionEx> GetContentFinderConditionExSheet()
        {
            return _contentFinderConditionExSheet ??= GetSheet<ContentFinderConditionEx>();
        }
        
        public ExcelSheet<ContentType> GetContentTypeSheet()
        {
            return _contentTypeSheet ??= GetSheet<ContentType>();
        }
        
        public ExcelSheet<ContentRoulette> GetContentRouletteSheet()
        {
            return _contentRouletteSheet ??= GetSheet<ContentRoulette>();
        }

        private ExcelSheet<ItemEx>? _itemExSheet;
        private ExcelSheet<CabinetCategory>? _cabinetCategorySheet;
        private ExcelSheet<RecipeEx>? _recipeExSheet;
        private ExcelSheet<EquipSlotCategory>? _equipSlotCategorySheet;
        private ExcelSheet<ItemSortCategory>? _itemSortCategorySheet;
        private ExcelSheet<ENpcBase>? _enpcBaseSheet;
        private ExcelSheet<BNpcNameEx>? _bNpcNameSheet;
        private ExcelSheet<BNpcBaseEx>? _bNpcBaseSheet;
        private ExcelSheet<PlaceName>? _placeNameSheet;
        private ExcelSheet<ItemSearchCategory>? _itemSearchCategorySheet;
        private ExcelSheet<ItemUICategory>? _itemUiCategorySheet;
        private ExcelSheet<ClassJob>? _classJobSheet;
        private ExcelSheet<CompanyCraftSupplyItemEx>? _companyCraftSupplyItemSheetEx;
        private ExcelSheet<SpecialShopEx>? _specialShopExSheet;
        private ExcelSheet<GCShopEx>? _gcShopExSheet;
        private ExcelSheet<CustomTalk>? _customTalkSheet;
        private ExcelSheet<GilShopEx>? _gilShopExSheet;
        private ExcelSheet<TerritoryTypeEx>? _territoryTypeExSheet;
        private ExcelSheet<ENpcResident>? _enpcResidentSheet;
        private ExcelSheet<PreHandler>? _preHandlerSheet;
        private ExcelSheet<LevelEx>? _levelExSheet;
        private ExcelSheet<GatheringItemPoint>? _gatheringItemPointSheet;
        private ExcelSheet<GatheringItem>? _gatheringItemSheet;
        private ExcelSheet<CompanyCraftSequence>? _companyCraftSequenceSheet;
        private ExcelSheet<RetainerTaskNormalEx>? _retainerTaskNormalExSheet;
        private ExcelSheet<EquipSlotCategoryEx>? _equipSlotCategoryExSheet;
        private ExcelSheet<ClassJobCategoryEx>? _classJobCategoryExSheet;
        private ExcelSheet<EquipRaceCategoryEx>? _equipRaceCategoryExSheet;
        private ExcelSheet<Cabinet>? _cabinetSheet;
        private ExcelSheet<TripleTriadCard>? _tripleTriadCardSheet;
        private ExcelSheet<GatheringItemEx>? _gatheringItemExSheet;
        private ExcelSheet<GatheringPointEx>? _gatheringPointExSheet;
        private ExcelSheet<TopicSelect>? _topicSelectSheet;
        private ExcelSheet<GatheringPointTransient>? _gatheringPointTransientSheet;
        private ExcelSheet<UIColor>? _uiColorSheet;
        private ExcelSheet<Race>? _raceSheet;
        private ExcelSheet<Stain>? _stainSheet;
        private ExcelSheet<World>? _worldSheet;
        private ExcelSheet<ContentFinderConditionEx>? _contentFinderConditionExSheet;
        private ExcelSheet<ContentType>? _contentTypeSheet;
        private ExcelSheet<ContentRoulette>? _contentRouletteSheet;
        private ExcelSheet<AirshipExplorationPoint>? _airshipExplorationPointSheet;
        private ExcelSheet<SubmarineExploration>? _submarineExplorationSheet;
        private Dictionary<uint, uint>? _itemToCabinetCategory;
    }
}