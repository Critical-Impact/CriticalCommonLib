using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CriticalCommonLib.Collections;
using CriticalCommonLib.Extensions;
using Lumina;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using CriticalCommonLib.Sheets;
using Dalamud.Plugin.Services;
using Lumina.Data;
using LuminaSupplemental.Excel.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache : IHostedService, IDisposable
    {
        private IDataManager? _dataManager;
        private readonly ILogger<ExcelCache> _logger;
        private GameData? _gameData;
        
        private Dictionary<uint, EventItem> _eventItemCache;
        private Dictionary<uint, ItemUICategory> _itemUiCategory;
        private Dictionary<uint, ItemSearchCategory> _itemSearchCategory;
        private Dictionary<uint, ItemSortCategory> _itemSortCategory;
        private Dictionary<uint, EquipSlotCategory> _equipSlotCategories;
        private Dictionary<uint, EquipRaceCategory> _equipRaceCategories;
        private ConcurrentDictionary<uint, RecipeEx>? _recipeCache;
        private Dictionary<uint, HashSet<uint>> _classJobCategoryLookup;
        private readonly HashSet<uint> _armoireItems;
        private bool _itemUiCategoriesFullyLoaded;
        private bool _itemUiSearchFullyLoaded;
        private bool _recipeLookUpCalculated;
        private bool _hwdCrafterSupplyLookupCalculated;
        private bool _classJobCategoryLookupCalculated;
        private bool _craftLevesItemLookupCalculated;
        private bool _armoireLoaded;
        private ENpcCollection? _eNpcCollection;
        private ShopCollection? _shopCollection;

        public ConcurrentDictionary<string, Dictionary<uint, List<uint>>> OneToManyCache =
            new ConcurrentDictionary<string, Dictionary<uint, List<uint>>>();

        public ConcurrentDictionary<string, Dictionary<uint, uint>> OneToOneCache =
            new ConcurrentDictionary<string, Dictionary<uint, uint>>();

        /// <summary>
        ///     Dictionary of each gc scrip shop and it's associated gc scrip shop items
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GcScripShopItemToGcScripCategories { get; private set; } = null!;


        /// <summary>
        ///     Dictionary of each gc scrip shop category and it's associated grand company
        /// </summary>
        public Dictionary<uint, uint> GcScripShopCategoryGrandCompany { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each gc ID matched to a gc shop 
        /// </summary>
        public Dictionary<uint, uint> GcShopGrandCompany { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of gc scrip shop items and their associated items
        /// </summary>
        public Dictionary<(uint, uint), uint> GcScripShopToItem { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of item IDs and associated scrip shop items
        /// </summary>
        public Dictionary<uint, HashSet<(uint, uint)>> ItemGcScripShopLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of item IDs and associated fcc shops
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemFccShopLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of gil shop IDs and their associated item IDs
        /// </summary>
        public Dictionary<uint,HashSet<uint>> GilShopItemLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of item IDs and their associated gil shop IDs
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemGilShopLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of gil shop IDs and their associated gil shop item ids
        /// </summary>
        public Dictionary<uint,HashSet<uint>> GilShopGilShopItemLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of item IDs and their associated special shop IDs where the item is the result
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemSpecialShopResultLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of item IDs and their associated special shop IDs where the item is the cost
        /// </summary>
        public Dictionary<uint,HashSet<uint>> ItemSpecialShopCostLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of all reward items and their associated cost items(currencies)
        /// </summary>
        public Dictionary<uint,HashSet<(uint,uint)>> SpecialShopItemRewardCostLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of all special shop cost items and their associated reward items(currencies)
        /// </summary>
        public Dictionary<uint,HashSet<(uint,uint)>> SpecialShopItemCostRewardLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of item IDs and their associated gathering item IDs
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ItemGatheringItem { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each gathering item and it's associated points
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringItemToGatheringItemPoint { get; private set; } = null!;

        /// <summary>
        ///     Dictionary of each gathering item point and it's associated gathering point base
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringItemPointToGatheringPointBase  { get; private set; } = null!;

        /// <summary>
        ///     Dictionary of each gathering item point and it's associated gathering point base
        /// </summary>
        public Dictionary<uint, HashSet<uint>> GatheringPointBaseToGatheringPoint  { get; private set; } = null!;

        /// <summary>
        ///     Dictionary of each item and it's associated aquarium fish(if applicable)
        /// </summary>
        public Dictionary<uint, uint> ItemToAquariumFish { get; private set; } = null!;

        /// <summary>
        ///     Dictionary of each item and it's associated daily supply item
        /// </summary>
        public Dictionary<uint, uint> ItemToDailySupplyItem { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each result item when handing in an inspection item for HWD and it's required items + amount
        /// </summary>
        public Dictionary<uint, (uint, uint)> HwdInspectionResults { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of all the shops referenced in the topic select sheet and their associated actual shop ids
        /// </summary>
        public Dictionary<uint, HashSet<uint>> ShopToShopCollectionLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each item and it's related fish parameter
        /// </summary>
        public Dictionary<uint, uint> FishParameters { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each tomestone ID and it's related item
        /// </summary>
        public Dictionary<uint, uint> TomestoneLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each inclusion shop and it's categories
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopToCategoriesLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each inclusion shop category and it's associated shop
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopCategoryToShopLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each inclusion shop category and it's associated shop series
        /// </summary>
        public Dictionary<uint, HashSet<uint>> InclusionShopCategoryToShopSeriesLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each company craft sequence by it's result item
        /// </summary>
        public Dictionary<uint, uint> CompanyCraftSequenceByResultItemIdLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each item and it's related satisfaction supply row
        /// </summary>
        public Dictionary<uint, uint> ItemToSatisfactionSupplyLookup { get; private set; } = null!;
        
        /// <summary>
        ///     Dictionary of each special shop and it's associated fate shop if applicable
        /// </summary>
        public Dictionary<uint, uint> SpecialShopToFateShopLookup { get; private set; }
        
        /// <summary>
        ///     Dictionary of each item by it's company craft sequence id
        /// </summary>
        public Dictionary<uint, uint> ItemIdToCompanyCraftSequenceLookup { get; private set; } = null!;

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

        private ConcurrentDictionary<uint, List<RetainerTaskNormalEx>>? _itemToRetainerTaskNormalLookup;
        private Dictionary<uint,uint>? _retainerTaskToRetainerNormalLookup;
        private Dictionary<uint,uint>? _itemToSpearFishingLookup;

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

        public ConcurrentDictionary<uint, RecipeEx> RecipeCache
        {
            get => _recipeCache ??= new ConcurrentDictionary<uint, RecipeEx>();
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
        public Dictionary<uint, uint> HWDCrafterSupplyByItemIdLookup { get; set; }
        public List<DungeonBoss>? DungeonBosses { get; set; }
        public List<DungeonBossDrop>? DungeonBossDrops { get; set; }
        public List<DungeonBossChest>? DungeonBossChests { get; set; }
        public List<ItemSupplement>? ItemSupplements { get; set; }
        public List<SubmarineDrop>? SubmarineDrops { get; set; }
        public List<AirshipDrop>? AirshipDrops { get; set; }
        public List<DungeonChestItem>? DungeonChestItems { get; set; }
        public List<DungeonDrop>? DungeonDrops { get; set; }
        public List<DungeonChest>? DungeonChests { get; set; }
        public List<MobSpawnPositionEx>? MobSpawns { get; set; }
        public List<ENpcPlaceEx>? ENpcPlaces { get; set; }
        public List<ENpcShop>? ENpcShops { get; set; }
        public List<ShopName>? ShopNames { get; set; }
        public List<AirshipUnlockEx>? AirshipUnlocks { get; set; }
        public List<SubmarineUnlockEx>? SubmarineUnlocks { get; set; }
        public List<ItemPatch>? ItemPatches { get; set; }
        public List<RetainerVentureItemEx>? RetainerVentureItems { get; set; }
        public List<StoreItem>? StoreItems { get; set; }
        public List<MobDropEx>? MobDrops { get; set; }
        public List<HouseVendor>? HouseVendors { get; set; }
        public List<FateItem>? FateItems { get; set; }

        private Dictionary<uint, List<ItemSupplement>>? _sourceSupplements;
        private Dictionary<uint, List<ItemSupplement>>? _useSupplements;
        private Dictionary<uint, HouseVendor>? _houseVendors;
        public HouseVendor? GetHouseVendor(uint eNpcResidentId)
        {
            if (HouseVendors == null)
            {
                return null;
            }
            if (_houseVendors == null)
            {
                _houseVendors = HouseVendors.ToDictionary(c => c.ENpcResidentId, c => c);
            }

            if (_houseVendors.ContainsKey(eNpcResidentId))
            {
                return _houseVendors[eNpcResidentId];
            }

            return null;
        }
        public List<ItemSupplement>? GetSupplementSources(uint sourceItemId)
        {
            if (ItemSupplements == null)
            {
                return null;
            }
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
            if (ItemSupplements == null)
            {
                return null;
            }
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
            if (SubmarineDrops == null)
            {
                return null;
            }
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
            if (AirshipDrops == null)
            {
                return null;
            }
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
            if (DungeonChestItems == null)
            {
                return null;
            }
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
            if (DungeonChests == null)
            {
                return null;
            }
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
            if (DungeonBosses == null)
            {
                return null;
            }
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

        private decimal currentPatch = new decimal(7.05); 
        private Dictionary<uint, decimal>? _itemPatches;

        public decimal GetItemPatch(uint itemId)
        {
            if (ItemPatches == null)
            {
                return 0;
            }
            if (_itemPatches == null)
            {
                _itemPatches = ItemPatch.ToItemLookup(ItemPatches);
            }

            return _itemPatches.ContainsKey(itemId) ? _itemPatches[itemId] : currentPatch;
        }

        private Dictionary<uint, List<DungeonBossDrop>>? _dungeonBossDrops;
        public List<DungeonBossDrop>? GetDungeonBossDrops(uint itemId)
        {
            if (DungeonBossDrops == null)
            {
                return null;
            }
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
            if (DungeonBossChests == null)
            {
                return null;
            }
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
            if (DungeonDrops == null)
            {
                return null;
            }
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

        private Dictionary<uint, List<MobDropEx>>? _mobDrops;
        public List<MobDropEx>? GetMobDrops(uint itemId)
        {
            if (MobDrops == null)
            {
                return null;
            }
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
        private Dictionary<uint, List<MobDropEx>>? _mobDropsByBNpcNameId;
        public List<MobDropEx>? GetMobDropsByBNpcNameId(uint bNpcNameId)
        {
            if (MobDrops == null)
            {
                return null;
            }
            if (_mobDropsByBNpcNameId == null)
            {
                _mobDropsByBNpcNameId = MobDrops.GroupBy(c => c.BNpcNameId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_mobDropsByBNpcNameId.ContainsKey(bNpcNameId))
            {
                return _mobDropsByBNpcNameId[bNpcNameId];
            }

            return null;
        }

        public bool HasMobDrops(uint itemId)
        {
            if (MobDrops == null)
            {
                return false;
            }
            if (_mobDrops == null)
            {
                _mobDrops = MobDrops.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            return _mobDrops.ContainsKey(itemId);
        }

        private Dictionary<uint, List<MobSpawnPositionEx>>? _mobSpawns;
        public List<MobSpawnPositionEx>? GetMobSpawns(uint bNpcNameId)
        {
            if (MobSpawns == null)
            {
                return null;
            }
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

        private Dictionary<uint, List<StoreItem>>? _itemStoreItems;
        public List<StoreItem>? GetItemsByStoreItem(uint itemId)
        {
            if (StoreItems == null)
            {
                return null;
            }
            if (_itemStoreItems == null)
            {
                _itemStoreItems = StoreItems.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_itemStoreItems.ContainsKey(itemId))
            {
                return _itemStoreItems[itemId];
            }

            return null;
        }

        private Dictionary<uint, List<FateItem>>? _fateItems;
        public List<FateItem>? GetFateItems(uint itemId)
        {
            if (FateItems == null)
            {
                return null;
            }
            _fateItems ??= FateItems.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());

            return _fateItems.GetValueOrDefault(itemId);
        }
        
        public Dictionary<uint, string>? _itemNamesById;
        
        public Dictionary<uint, string> ItemNamesById
        {
            get
            {
                if (_itemNamesById == null)
                {
                    _itemNamesById = Service.ExcelCache.GetItemExSheet().ToDictionary(c => c.RowId, c => c.NameString);
                }
                return _itemNamesById;
            }
            set => _itemNamesById = value;
        }
        
        public Dictionary<uint, string>? _territoryNamesById;
        
        public Dictionary<uint, string> TerritoryNamesById
        {
            get
            {
                if (_territoryNamesById == null)
                {
                    _territoryNamesById = Service.ExcelCache.GetTerritoryTypeExSheet().Where(c => c.TerritoryIntendedUse is 0 or 1 or 13 or 14).ToDictionary(c => c.RowId, c => c.FormattedName);
                }
                return _territoryNamesById;
            }
            set => _territoryNamesById = value;
        }
        
        public Dictionary<string, uint>? _itemsByName;
        
        public Dictionary<string, uint> ItemsByName
        {
            get
            {
                if (_itemsByName == null)
                {
                    _itemsByName = Service.ExcelCache.GetItemExSheet().DistinctBy(c => c.NameString).ToDictionary(c => c.NameString, c => c.RowId);
                }
                return _itemsByName;
            }
            set => _itemsByName = value;
        }


        private Dictionary<uint, List<ENpcPlaceEx>>? _eNpcPlaces;
        public List<ENpcPlaceEx>? GetENpcPlaces(uint eNpcResidentId)
        {
            if (ENpcPlaces == null)
            {
                return null;
            }
            if (_eNpcPlaces == null)
            {
                _eNpcPlaces = ENpcPlaces.GroupBy(c => c.ENpcResidentId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_eNpcPlaces.ContainsKey(eNpcResidentId))
            {
                return _eNpcPlaces[eNpcResidentId];
            }

            return null;
        }

        private Dictionary<uint, ShopName>? _shopNames;
        public ShopName? GetShopName(uint shopId)
        {
            if (ShopNames == null)
            {
                return null;
            }
            if (_shopNames == null)
            {
                _shopNames = ShopNames.ToDictionary(c => c.ShopId, c => c);
            }

            if (_shopNames.ContainsKey(shopId))
            {
                return _shopNames[shopId];
            }

            return null;
        }

        private Dictionary<uint, List<ENpcShop>>? _eNpcShops;
        public List<ENpcShop>? GetENpcShops(uint eNpcId)
        {
            if (ENpcShops == null)
            {
                return null;
            }
            if (_eNpcShops == null)
            {
                _eNpcShops = ENpcShops.GroupBy(c => c.ENpcResidentId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_eNpcShops.ContainsKey(eNpcId))
            {
                return _eNpcShops[eNpcId];
            }

            return null;
        }

        private Dictionary<uint, AirshipUnlockEx>? _airshipUnlocks;
        public AirshipUnlockEx? GetAirshipUnlock(uint airshipPointId)
        {
            if (AirshipUnlocks == null)
            {
                return null;
            }
            if (_airshipUnlocks == null)
            {
                _airshipUnlocks = AirshipUnlocks.ToDictionary(c => c.AirshipExplorationPointId, c => c);
            }

            if (_airshipUnlocks.ContainsKey(airshipPointId))
            {
                return _airshipUnlocks[airshipPointId];
            }

            return null;
        }

        private Dictionary<uint, SubmarineUnlockEx>? _submarineUnlocks;
        public SubmarineUnlockEx? GetSubmarineUnlock(uint submarinePointId)
        {
            if (SubmarineUnlocks == null)
            {
                return null;
            }
            if (_submarineUnlocks == null)
            {
                _submarineUnlocks = SubmarineUnlocks.ToDictionary(c => c.SubmarineExplorationId, c => c);
            }

            if (_submarineUnlocks.ContainsKey(submarinePointId))
            {
                return _submarineUnlocks[submarinePointId];
            }

            return null;
        }

        private Dictionary<uint, List<RetainerVentureItemEx>>? _retainerVentureItems;
        public List<RetainerVentureItemEx>? GetRetainerVentureItems(uint retainerTaskRandomId)
        {
            if (RetainerVentureItems == null)
            {
                return null;
            }
            if (_retainerVentureItems == null)
            {
                _retainerVentureItems = RetainerVentureItems.GroupBy(c => c.RetainerTaskRandomId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_retainerVentureItems.ContainsKey(retainerTaskRandomId))
            {
                return _retainerVentureItems[retainerTaskRandomId];
            }

            return null;
        }

        private Dictionary<uint, List<RetainerVentureItemEx>>? _itemRetainerVentures;
        public List<RetainerVentureItemEx>? GetItemRetainerVentures(uint itemId)
        {
            if (RetainerVentureItems == null)
            {
                return null;
            }
            if (_itemRetainerVentures == null)
            {
                _itemRetainerVentures = RetainerVentureItems.GroupBy(c => c.ItemId, c => c).ToDictionary(c => c.Key, c => c.ToList());
            }

            if (_itemRetainerVentures.ContainsKey(itemId))
            {
                return _itemRetainerVentures[itemId];
            }

            return null;
        }

        private bool _notoriousMonstersLookedUp = false;
        private Dictionary<uint, NotoriousMonster> _notoriousMonsters;
        public NotoriousMonster? GetNotoriousMonster(uint bNpcNameId)
        {
            if (!_notoriousMonstersLookedUp)
            {
                _notoriousMonsters = GetNotoriousMonsterSheet().Where(c => c.RowId != 0).DistinctBy(c => c.BNpcName.Row).ToDictionary(c => c.BNpcName.Row, c => c);
            }

            if (_notoriousMonsters.ContainsKey(bNpcNameId))
            {
                return _notoriousMonsters[bNpcNameId];
            }

            return null;
        }

        private Dictionary<uint, List<RetainerTaskEx>>? _itemRetainerTasks;
        private Dictionary<uint, HashSet<RetainerTaskType>>? _itemRetainerTypes;

        private void GenerateItemRetainerTaskLookup()
        {
            Dictionary<uint, List<RetainerTaskEx>> itemRetainerTasks = new();
            Dictionary<uint, HashSet<RetainerTaskType>> itemRetainerTypes = new();
            foreach (var item in GetRetainerTaskExSheet())
            {
                foreach (var drop in item.Drops)
                {
                    itemRetainerTasks.TryAdd(drop.RowId, new List<RetainerTaskEx>());
                    itemRetainerTasks[drop.RowId].Add(item);
                    itemRetainerTypes.TryAdd(drop.RowId, new HashSet<RetainerTaskType>());
                    itemRetainerTypes[drop.RowId].Add(item.RetainerTaskType);
                }
            }

            _itemRetainerTasks = itemRetainerTasks;
            _itemRetainerTypes = itemRetainerTypes;
        }
        public List<RetainerTaskEx>? GetItemRetainerTasks(uint itemId)
        {
            if (_itemRetainerTasks == null)
            {
                GenerateItemRetainerTaskLookup();
            }

            if (_itemRetainerTasks!.ContainsKey(itemId))
            {
                return _itemRetainerTasks[itemId];
            }

            return null;
        }
        public HashSet<RetainerTaskType>? GetItemRetainerTaskTypes(uint itemId)
        {
            if (_itemRetainerTypes == null)
            {
                GenerateItemRetainerTaskLookup();
            }

            if (_itemRetainerTypes!.ContainsKey(itemId))
            {
                return _itemRetainerTypes[itemId];
            }

            return null;
        }

        public ConcurrentDictionary<uint, List<RetainerTaskNormalEx>> ItemToRetainerTaskNormalLookup
        {
            get
            {
                return _itemToRetainerTaskNormalLookup ??= new ConcurrentDictionary<uint, List<RetainerTaskNormalEx>>(GetSheet<RetainerTaskNormalEx>()
                    .GroupBy(c => c.Item.Row)
                    .ToDictionary(c => c.Key, c => c.ToList()));
            }
        }

        /*
         * This provides a lookup between a retainer task and RetainerTaskNormal and RetainerTaskRandom
         */
        public Dictionary<uint,uint> RetainerTaskToRetainerNormalLookup
        {
            get
            {
                return _retainerTaskToRetainerNormalLookup ??= GetSheet<RetainerTask>().ToSingleLookup(c => c.Task, c => c.RowId);
            }
        }
        
        public Dictionary<uint,uint> ItemToSpearfishingItemLookup
        {
            get
            {
                return _itemToSpearFishingLookup ??= GetSheet<SpearfishingItem>().ToSingleLookup(c => c.Item.Row, c => c.RowId);
            }
        }

        public ENpcCollection? ENpcCollection => _loadNpcs ? _eNpcCollection ??= new ENpcCollection() : null;
        public ShopCollection? ShopCollection => _loadShops ? _shopCollection ??= new ShopCollection() : null;

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

        public int ItemRecipeCount(uint rowId)
        {
            if (!_recipeLookUpCalculated) CalculateRecipeLookup();
            if (!CraftLookupTable.ContainsKey(rowId)) return 0;

            return CraftLookupTable[rowId].Count;
        }

        public bool IsArmoireItem(uint rowId)
        {
            if (!_armoireLoaded) CalculateArmoireItems();

            return _armoireItems.Contains(rowId);
        }

        public HashSet<uint> GetArmoireItems()
        {
            if (!_armoireLoaded) CalculateArmoireItems();

            return _armoireItems;
        }
        
        public GameData GameData => _dataManager == null ? _gameData! : _dataManager.GameData;

        public Language Language => GameData.Options.DefaultExcelLanguage;

        /// <summary>
        /// Creates a new instance of ExcelCache
        /// </summary>
        /// <param name="dataManager">An instance of Dalamuds DataManager</param>
        /// <param name="logger"></param>
        /// <param name="loadCsvs">Should LuminaSupplemental CSV sheets be loaded by default?</param>
        /// <param name="loadNpcs">Should NPC locations be calculated on boot(these cannot be loaded later as of yet)</param>
        /// <param name="loadShops">Should shop locations be calculated on boot(these cannot be loaded later as of yet)</param>
        public ExcelCache(IDataManager dataManager, ILogger<ExcelCache> logger, bool loadCsvs = true, bool loadNpcs = true, bool loadShops = true)
        {
            logger.LogDebug("Creating {type} ({this})", GetType().Name, this);
            Service.ExcelCache = this;
            _eventItemCache = new Dictionary<uint, EventItem>();
            _itemUiCategory = new Dictionary<uint, ItemUICategory>();
            _itemSearchCategory = new Dictionary<uint, ItemSearchCategory>();
            _itemSortCategory = new Dictionary<uint, ItemSortCategory>();
            _equipSlotCategories = new Dictionary<uint, EquipSlotCategory>();
            _equipRaceCategories = new Dictionary<uint, EquipRaceCategory>();
            _classJobCategoryLookup = new Dictionary<uint, HashSet<uint>>();
            _armoireItems = new HashSet<uint>();
            flattenedRecipes = new Dictionary<uint, Dictionary<uint, uint>>();
            RecipeLookupTable = new Dictionary<uint, HashSet<uint>>();
            CraftLookupTable = new Dictionary<uint, HashSet<uint>>();
            AddonNames = new Dictionary<uint, string>();
            CraftLevesItemLookup = new Dictionary<uint, uint>();
            CompanyCraftSequenceByResultItemIdLookup = new Dictionary<uint, uint>();
            ItemToSatisfactionSupplyLookup = new Dictionary<uint, uint>();
            SpecialShopToFateShopLookup = new Dictionary<uint, uint>();
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
            RecipeCache = new ConcurrentDictionary<uint, RecipeEx>();
            ClassJobCategoryLookup = new Dictionary<uint, HashSet<uint>>();
            CraftLevesItemLookup = new Dictionary<uint, uint>();
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _recipeLookUpCalculated = false;
            _craftLevesItemLookupCalculated = false;
            _armoireLoaded = false;
            
            _dataManager = dataManager;
            _logger = logger;
            Service.ExcelCache = this;
            _loadNpcs = loadNpcs;
            _loadShops = loadShops;
        }

        private bool _loadShops;
        private bool _loadNpcs;
        
        
        private void LoadCsvs()
        {
            DungeonBosses = LoadCsv<DungeonBoss>(CsvLoader.DungeonBossResourceName, "Dungeon Boss");
            DungeonBossChests = LoadCsv<DungeonBossChest>(CsvLoader.DungeonBossChestResourceName, "Dungeon Boss Chests");
            DungeonBossDrops = LoadCsv<DungeonBossDrop>(CsvLoader.DungeonBossDropResourceName, "Dungeon Boss Drops");
            DungeonChestItems = LoadCsv<DungeonChestItem>(CsvLoader.DungeonChestItemResourceName, "Dungeon Chest Items");
            DungeonChests = LoadCsv<DungeonChest>(CsvLoader.DungeonChestResourceName, "Dungeon Chests");
            DungeonDrops = LoadCsv<DungeonDrop>(CsvLoader.DungeonDropItemResourceName, "Dungeon Chest Items");
            ItemSupplements = LoadCsv<ItemSupplement>(CsvLoader.ItemSupplementResourceName, "Item Supplement");
            MobDrops = LoadCsv<MobDropEx>(CsvLoader.MobDropResourceName, "Mob Drops");
            SubmarineDrops = LoadCsv<SubmarineDrop>(CsvLoader.SubmarineDropResourceName, "Submarine Drops");
            AirshipDrops = LoadCsv<AirshipDrop>(CsvLoader.AirshipDropResourceName, "Airship Drops");
            MobSpawns = LoadCsv<MobSpawnPositionEx>(CsvLoader.MobSpawnResourceName, "Mob Spawns");
            ENpcPlaces = LoadCsv<ENpcPlaceEx>(CsvLoader.ENpcPlaceResourceName, "ENpc Places");
            ENpcShops = LoadCsv<ENpcShop>(CsvLoader.ENpcShopResourceName, "ENpc Shops");
            ShopNames = LoadCsv<ShopName>(CsvLoader.ShopNameResourceName, "Shop Names");
            AirshipUnlocks = LoadCsv<AirshipUnlockEx>(CsvLoader.AirshipUnlockResourceName, "Airship Unlocks");
            SubmarineUnlocks = LoadCsv<SubmarineUnlockEx>(CsvLoader.SubmarineUnlockResourceName, "Submarine Unlocks");
            ItemPatches = LoadCsv<ItemPatch>(CsvLoader.ItemPatchResourceName, "Item Patches");
            RetainerVentureItems = LoadCsv<RetainerVentureItemEx>(CsvLoader.RetainerVentureItemResourceName, "Retainer Ventures");
            StoreItems = LoadCsv<StoreItem>(CsvLoader.StoreItemResourceName, "SQ Store Items");
            HouseVendors = LoadCsv<HouseVendor>(CsvLoader.HouseVendorResourceName, "House Vendors");
            FateItems = LoadCsv<FateItem>(CsvLoader.FateItemResourceName, "Fate Items");
        }

        private List<T> LoadCsv<T>(string resourceName, string title) where T : ICsv, new()
        {
            try
            {
                var lines = CsvLoader.LoadResource<T>(resourceName, out var failedLines, GameData, GameData.Options.DefaultExcelLanguage);
                if (failedLines.Count != 0)
                {
                    foreach (var failedLine in failedLines)
                    {
                        Service.Log.Error("Failed to load line from " + title + ": " + failedLine);
                    }
                }
                return lines;
            }
            catch (Exception e)
            {
                Service.Log.Error("Failed to load " + title);
                Service.Log.Error(e.Message);
            }

            return new List<T>();
        }

        public void PreCacheItemData()
        {
            foreach (var item in AllItems)
            {
                var sources = item.Value.Sources;
                var uses = item.Value.Uses;
            }

        }
        
        public bool FinishedLoading { get; private set; }
        
        public int CabinetSize { get; private set; }
        public int GlamourChestSize { get; private set; } = 800;

        public void CalculateLookups(bool loadNpcs, bool loadShops)
        {
            CabinetSize = GetCabinetSheet().Count();
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
            GatheringItemToGatheringItemPoint =
                GetSheet<GatheringItemPoint>().ToColumnLookup(c => c.RowId, c => c.GatheringPoint.Row);
            GatheringItemPointToGatheringPointBase =
                GetSheet<GatheringPoint>().ToColumnLookup(c => c.RowId, c => c.GatheringPointBase.Row);
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
            CompanyCraftSequenceByResultItemIdLookup = GetSheet<CompanyCraftSequence>().ToSingleLookup(c => c.ResultItem.Row, c => c.RowId);
            ItemToSatisfactionSupplyLookup = GetSheet<SatisfactionSupply>().ToSingleLookup(c => c.Item.Row, c => c.RowId);
            SpecialShopToFateShopLookup = GetSheet<FateShop>().ToSingleLookup(c => c.SpecialShop.Select(d => d.Row), c => c.RowId);
            ItemIdToCompanyCraftSequenceLookup = GetSheet<CompanyCraftSequence>().ToSingleLookup(c => c.RowId, c => c.ResultItem.Row);
            ItemToAquariumFish = GetSheet<AquariumFish>().ToSingleLookup(c => c.Item.Row, c => c.RowId);
            ItemToDailySupplyItem = GetSheet<DailySupplyItem>().SelectMany(c => c.UnkData0.Select(i => (c.RowId,i.Item))).Where(c => c.Item != 0).Distinct().ToDictionary(c => (uint)c.Item, c => c.RowId);
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
                            specialShopItemCostRewardLookup[item.ItemEx.Row].Add(((uint)rewardItem.Count, rewardItem.ItemEx.Row));
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

            if (loadNpcs || loadShops)
            {
                _eNpcCollection = new ENpcCollection();
            }

            if (loadShops)
            {
                _shopCollection = new ShopCollection();
            }

            _allItems ??= GetItemExSheet().ToCache();
            FinishedLoading = true;
        }



        public void Destroy()
        {
            _itemUiCategoriesFullyLoaded = false;
            _gatheringItemLinksCalculated = false;
            _gatheringItemPointLinksCalculated = false;
            _itemUiSearchFullyLoaded = false;
            _recipeLookUpCalculated = false;
            _classJobCategoryLookupCalculated = false;
            _craftLevesItemLookupCalculated = false;
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

        public HashSet<Type> LoadedTypes = new HashSet<Type>(); 
        public ExcelSheet<T> GetSheet<T>() where T : ExcelRow
        {
            if (!LoadedTypes.Contains(typeof(T)))
            {
                LoadedTypes.Add(typeof(T));
            }
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

        public RecipeEx? GetRecipe(uint recipeId)
        {
            if (!RecipeCache.ContainsKey(recipeId))
            {
                var recipe = GetRecipeExSheet().GetRow(recipeId);
                if (recipe == null) return null;

                RecipeCache[recipeId] = recipe;
                return recipe;
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

        public bool IsIshgardCraft(uint itemId)
        {
            if (itemId == 0) return false;
            if (!_hwdCrafterSupplyLookupCalculated) CalculateHwdCrafterSupplyLookup();
            return HWDCrafterSupplyByItemIdLookup.ContainsKey(itemId);
        }

        public uint? GetHWDCrafterSupplyId(uint itemId)
        {
            if (itemId == 0) return null;
            if (!_hwdCrafterSupplyLookupCalculated) CalculateHwdCrafterSupplyLookup();
            return HWDCrafterSupplyByItemIdLookup.ContainsKey(itemId) ? HWDCrafterSupplyByItemIdLookup[itemId] : null;
        }

        public void CalculateHwdCrafterSupplyLookup()
        {
            Dictionary<uint, uint> crafterSupplyLookup = new Dictionary<uint, uint>();
            var crafterSupplies = GetSheet<HWDCrafterSupply>();
            foreach (var crafterSupply in crafterSupplies)
            {
                foreach (var item in crafterSupply.ItemTradeIn)
                {
                    crafterSupplyLookup.TryAdd(item.Row, crafterSupply.RowId);
                }
            }

            HWDCrafterSupplyByItemIdLookup = crafterSupplyLookup;

            _hwdCrafterSupplyLookupCalculated = true;
        }

        public bool IsCompanyCraft(uint itemId)
        {
            if (itemId == 0) return false;

            return CompanyCraftSequenceByResultItemIdLookup.ContainsKey(itemId);
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

        private Dictionary<(uint, sbyte), uint>? _mapIdByTerritoryTypeAndMapIndex;
        public uint GetMapIdByTerritoryTypeAndMapIndex(uint territoryTypeId, sbyte mapIndex)
        {
            if (_mapIdByTerritoryTypeAndMapIndex == null)
            {
                _mapIdByTerritoryTypeAndMapIndex = new Dictionary<(uint, sbyte), uint>();
                foreach (var map in GetMapSheet())
                {
                    _mapIdByTerritoryTypeAndMapIndex.TryAdd((map.TerritoryType.Row, map.MapIndex), map.RowId);
                }
            }

            var mapKey = (territoryTypeId, mapIndex);
            if (_mapIdByTerritoryTypeAndMapIndex.ContainsKey(mapKey))
            {
                return _mapIdByTerritoryTypeAndMapIndex[mapKey];
            }

            return 0;
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
        
        public readonly uint[] HiddenNodes = 
        {
            7758,  // Grade 1 La Noscean Topsoil
            7761,  // Grade 1 Shroud Topsoil   
            7764,  // Grade 1 Thanalan Topsoil 
            7759,  // Grade 2 La Noscean Topsoil
            7762,  // Grade 2 Shroud Topsoil   
            7765,  // Grade 2 Thanalan Topsoil 
            10092, // Black Limestone          
            10094, // Little Worm              
            10097, // Yafaemi Wildgrass        
            12893, // Dark Chestnut            
            15865,  // Firelight Seeds          
            15866,  // Icelight Seeds           
            15867,  // Windlight Seeds          
            15868,  // Earthlight Seeds         
            15869,  // Levinlight Seeds         
            15870,  // Waterlight Seeds
            12534, // Mythrite Ore             
            12535, // Hardsilver Ore           
            12537, // Titanium Ore             
            12579, // Birch Log                
            12878, // Cyclops Onion            
            12879, // Emerald Beans            
        };

        public bool IsItemAvailableAtHiddenNode(uint itemId)
        {
            return HiddenNodes.Contains(itemId);
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

        public bool IsItemAvailableAtEphemeralNode(uint itemId)
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
                        return gatheringPointTransient.EphemeralStartTime < 65535 && (gatheringPointTransient.EphemeralStartTime != 0 || gatheringPointTransient.EphemeralEndTime != 0);
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
            if (!_disposed && disposing)
            {
                _logger.LogDebug("Disposing {type} ({this})", GetType().Name, this);
                var methodInfo = typeof(ExcelModule).GetMethod("RemoveSheetFromCache", new Type[] { typeof(string) });
                if (methodInfo != null)
                {
                    _logger.LogDebug("Clearing {count} sheets from {type} ({this})", LoadedTypes.Count,GetType().Name, this);
                    foreach (var type in LoadedTypes)
                    {
                        if (type.Namespace != null && type.Namespace == "CriticalCommonLib.Sheets")
                        {
                            var excel = GameData.Excel;
                            
                            var genericMethod = methodInfo.MakeGenericMethod(type);
                            var reformattedName = type.Name.ToLower().Replace("ex", "");
                            genericMethod.Invoke(excel, new object[] { reformattedName });
                        }
                    }
                }
            }

            _shopCollection = null;
            _eNpcCollection = null;
            _gameData = null;
            _dataManager = null;
            _disposed = true;         
        }
        
        ~ExcelCache()
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

        public ExcelSheet<ENpcBaseEx> GetENpcBaseExSheet()
        {
            return _enpcBaseExSheet ??= GetSheet<ENpcBaseEx>();
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

        public ExcelSheet<ENpcResidentEx> GetENpcResidentExSheet()
        {
            return _enpcResidentExSheet ??= GetSheet<ENpcResidentEx>();
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

        public ExcelSheet<ClassJobEx> GetClassJobSheet()
        {
            return _classJobSheet ??= GetSheet<ClassJobEx>();
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
        
        public ExcelSheet<AirshipExplorationPointEx> GetAirshipExplorationPointExSheet()
        {
            return _airshipExplorationPointSheet ??= GetSheet<AirshipExplorationPointEx>();
        }
        
        public ExcelSheet<SubmarineExplorationEx> GetSubmarineExplorationExSheet()
        {
            return _submarineExplorationSheetEx ??= GetSheet<SubmarineExplorationEx>();
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

        public ExcelSheet<RetainerTaskEx> GetRetainerTaskExSheet()
        {
            return _retainerTaskExSheet ??= GetSheet<RetainerTaskEx>();
        }
        
        public ExcelSheet<CompanyCraftSequenceEx> GetCompanyCraftSequenceSheet()
        {
            return _companyCraftSequenceSheet ??= GetSheet<CompanyCraftSequenceEx>();
        }

        public ExcelSheet<GatheringItem> GetGatheringItemSheet()
        {
            return _gatheringItemSheet ??= GetSheet<GatheringItem>();
        }

        public ExcelSheet<GatheringItemPoint> GetGatheringItemPointSheet()
        {
            return _gatheringItemPointSheet ??= GetSheet<GatheringItemPoint>();
        }

        public ExcelSheet<LevelEx> GetLevelExSheet()
        {
            return _levelExSheet ??= GetSheet<LevelEx>();
        }
        
        public ExcelSheet<GatheringPointTransient> GetGatheringPointTransientSheet()
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
        
        public ExcelSheet<MapEx> GetMapSheet()
        {
            return _mapSheet ??= GetSheet<MapEx>();
        }
        
        public ExcelSheet<MapMarker> GetMapMarkerSheet()
        {
            return _mapMarkerSheet ??= GetSheet<MapMarker>();
        }
        
        public ExcelSheet<Aetheryte> GetAetheryteSheet()
        {
            return _aetheryteSheet ??= GetSheet<Aetheryte>();
        }
        
        public ExcelSheet<Stain> GetStainSheet()
        {
            return _stainSheet ??= GetSheet<Stain>();
        }
        
        public ExcelSheet<WorldEx> GetWorldSheet()
        {
            return _worldSheet ??= GetSheet<WorldEx>();
        }
        
        public ExcelSheet<ContentFinderConditionEx> GetContentFinderConditionExSheet()
        {
            return _contentFinderConditionExSheet ??= GetSheet<ContentFinderConditionEx>();
        }
        
        public ExcelSheet<Fate> GetFateSheet()
        {
            return _fateSheet ??= GetSheet<Fate>();
        }
        
        public ExcelSheet<ContentType> GetContentTypeSheet()
        {
            return _contentTypeSheet ??= GetSheet<ContentType>();
        }
        
        public ExcelSheet<SubmarineMap> GetSubmarineMapSheet()
        {
            return _submarineMapSheet ??= GetSheet<SubmarineMap>();
        }
        
        public ExcelSheet<ContentRoulette> GetContentRouletteSheet()
        {
            return _contentRouletteSheet ??= GetSheet<ContentRoulette>();
        }
        
        public ExcelSheet<HWDCrafterSupplyEx> GetHWDCrafterSupplySheet()
        {
            return _hwdCrafterSupplySheet ??= GetSheet<HWDCrafterSupplyEx>();
        }
        
        public ExcelSheet<GCScripShopItemEx> GetGCScripShopItemSheet()
        {
            return _gcScripShopItemSheet ??= GetSheet<GCScripShopItemEx>();
        }
        
        public ExcelSheet<CraftTypeEx> GetCraftTypeSheet()
        {
            return _craftTypeSheet ??= GetSheet<CraftTypeEx>();
        }

        public ExcelSheet<NotoriousMonster> GetNotoriousMonsterSheet()
        {
            return _notoriousMonsterSheet ??= GetSheet<NotoriousMonster>();
        }

        public ExcelSheet<FateShop> GetFateShopSheet()
        {
            return _fateShopSheet ??= GetSheet<FateShop>();
        }

        private ExcelSheet<ItemEx>? _itemExSheet;
        private ExcelSheet<CabinetCategory>? _cabinetCategorySheet;
        private ExcelSheet<RecipeEx>? _recipeExSheet;
        private ExcelSheet<EquipSlotCategory>? _equipSlotCategorySheet;
        private ExcelSheet<ItemSortCategory>? _itemSortCategorySheet;
        private ExcelSheet<ENpcBaseEx>? _enpcBaseExSheet;
        private ExcelSheet<BNpcNameEx>? _bNpcNameSheet;
        private ExcelSheet<BNpcBaseEx>? _bNpcBaseSheet;
        private ExcelSheet<PlaceName>? _placeNameSheet;
        private ExcelSheet<ItemSearchCategory>? _itemSearchCategorySheet;
        private ExcelSheet<ItemUICategory>? _itemUiCategorySheet;
        private ExcelSheet<ClassJobEx>? _classJobSheet;
        private ExcelSheet<CompanyCraftSupplyItemEx>? _companyCraftSupplyItemSheetEx;
        private ExcelSheet<SpecialShopEx>? _specialShopExSheet;
        private ExcelSheet<GCShopEx>? _gcShopExSheet;
        private ExcelSheet<CustomTalk>? _customTalkSheet;
        private ExcelSheet<GilShopEx>? _gilShopExSheet;
        private ExcelSheet<TerritoryTypeEx>? _territoryTypeExSheet;
        private ExcelSheet<ENpcResidentEx>? _enpcResidentExSheet;
        private ExcelSheet<PreHandler>? _preHandlerSheet;
        private ExcelSheet<LevelEx>? _levelExSheet;
        private ExcelSheet<GatheringItemPoint>? _gatheringItemPointSheet;
        private ExcelSheet<GatheringItem>? _gatheringItemSheet;
        private ExcelSheet<CompanyCraftSequenceEx>? _companyCraftSequenceSheet;
        private ExcelSheet<RetainerTaskNormalEx>? _retainerTaskNormalExSheet;
        private ExcelSheet<RetainerTaskEx>? _retainerTaskExSheet;
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
        private ExcelSheet<MapEx>? _mapSheet;
        private ExcelSheet<MapMarker>? _mapMarkerSheet;
        private ExcelSheet<Aetheryte>? _aetheryteSheet;
        private ExcelSheet<Stain>? _stainSheet;
        private ExcelSheet<WorldEx>? _worldSheet;
        private ExcelSheet<ContentFinderConditionEx>? _contentFinderConditionExSheet;
        private ExcelSheet<Fate>? _fateSheet;
        private ExcelSheet<ContentType>? _contentTypeSheet;
        private ExcelSheet<SubmarineMap>? _submarineMapSheet;
        private ExcelSheet<ContentRoulette>? _contentRouletteSheet;
        private ExcelSheet<GCScripShopItemEx>? _gcScripShopItemSheet;
        private ExcelSheet<AirshipExplorationPointEx>? _airshipExplorationPointSheet;
        private ExcelSheet<SubmarineExplorationEx>? _submarineExplorationSheetEx;
        private ExcelSheet<HWDCrafterSupplyEx>? _hwdCrafterSupplySheet;
        private ExcelSheet<CraftTypeEx>? _craftTypeSheet;
        private ExcelSheet<NotoriousMonster>? _notoriousMonsterSheet;
        private ExcelSheet<FateShop>? _fateShopSheet;
        private Dictionary<uint, uint>? _itemToCabinetCategory;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Started service {type} ({this})", GetType().Name, this);
            LoadCsvs();
            CalculateLookups(true, true);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Stopped service {type} ({this})", GetType().Name, this);
            return Task.CompletedTask;
        }
    }
}