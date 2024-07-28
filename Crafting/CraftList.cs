using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Sheets;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Common.Math;
using Newtonsoft.Json;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Crafting
{
    public class CraftList
    {
        private List<CraftItem>? _craftItems = new();

        [JsonIgnore] public bool BeenUpdated;
        [JsonIgnore] public bool BeenGenerated;
        [JsonIgnore] public bool NeedsRefresh { get; set; }
        [JsonIgnore] public uint MinimumNQCost = 0;
        [JsonIgnore] public uint MinimumHQCost = 0;

        private List<(IngredientPreferenceType,uint?)>? _ingredientPreferenceTypeOrder;
        private List<uint>? _zonePreferenceOrder;
        private Dictionary<uint, IngredientPreference>? _ingredientPreferences = new Dictionary<uint, IngredientPreference>();
        private Dictionary<uint, bool>? _hqRequired;
        private Dictionary<uint, CraftRetainerRetrieval>? _craftRetainerRetrievals;
        private Dictionary<uint, uint>? _craftRecipePreferences;
        private Dictionary<uint, uint>? _zoneItemPreferences;
        private Dictionary<uint, uint>? _zoneBuyPreferences;
        private Dictionary<uint, uint>? _zoneMobPreferences;
        private Dictionary<uint, uint>? _zoneBotanyPreferences;
        private Dictionary<uint, uint>? _zoneMiningPreferences;
        private Dictionary<uint, uint>? _marketItemWorldPreference;
        private Dictionary<uint, uint>? _marketItemPriceOverride;
        private List<uint>? _worldPricePreference;

        public bool IsCompleted
        {
            get
            {
                return CraftItems.All(c => c.IsCompleted);
            }
        }

        public bool HideComplete
        {
            get => _hideComplete;
            set
            {
                _hideComplete = value;
                ClearGroupCache();
            }
        }

        [JsonProperty]
        public RetainerRetrieveOrder RetainerRetrieveOrder { get; set; } = RetainerRetrieveOrder.RetrieveFirst;
        [JsonProperty]
        public CraftRetainerRetrieval CraftRetainerRetrieval { get; set; } = CraftRetainerRetrieval.Yes;
        [JsonProperty]
        public CraftRetainerRetrieval CraftRetainerRetrievalOutput { get; set; } = CraftRetainerRetrieval.No;
        [JsonProperty]
        public CurrencyGroupSetting CurrencyGroupSetting { get; private set; } = CurrencyGroupSetting.Separate;
        [JsonProperty]
        public CrystalGroupSetting CrystalGroupSetting { get; private set; } = CrystalGroupSetting.Separate;
        [JsonProperty]
        public PrecraftGroupSetting PrecraftGroupSetting { get; private set; } = PrecraftGroupSetting.ByDepth;
        [JsonProperty]
        public EverythingElseGroupSetting EverythingElseGroupSetting { get; private set; } = EverythingElseGroupSetting.Together;
        [JsonProperty]
        public RetrieveGroupSetting RetrieveGroupSetting { get; private set; } = RetrieveGroupSetting.Together;
        [JsonProperty]
        public HouseVendorSetting HouseVendorSetting { get; set; } = HouseVendorSetting.Together;

        [JsonProperty] 
        public OutputOrderingSetting OutputOrderingSetting { get; set; } = OutputOrderingSetting.AsAdded;
        [JsonProperty]
        public bool HQRequired { get; set; }

        public CraftCompletionMode CraftCompletionMode { get; set; } = CraftCompletionMode.Delete;

        public void SetCrystalGroupSetting(CrystalGroupSetting newValue)
        {
            CrystalGroupSetting = newValue;
            ClearGroupCache();
        }

        public void SetCurrencyGroupSetting(CurrencyGroupSetting newValue)
        {
            CurrencyGroupSetting = newValue;
            ClearGroupCache();
        }
        
        public void SetPrecraftGroupSetting(PrecraftGroupSetting newValue)
        {
            PrecraftGroupSetting = newValue;
            ClearGroupCache();
        }
        
        public void SetEverythingElseGroupSetting(EverythingElseGroupSetting newValue)
        {
            EverythingElseGroupSetting = newValue;
            ClearGroupCache();
        }
        
        public void SetRetrieveGroupSetting(RetrieveGroupSetting newValue)
        {
            RetrieveGroupSetting = newValue;
            ClearGroupCache();
        }

        public void ClearGroupCache()
        {
            _clearGroupCache = true;
        }
        
        public void PriceList(CraftPricer pricer)
        {
            var prices = pricer.GetItemPricing(GetFlattenedMergedMaterials(), WorldPricePreference, true);
        }


        private List<CraftGrouping>? _craftGroupings;
        private bool _hideComplete;
        private bool _clearGroupCache;

        public List<CraftGrouping> GetOutputList(bool forceRefresh = false)
        {
            if (_clearGroupCache || _craftGroupings == null || forceRefresh)
            {
                _clearGroupCache = false;
                _craftGroupings = GenerateGroupedCraftItems();
            }

            return _craftGroupings;
        }

        private List<CraftGrouping> GenerateGroupedCraftItems()
        {
            var craftGroupings = new List<CraftGrouping>();
            var groupedItems = GetFlattenedMergedMaterials();
            
            if(HideComplete)
            {
                groupedItems = groupedItems.Where(c => !HideComplete || !c.IsCompleted).ToList();
            }

            var sortedItems = new Dictionary<(CraftGroupType, uint?), List<CraftItem>>();

            void AddToGroup(CraftItem craftItem, CraftGroupType type, uint? identifier = null)
            {
                (CraftGroupType, uint?) key = (type, identifier);
                sortedItems.TryAdd(key, new List<CraftItem>());
                sortedItems[key].Add(craftItem);
            }

            foreach (var item in groupedItems)
            {
                if (item.IsOutputItem)
                {
                    AddToGroup(item, CraftGroupType.Output);
                    continue;
                }

                //Early Retrieval
                if (RetrieveGroupSetting == RetrieveGroupSetting.Together && RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst && item.QuantityWillRetrieve != 0)
                {
                    AddToGroup(item, CraftGroupType.Retrieve);
                    continue;
                }
                
                //Late Retrieval
                if (RetrieveGroupSetting == RetrieveGroupSetting.Together && RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveLast && item.QuantityWillRetrieve != 0 && item.QuantityMissingInventory == item.QuantityWillRetrieve)
                {
                    AddToGroup(item, CraftGroupType.Retrieve);
                    continue;
                }
                
                //Precrafts
                if (item.Item.CanBeCrafted && item.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    if (PrecraftGroupSetting == PrecraftGroupSetting.Together)
                    {
                        AddToGroup(item, CraftGroupType.Precraft);
                        continue;
                    }
                    else if (PrecraftGroupSetting == PrecraftGroupSetting.ByDepth)
                    {
                        AddToGroup(item, CraftGroupType.PrecraftDepth, item.Depth);
                        continue;
                    }
                    else if (PrecraftGroupSetting == PrecraftGroupSetting.ByClass)
                    {
                        AddToGroup(item, CraftGroupType.PrecraftClass, item.Recipe?.CraftType.Row ?? 0);
                        continue;
                    }
                }
                
                if(CurrencyGroupSetting == CurrencyGroupSetting.Separate && item.Item.IsCurrency)
                {
                    AddToGroup(item, CraftGroupType.Currency);
                    continue;
                }
                if(CrystalGroupSetting == CrystalGroupSetting.Separate && item.Item.IsCrystal)
                {
                    AddToGroup(item, CraftGroupType.Crystals);
                    continue;
                }
                

                if (item.IngredientPreference.Type == IngredientPreferenceType.Buy || item.IngredientPreference.Type == IngredientPreferenceType.Item)
                {
                    var mapIds = item.Item.GetSourceMaps(item.IngredientPreference.Type, item.IngredientPreference.LinkedItemId)
                        .OrderBySequence(ZonePreferenceOrder, location => location);
                    MapEx? selectedLocation = null;
                    uint? mapPreference;
                    if (item.IngredientPreference.Type == IngredientPreferenceType.Buy)
                    {
                        mapPreference = ZoneBuyPreferences.ContainsKey(item.ItemId)
                            ? ZoneBuyPreferences[item.ItemId]
                            : null;
                    }
                    else
                    {
                        mapPreference = ZoneItemPreferences.ContainsKey(item.ItemId)
                            ? ZoneItemPreferences[item.ItemId]
                            : null;
                    }

                    foreach (var mapId in mapIds)
                    {
                        if (selectedLocation == null)
                        {
                            selectedLocation = Service.ExcelCache.GetMapSheet().GetRow(mapId);
                        }

                        if (mapPreference != null && mapPreference == mapId)
                        {
                            selectedLocation = Service.ExcelCache.GetMapSheet().GetRow(mapId);
                            break;
                        }
                    }

                    item.MapId = selectedLocation?.RowId ?? null;
                    if (selectedLocation != null && EverythingElseGroupSetting == EverythingElseGroupSetting.ByClosestZone)
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse, item.MapId);
                    }
                    else
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse);
                    }
                }
                else if (item.IngredientPreference.Type == IngredientPreferenceType.Mobs)
                {
                    uint? selectedLocation = null;
                    uint? mapPreference = ZoneMobPreferences.ContainsKey(item.ItemId)
                            ? ZoneMobPreferences[item.ItemId]
                            : null;
                    foreach (var mobSpawns in item.Item.MobDrops.SelectMany(mobDrop => mobDrop.GroupedMobSpawns).Select(c => Service.ExcelCache.GetTerritoryTypeExSheet().GetRow(c.Key.RowId)!.MapEx.Row).OrderBySequence(ZonePreferenceOrder, u => u))
                    {
                        if (selectedLocation == null)
                        {
                            selectedLocation = mobSpawns;
                        }

                        if (mapPreference != null && mapPreference == mobSpawns)
                        {
                            selectedLocation = mobSpawns;
                            break;
                        }
                    }                        
                    item.MapId = selectedLocation;
                    if (selectedLocation != null && EverythingElseGroupSetting == EverythingElseGroupSetting.ByClosestZone)
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse, selectedLocation);
                    }
                    else
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse);
                    }

                }
                else if (item.IngredientPreference.Type == IngredientPreferenceType.HouseVendor)
                {
                    if (HouseVendorSetting == HouseVendorSetting.Separate)
                    {
                        AddToGroup(item, CraftGroupType.HouseVendors);
                    }
                    else
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse);
                    }

                }
                else if (item.IngredientPreference.Type == IngredientPreferenceType.Botany ||
                         item.IngredientPreference.Type == IngredientPreferenceType.Mining)
                {

                    uint? selectedLocation = null;
                    uint? mapPreference;
                    if (item.IngredientPreference.Type == IngredientPreferenceType.Buy)
                    {
                        mapPreference = ZoneBuyPreferences.ContainsKey(item.ItemId)
                            ? ZoneBuyPreferences[item.ItemId]
                            : null;
                    }
                    else
                    {
                        mapPreference = ZoneItemPreferences.ContainsKey(item.ItemId)
                            ? ZoneItemPreferences[item.ItemId]
                            : null;
                    }

                    foreach (var gatheringSource in item.Item.GetGatheringSources()
                                 .OrderBySequence(ZonePreferenceOrder, source => source.TerritoryType.RowId))
                    {
                        if (gatheringSource.TerritoryType.RowId == 0 || gatheringSource.PlaceName.RowId == 0) continue;
                        if (selectedLocation == null)
                        {
                            selectedLocation = gatheringSource.TerritoryType.Map.Row;
                        }

                        if (mapPreference != null && mapPreference == gatheringSource.TerritoryType.Map.Row)
                        {
                            selectedLocation = gatheringSource.TerritoryType.Map.Row;
                            break;
                        }
                    }

                    item.MapId = selectedLocation;

                    if (selectedLocation != null &&
                        EverythingElseGroupSetting == EverythingElseGroupSetting.ByClosestZone)
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse, selectedLocation);
                    }
                    else
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse);
                    }
                }
                else
                {
                    AddToGroup(item, CraftGroupType.EverythingElse);
                }
            }

            uint OrderByCraftGroupType(CraftGrouping craftGroup)
            {
                switch (craftGroup.CraftGroupType)
                {
                    case CraftGroupType.Output:
                    {
                        return 0;
                    }
                    case CraftGroupType.Precraft:
                    {
                        return 10 + (craftGroup.ClassJobId ?? 0);
                    }
                    case CraftGroupType.HouseVendors:
                    {
                        return 51;
                    }
                    case CraftGroupType.EverythingElse:
                    {
                        //Rework this ordering later so that it's based off the aetheryte list
                        return 52 + (craftGroup.MapId ?? 0);
                    }
                    case CraftGroupType.Retrieve:
                    {
                        return RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst ? 1052u : 50u;
                    }
                    case CraftGroupType.Crystals:
                    {
                        return 1060;
                    }
                    case CraftGroupType.Currency:
                    {
                        return 1070;
                    }
                }

                return 1080;
            }

            foreach (var sortedGroup in sortedItems)
            {
                if (sortedGroup.Key.Item1 == CraftGroupType.Output)
                {
                    var outputItems = sortedGroup.Value;
                    if (OutputOrderingSetting == OutputOrderingSetting.ByClass)
                    {
                        outputItems = outputItems.OrderBy(c => c.Recipe?.CraftType.Row ?? 0).ToList();
                    }
                    if (OutputOrderingSetting == OutputOrderingSetting.ByName)
                    {
                        outputItems = outputItems.OrderBy(c => c.FormattedName).ToList();
                    }
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Output, outputItems));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Currency)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Currency, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Crystals)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Crystals, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Retrieve)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Retrieve, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Precraft)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Precraft, sortedGroup.Value.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList()));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.PrecraftDepth)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Precraft, sortedGroup.Value.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList(), sortedGroup.Key.Item2));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.PrecraftClass)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Precraft, sortedGroup.Value.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList(),null, sortedGroup.Key.Item2));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.EverythingElse)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.EverythingElse, sortedGroup.Value,null,null, sortedGroup.Key.Item2));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.HouseVendors)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.HouseVendors, sortedGroup.Value));
                }
            }

            craftGroupings = craftGroupings.OrderBy(OrderByCraftGroupType).ToList();

            return craftGroupings.Where(c => c.CraftItems.Count != 0).ToList();
        }

        private bool FilterRetrieveItems(bool groupRetrieve, CraftItem craftItem)
        {
            return !groupRetrieve || (craftItem.QuantityWillRetrieve != 0 && RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst) || craftItem.QuantityWillRetrieve == 0;
        }
        
        public void ResetWorldPricePreferences()
        {
            _worldPricePreference = new();
        }

        public void ResetIngredientPreferences()
        {
            _ingredientPreferenceTypeOrder = new List<(IngredientPreferenceType,uint?)>()
            {   
                (IngredientPreferenceType.Crafting,null),
                (IngredientPreferenceType.Mining,null),
                (IngredientPreferenceType.Botany,null),
                (IngredientPreferenceType.Fishing,null),
                (IngredientPreferenceType.Venture,null),
                (IngredientPreferenceType.Buy,null),
                (IngredientPreferenceType.HouseVendor,null),
                (IngredientPreferenceType.ResourceInspection,null),
                (IngredientPreferenceType.Mobs,null),
                (IngredientPreferenceType.Desynthesis,null),
                (IngredientPreferenceType.Reduction,null),
                (IngredientPreferenceType.Gardening,null),
                (IngredientPreferenceType.Item,20),
                (IngredientPreferenceType.Item,21),
                (IngredientPreferenceType.Item,22),
                (IngredientPreferenceType.Item,28),//Poetics
                (IngredientPreferenceType.Item,25199),//White Crafters' Scrip
                (IngredientPreferenceType.Item,33913),//Purple Crafters' Scrip
                (IngredientPreferenceType.Item,25200),//White Gatherers Scrip
                (IngredientPreferenceType.Item,33914),//Purple Gatherers Scrip
                (IngredientPreferenceType.Marketboard,null),
                (IngredientPreferenceType.ExplorationVenture,null),
                (IngredientPreferenceType.Item,null),
            };
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<(IngredientPreferenceType,uint?)> IngredientPreferenceTypeOrder
        {
            get
            {
                if (_ingredientPreferenceTypeOrder == null)
                {
                    _ingredientPreferenceTypeOrder = new();
                    ResetIngredientPreferences();
                }

                return _ingredientPreferenceTypeOrder;
            }
            set => _ingredientPreferenceTypeOrder = value?.Distinct().ToList() ?? new List<(IngredientPreferenceType, uint?)>();
        }
        
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<uint> WorldPricePreference
        {
            get
            {
                if (_worldPricePreference == null)
                {
                    _worldPricePreference = new();
                }
                return _worldPricePreference;
            }
            set => _worldPricePreference = value ?? new List<uint>();
        }


        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<uint> ZonePreferenceOrder
        {
            get
            {
                if (_zonePreferenceOrder == null)
                {
                    _zonePreferenceOrder = new();
                }

                return _zonePreferenceOrder;
            }
            set => _zonePreferenceOrder = value?.Distinct().ToList() ?? new List<uint>();
        }

        public List<CraftItem> CraftItems
        {
            get
            {
                if (_craftItems == null)
                {
                    _craftItems = new List<CraftItem>();
                }
                return _craftItems;
            }
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, IngredientPreference> IngredientPreferences
        {
            get => _ingredientPreferences ??= new Dictionary<uint, IngredientPreference>();
            private set => _ingredientPreferences = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, CraftRetainerRetrieval> CraftRetainerRetrievals
        {
            get
            {
                if (_craftRetainerRetrievals == null)
                {
                    _craftRetainerRetrievals = new Dictionary<uint, CraftRetainerRetrieval>();
                }
                return _craftRetainerRetrievals;
            }
            set => _craftRetainerRetrievals = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> ZoneItemPreferences
        {
            get => _zoneItemPreferences ??= new Dictionary<uint, uint>();
            set => _zoneItemPreferences = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> ZoneBuyPreferences
        {
            get => _zoneBuyPreferences ??= new Dictionary<uint, uint>();
            set => _zoneBuyPreferences = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> ZoneMobPreferences
        {
            get => _zoneMobPreferences ??= new Dictionary<uint, uint>();
            set => _zoneMobPreferences = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> ZoneBotanyPreferences
        {
            get => _zoneBotanyPreferences ??= new Dictionary<uint, uint>();
            set => _zoneBotanyPreferences = value;
        }

        /// <summary>
        /// When sourcing items via mining, which zone is preferred for a specific item
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> ZoneMiningPreferences
        {
            get => _zoneMiningPreferences ??= new Dictionary<uint, uint>();
            set => _zoneMiningPreferences = value;
        }
        
        /// <summary>
        /// When sourcing items via the marketboard, should be override the list preferences and pick a specific world instead
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> MarketItemWorldPreference
        {
            get => _marketItemWorldPreference ??= new Dictionary<uint, uint>();
            set => _marketItemWorldPreference = value;
        }
        
        /// <summary>
        /// When sourcing items via the marketboard and there are no prices available, how much gil should the item be flagged as
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> MarketItemPriceOverride
        {
            get => _marketItemPriceOverride ??= new Dictionary<uint, uint>();
            set => _marketItemPriceOverride = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, uint> CraftRecipePreferences
        {
            get
            {
                if (_craftRecipePreferences == null)
                {
                    _craftRecipePreferences = new Dictionary<uint, uint>();
                }
                return _craftRecipePreferences;
            }
            set => _craftRecipePreferences = value;
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<uint, bool> HQRequireds
        {
            get
            {
                if (_hqRequired == null)
                {
                    _hqRequired = new Dictionary<uint, bool>();
                }
                return _hqRequired;
            }
            set => _hqRequired = value;
        }
        
        public bool? GetHQRequired(uint itemId)
        {
            if (!HQRequireds.ContainsKey(itemId))
            {
                return null;
            }

            return HQRequireds[itemId];
        }

        public void UpdateHQRequired(uint itemId, bool? newValue)
        {
            if (newValue == null)
            {
                HQRequireds.Remove(itemId);
            }
            else
            {
                HQRequireds[itemId] = newValue.Value;
            }
        }

        public CraftRetainerRetrieval? GetCraftRetainerRetrieval(uint itemId)
        {
            if (!CraftRetainerRetrievals.ContainsKey(itemId))
            {
                return null;
            }

            return CraftRetainerRetrievals[itemId];
        }
        
        public void UpdateCraftRetainerRetrieval(uint itemId, CraftRetainerRetrieval? newValue)
        {
            if (newValue == null)
            {
                CraftRetainerRetrievals.Remove(itemId);
            }
            else
            {
                CraftRetainerRetrievals[itemId] = newValue.Value;
            }
        }
        
        public void UpdateCraftRecipePreference(uint itemId, uint? newRecipeId)
        {
            if (newRecipeId == null)
            {
                CraftRecipePreferences.Remove(itemId);
            }
            else
            {
                CraftRecipePreferences[itemId] = newRecipeId.Value;
            }
        }
        
        public void UpdateZoneItemPreference(uint itemId, uint? territoryId)
        {
            if (territoryId == null)
            {
                ZoneItemPreferences.Remove(itemId);
            }
            else
            {
                ZoneItemPreferences[itemId] = territoryId.Value;
            }
        }
        
        public uint? GetZoneItemPreference(uint itemId)
        {
            if (!ZoneItemPreferences.ContainsKey(itemId))
            {
                return null;
            }

            return ZoneItemPreferences[itemId];
        }
        
        public void UpdateZoneBuyPreference(uint itemId, uint? newValue)
        {
            if (newValue == null)
            {
                ZoneBuyPreferences.Remove(itemId);
            }
            else
            {
                ZoneBuyPreferences[itemId] = newValue.Value;
            }
        }
        
        public uint? GetZoneBuyPreference(uint itemId)
        {
            if (!ZoneBuyPreferences.ContainsKey(itemId))
            {
                return null;
            }

            return ZoneBuyPreferences[itemId];
        }
        
        public void UpdateZoneBotanyPreference(uint itemId, uint? newValue)
        {
            if (newValue == null)
            {
                ZoneBotanyPreferences.Remove(itemId);
            }
            else
            {
                ZoneBotanyPreferences[itemId] = newValue.Value;
            }
        }
        
        public uint? GetZoneBotanyPreference(uint itemId)
        {
            if (!ZoneBotanyPreferences.ContainsKey(itemId))
            {
                return null;
            }

            return ZoneBotanyPreferences[itemId];
        }
        
        public void UpdateZoneMiningPreference(uint itemId, uint? newValue)
        {
            if (newValue == null)
            {
                ZoneMiningPreferences.Remove(itemId);
            }
            else
            {
                ZoneMiningPreferences[itemId] = newValue.Value;
            }
        }
        
        public uint? GetZoneMiningPreference(uint itemId)
        {
            if (!ZoneMiningPreferences.ContainsKey(itemId))
            {
                return null;
            }

            return ZoneMiningPreferences[itemId];
        }
        
        public void UpdateZoneMobPreference(uint itemId, uint? newValue)
        {
            if (newValue == null)
            {
                ZoneMobPreferences.Remove(itemId);
            }
            else
            {
                ZoneMobPreferences[itemId] = newValue.Value;
            }
        }
        
        public uint? GetZoneMobPreference(uint itemId)
        {
            if (!ZoneMobPreferences.ContainsKey(itemId))
            {
                return null;
            }

            return ZoneMobPreferences[itemId];
        }

        public uint? GetZonePreference(IngredientPreferenceType type, uint itemId)
        {
            switch (type)
            {
                case IngredientPreferenceType.Buy:
                    return GetZoneBuyPreference(itemId);
                case IngredientPreferenceType.Mobs:
                    return GetZoneMobPreference(itemId);
                case IngredientPreferenceType.Item:
                    return GetZoneItemPreference(itemId);
                case IngredientPreferenceType.Botany:
                    return GetZoneBotanyPreference(itemId);
                case IngredientPreferenceType.Mining:
                    return GetZoneMiningPreference(itemId);
            }

            return null;
        }

        public void UpdateZonePreference(IngredientPreferenceType type, uint itemId, uint? newValue)
        {
            switch (type)
            {
                case IngredientPreferenceType.Buy:
                    UpdateZoneBuyPreference(itemId, newValue);
                    return;
                case IngredientPreferenceType.Mobs:
                    UpdateZoneMobPreference(itemId, newValue);
                    return;
                case IngredientPreferenceType.Item:
                    UpdateZoneItemPreference(itemId, newValue);
                    return;
                case IngredientPreferenceType.Botany:
                    UpdateZoneBotanyPreference(itemId, newValue);
                    return;
                case IngredientPreferenceType.Mining:
                    UpdateZoneMiningPreference(itemId, newValue);
                    return;
            }
        }

        public void UpdateItemWorldPreference(uint itemId, uint? newValue)
        {
            if (newValue == null)
            {
                MarketItemWorldPreference.Remove(itemId);
            }
            else
            {
                MarketItemWorldPreference[itemId] = newValue.Value;
            }
        }

        public void UpdateMarketItemPriceOverride(uint itemId, uint? newValue)
        {
            if (newValue == null)
            {
                MarketItemPriceOverride.Remove(itemId);
            }
            else
            {
                MarketItemPriceOverride[itemId] = newValue.Value;
            }
        }
        
        public void CalculateCosts(CraftListConfiguration craftListConfiguration, CraftPricer craftPricer)
        {
            //Fix me later
            var minimumNQCost = 0u;
            var minimumHQCost = 0u;
            var list = GetFlattenedMergedMaterials();
            var worldIds = WorldPricePreference.ToList();
            if (craftListConfiguration.WorldPreferences != null)
            {
                foreach (var worldId in craftListConfiguration.WorldPreferences)
                {
                    if (!worldIds.Contains(worldId))
                    {
                        worldIds.Add(worldId);
                    }
                }
            }
            var itemPricing = craftPricer.GetItemPricing(list, worldIds, true).GroupBy(c => c.ItemId).ToDictionary(c => c.Key, c => c.ToList());
            
            for (var index = 0; index < list.Count; index++)
            {
                var craftItem = list[index];
                UpdateItemPricing(itemPricing, craftItem);
            }
        }

        public CraftList AddCraftItem(string itemName, uint quantity = 1,
            InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
        {
            if (Service.ExcelCache.ItemsByName.ContainsKey(itemName))
            {
                var itemId = Service.ExcelCache.ItemsByName[itemName];
                AddCraftItem(itemId, quantity, flags, phase);
            }
            else
            {
                throw new Exception("Item with name " + itemName + " could not be found");
            }

            return this;
        }

        public CraftList AddCraftItem(uint itemId, uint quantity = 1, InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
        {
            var item = Service.ExcelCache.GetItemExSheet().GetRow(itemId);
            if (item != null)
            {
                if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase))
                {
                    var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                    craftItem.AddQuantity(quantity);
                }
                else
                {
                    var newCraftItems = CraftItems.ToList();
                    newCraftItems.Add(new CraftItem(itemId, flags, quantity, null, true, null, phase));
                    _craftItems = newCraftItems;
                }
                BeenGenerated = false;
                NeedsRefresh = true;
            }
            return this;
        }

        public void AddCompanyCraftItem(uint itemId, uint quantity, uint phase, bool includeItem = false, CompanyCraftStatus status = CompanyCraftStatus.Normal)
        {
            if (includeItem)
            {
                AddCraftItem(itemId, quantity, InventoryItem.ItemFlags.None, phase);
                return;
            }
        }

        public void SetCraftRecipe(uint itemId, uint newRecipeId)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.IsOutputItem))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.IsOutputItem);
                craftItem.SwitchRecipe(newRecipeId);
                BeenGenerated = false;
                NeedsRefresh = true;
            }
        }

        public void SetCraftPhase(uint itemId, uint? newPhase, uint? oldPhase)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.IsOutputItem && c.Phase == oldPhase))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.IsOutputItem && c.Phase == oldPhase);
                craftItem.SwitchPhase(newPhase);
                BeenGenerated = false;
                NeedsRefresh = true;
            }
        }

        public void SetCraftRequiredQuantity(uint itemId, uint quantity, InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                craftItem.SetQuantity(quantity);
                BeenGenerated = false;
                NeedsRefresh = true;
            }
        }
        
        public void RemoveCraftItem(uint itemId, InventoryItem.ItemFlags itemFlags)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags))
            {
                var withRemoved = CraftItems.ToList();
                withRemoved.RemoveAll(c => c.ItemId == itemId && c.Flags == itemFlags);
                _craftItems = withRemoved;
                BeenGenerated = false;
                NeedsRefresh = true;
                ClearGroupCache();
            }
        }
        
        public void RemoveCraftItem(uint itemId)
        {
            if (CraftItems.Any(c => c.ItemId == itemId))
            {
                var withRemoved = CraftItems.ToList();
                withRemoved.RemoveAll(c => c.ItemId == itemId);
                _craftItems = withRemoved;
                BeenGenerated = false;
                NeedsRefresh = true;
                ClearGroupCache();
            }
        }
        
        public void RemoveCraftItem(uint itemId, uint quantity, InventoryItem.ItemFlags itemFlags)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags))
            {
                var withRemoved = CraftItems.ToList();
                var totalRequired = withRemoved.Where(c =>  c.ItemId == itemId && c.Flags == itemFlags).Sum( c => c.QuantityRequired);
                if (totalRequired > quantity)
                {
                    SetCraftRequiredQuantity(itemId, (uint)(totalRequired - quantity), itemFlags);
                }
                else
                {
                    withRemoved.RemoveAll(c => c.ItemId == itemId && c.Flags == itemFlags);
                    _craftItems = withRemoved;
                }
                BeenGenerated = false;
                NeedsRefresh = true;
                ClearGroupCache();
            }
        }

        public void GenerateCraftChildren()
        {
            _flattenedMergedMaterials = null;
            var leftOvers = new Dictionary<uint, double>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                craftItem.ClearChildCrafts();
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem, leftOvers).OrderByDescending(c => c.RecipeId).ToList();
            }
            BeenGenerated = true;
        }
        
        public IngredientPreference? GetIngredientPreference(uint itemId)
        {
            return IngredientPreferences.ContainsKey(itemId) ? IngredientPreferences[itemId] : null;
        }
        
        public uint? GetMarketItemWorldPreference(uint itemId)
        {
            return MarketItemWorldPreference.ContainsKey(itemId) ? MarketItemWorldPreference[itemId] : null;
        }
        
        public uint? GetMarketItemPriceOverride(uint itemId)
        {
            return MarketItemPriceOverride.ContainsKey(itemId) ? MarketItemPriceOverride[itemId] : null;
        }

        public void UpdateIngredientPreference(uint itemId, IngredientPreference? ingredientPreference)
        {
            if (ingredientPreference == null)
            {
                IngredientPreferences.Remove(itemId);
            }
            else
            {
                IngredientPreferences[itemId] = ingredientPreference;
            }
            BeenGenerated = false;
            NeedsRefresh = true;
        }
        
        /// <summary>
        /// Generates the required materials within a craft item.
        /// </summary>
        /// <param name="craftItem"></param>
        /// <param name="spareIngredients"></param>
        /// <returns></returns>
        private List<CraftItem> CalculateChildCrafts(CraftItem craftItem, Dictionary<uint, double>? spareIngredients = null, CraftItem? parentItem = null)
        {
            if (spareIngredients == null)
            {
                spareIngredients = new Dictionary<uint, double>();
            }
            var childCrafts = new List<CraftItem>();
            if (craftItem.QuantityRequired == 0)
            {
                return childCrafts;
            }
            craftItem.MissingIngredients = new ConcurrentDictionary<(uint, bool), uint>();
            IngredientPreference? ingredientPreference = null;
            if (IngredientPreferences.ContainsKey(craftItem.ItemId))
            {
                if (IngredientPreferences[craftItem.ItemId].Type == IngredientPreferenceType.None)
                {
                    UpdateIngredientPreference(craftItem.ItemId, null);
                }
                else
                {
                    ingredientPreference = IngredientPreferences[craftItem.ItemId];
                }
            }
            
            if(ingredientPreference == null)
            {
                foreach (var defaultPreference in IngredientPreferenceTypeOrder)
                {
                    if (craftItem.Item.GetIngredientPreference(defaultPreference.Item1, defaultPreference.Item2,out ingredientPreference))
                    {
                        break;
                    }
                }
            }

            if (ingredientPreference == null)
            {
                ingredientPreference = craftItem.Item.IngredientPreferences.FirstOrDefault();
            }
            
            if (ingredientPreference != null)
            {
                craftItem.IngredientPreference = new IngredientPreference(ingredientPreference);
                switch (ingredientPreference.Type)
                {
                    case IngredientPreferenceType.Botany:
                    case IngredientPreferenceType.Fishing:
                    case IngredientPreferenceType.Mining:
                    {
                        return childCrafts;
                    }
                    case IngredientPreferenceType.Buy:
                    case IngredientPreferenceType.HouseVendor:
                    {
                        if (craftItem.Item.BuyFromVendorPrice != 0 && craftItem.Item.ObtainedGil)
                        {
                            var childCraftItem = new CraftItem(1, InventoryItem.ItemFlags.None,
                                (uint)craftItem.Item.BuyFromVendorPrice * craftItem.QuantityRequired,
                                (uint)craftItem.Item.BuyFromVendorPrice * craftItem.QuantityNeeded);
                            childCraftItem.ChildCrafts =
                                CalculateChildCrafts(childCraftItem, spareIngredients, craftItem)
                                    .OrderByDescending(c => c.RecipeId).ToList();
                            childCrafts.Add(childCraftItem);
                        }

                        return childCrafts;
                    }
                    case IngredientPreferenceType.Marketboard:
                    {
                        //TODO: Add in a manually set amount so that we can price items even if there are no marketboard prices
                        var childCraftItem = new CraftItem(1, InventoryItem.ItemFlags.None, 0);
                        childCrafts.Add(childCraftItem);
                        return childCrafts;
                    }
                    case IngredientPreferenceType.Venture:
                    {
                        var quantity = 1u;
                        if (craftItem.Item.RetainerFixedTasks != null && craftItem.Item.RetainerFixedTasks.Count != 0)
                        {
                            var retainerTask = craftItem.Item.RetainerFixedTasks.First();
                            if (retainerTask.Quantity != 0)
                            {
                                quantity = retainerTask.Quantity;
                            }
                        }

                        //TODO: Work out the exact amount of ventures required.
                        var ventureItem = new CraftItem(21072, InventoryItem.ItemFlags.None,
                            (uint)Math.Ceiling(craftItem.QuantityRequired / (double)quantity),
                            (uint)Math.Ceiling(craftItem.QuantityNeeded / (double)quantity));
                        ventureItem.ChildCrafts = CalculateChildCrafts(ventureItem, spareIngredients, craftItem)
                            .OrderByDescending(c => c.RecipeId).ToList();
                        childCrafts.Add(ventureItem);
                        return childCrafts;
                    }
                    case IngredientPreferenceType.ExplorationVenture:
                    {
                        var quantity = 1u;
                        if (craftItem.Item.RetainerRandomTasks != null && craftItem.Item.RetainerRandomTasks.Count != 0)
                        {
                            var retainerTask = craftItem.Item.RetainerRandomTasks.First();
                            if (retainerTask.Quantity != 0)
                            {
                                quantity = retainerTask.Quantity;
                            }
                        }

                        //TODO: Work out the exact amount of ventures required.
                        var ventureItem = new CraftItem(21072, InventoryItem.ItemFlags.None,
                            (uint)Math.Ceiling(craftItem.QuantityRequired / (double)quantity),
                            (uint)Math.Ceiling(craftItem.QuantityNeeded / (double)quantity));
                        ventureItem.ChildCrafts = CalculateChildCrafts(ventureItem, spareIngredients, craftItem)
                            .OrderByDescending(c => c.RecipeId).ToList();
                        childCrafts.Add(ventureItem);
                        return childCrafts;
                    }
                    case IngredientPreferenceType.Item:
                    {
                        if (ingredientPreference.LinkedItemId != null &&
                            ingredientPreference.LinkedItemQuantity != null)
                        {
                            if (parentItem != null && ingredientPreference.LinkedItemId == parentItem.ItemId)
                            {
                                //Stops recursion
                                return childCrafts;
                            }

                            var childCraftItem = new CraftItem(ingredientPreference.LinkedItemId.Value,
                                (GetHQRequired(ingredientPreference.LinkedItemId.Value) ?? HQRequired)
                                    ? InventoryItem.ItemFlags.HighQuality
                                    : InventoryItem.ItemFlags.None,
                                craftItem.QuantityRequired * (uint)ingredientPreference.LinkedItemQuantity,
                                craftItem.QuantityNeeded * (uint)ingredientPreference.LinkedItemQuantity);
                            childCraftItem.ChildCrafts =
                                CalculateChildCrafts(childCraftItem, spareIngredients, craftItem)
                                    .OrderByDescending(c => c.RecipeId).ToList();
                            childCrafts.Add(childCraftItem);
                            if (ingredientPreference.LinkedItem2Id != null &&
                                ingredientPreference.LinkedItem2Quantity != null)
                            {
                                var secondChildCraftItem = new CraftItem(ingredientPreference.LinkedItem2Id.Value,
                                    (GetHQRequired(ingredientPreference.LinkedItem2Id.Value) ?? HQRequired)
                                        ? InventoryItem.ItemFlags.HighQuality
                                        : InventoryItem.ItemFlags.None,
                                    craftItem.QuantityRequired * (uint)ingredientPreference.LinkedItem2Quantity,
                                    craftItem.QuantityNeeded * (uint)ingredientPreference.LinkedItem2Quantity);
                                secondChildCraftItem.ChildCrafts =
                                    CalculateChildCrafts(secondChildCraftItem, spareIngredients, craftItem)
                                        .OrderByDescending(c => c.RecipeId).ToList();
                                childCrafts.Add(secondChildCraftItem);
                            }

                            if (ingredientPreference.LinkedItem3Id != null &&
                                ingredientPreference.LinkedItem3Quantity != null)
                            {
                                var thirdChildCraftItem = new CraftItem(ingredientPreference.LinkedItem3Id.Value,
                                    (GetHQRequired(ingredientPreference.LinkedItem3Id.Value) ?? HQRequired)
                                        ? InventoryItem.ItemFlags.HighQuality
                                        : InventoryItem.ItemFlags.None,
                                    craftItem.QuantityRequired * (uint)ingredientPreference.LinkedItem3Quantity,
                                    craftItem.QuantityNeeded * (uint)ingredientPreference.LinkedItem3Quantity);
                                thirdChildCraftItem.ChildCrafts =
                                    CalculateChildCrafts(thirdChildCraftItem, spareIngredients, craftItem)
                                        .OrderByDescending(c => c.RecipeId).ToList();
                                childCrafts.Add(thirdChildCraftItem);
                            }
                        }

                        return childCrafts;
                    }
                    case IngredientPreferenceType.Reduction:
                    {
                        if (ingredientPreference.LinkedItemId != null &&
                            ingredientPreference.LinkedItemQuantity != null)
                        {
                            if (parentItem != null && ingredientPreference.LinkedItemId == parentItem.ItemId)
                            {
                                //Stops recursion
                                return childCrafts;
                            }

                            var childCraftItem = new CraftItem(ingredientPreference.LinkedItemId.Value,
                                (GetHQRequired(ingredientPreference.LinkedItemId.Value) ?? HQRequired)
                                    ? InventoryItem.ItemFlags.HighQuality
                                    : InventoryItem.ItemFlags.None,
                                craftItem.QuantityRequired * (uint)ingredientPreference.LinkedItemQuantity,
                                craftItem.QuantityNeeded * (uint)ingredientPreference.LinkedItemQuantity);
                            childCraftItem.ChildCrafts =
                                CalculateChildCrafts(childCraftItem, spareIngredients, craftItem)
                                    .OrderByDescending(c => c.RecipeId).ToList();
                            childCrafts.Add(childCraftItem);
                        }

                        return childCrafts;
                    }
                    case IngredientPreferenceType.Crafting:
                    {
                        if (craftItem.Recipe == null || !craftItem.IsOutputItem)
                        {
                            if (CraftRecipePreferences.ContainsKey(craftItem.ItemId))
                            {
                                craftItem.RecipeId = CraftRecipePreferences[craftItem.ItemId];
                            }
                            else if (Service.ExcelCache.ItemRecipes.ContainsKey(craftItem.ItemId))
                            {
                                var recipes = Service.ExcelCache.ItemRecipes[craftItem.ItemId];
                                if (recipes.Count != 0)
                                {
                                    craftItem.RecipeId = recipes.First();
                                }
                            }
                        }

                        if (craftItem.Recipe != null)
                        {
                            craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.Crafting);
                            var craftAmountNeeded = Math.Max(0, Math.Ceiling((double)craftItem.QuantityNeeded / craftItem.Yield)) * craftItem.Yield;
                            var craftAmountUsed = craftItem.QuantityNeeded;
                            var amountLeftOver = craftAmountNeeded - craftAmountUsed;
                            if (amountLeftOver > 0)
                            {
                                if (!spareIngredients.ContainsKey(craftItem.ItemId))
                                {
                                    spareIngredients[craftItem.ItemId] = 0;
                                }

                                spareIngredients[craftItem.ItemId] += amountLeftOver;
                            }
                            
                            foreach (var material in craftItem.Recipe.UnkData5)
                            {
                                if (material.ItemIngredient == 0 || material.AmountIngredient == 0)
                                {
                                    continue;
                                }

                                var materialItemId = (uint)material.ItemIngredient;
                                var materialAmountIngredient = (uint)material.AmountIngredient;

                                var quantityNeeded = (double)craftItem.QuantityNeeded;
                                var quantityRequired = (double)craftItem.QuantityRequired;
                                
                                var actualAmountNeeded = Math.Max(0, Math.Ceiling(quantityNeeded / craftItem.Yield)) * materialAmountIngredient;
                                var actualAmountUsed = Math.Max(0, quantityNeeded / craftItem.Yield) * material.AmountIngredient;

                                var actualAmountRequired = Math.Max(0, Math.Ceiling(quantityRequired / craftItem.Yield)) * materialAmountIngredient;

                                var tempAmountNeeded = actualAmountRequired;
                                if (spareIngredients.ContainsKey(materialItemId))
                                {
                                    //Factor in the possible extra we get and then 
                                    var amountAvailable = Math.Max(0,Math.Min(quantityNeeded, spareIngredients[materialItemId]));
                                    //actualAmountRequired -= amountAvailable;
                                    tempAmountNeeded -= amountAvailable;
                                    spareIngredients[materialItemId] -= amountAvailable;
                                }
                                


                                var childCraftItem = new CraftItem(materialItemId, (GetHQRequired(materialItemId) ?? HQRequired) ? InventoryItem.ItemFlags.HighQuality : InventoryItem.ItemFlags.None, (uint)actualAmountRequired, (uint)tempAmountNeeded, false);
                                childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients, craftItem).OrderByDescending(c => c.RecipeId).ToList();
                                childCraftItem.QuantityNeeded = (uint)actualAmountNeeded;
                                childCrafts.Add(childCraftItem);
                            }
                        }
                        else
                        {
                            var companyCraftSequence = craftItem.Item.CompanyCraftSequenceEx;
                            ;
                            if (companyCraftSequence != null)
                            {
                                craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId,
                                    IngredientPreferenceType.Crafting);
                                var materialsRequired = companyCraftSequence.MaterialsRequired(craftItem.Phase);
                                for (var index = 0; index < materialsRequired.Count; index++)
                                {
                                    var materialRequired = materialsRequired[index];
                                    var childCraftItem = new CraftItem(materialRequired.ItemId,
                                        (GetHQRequired(materialRequired.ItemId) ?? false)
                                            ? InventoryItem.ItemFlags.HighQuality
                                            : InventoryItem.ItemFlags.None,
                                        materialRequired.Quantity * craftItem.QuantityRequired,
                                        materialRequired.Quantity * craftItem.QuantityNeeded, false);
                                    childCraftItem.ChildCrafts =
                                        CalculateChildCrafts(childCraftItem, spareIngredients, craftItem)
                                            .OrderByDescending(c => c.RecipeId).ToList();
                                    childCrafts.Add(childCraftItem);
                                }
                            }
                        }

                        return childCrafts;
                    }
                    case IngredientPreferenceType.ResourceInspection:
                    {
                        craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.ResourceInspection);
                        var requirements = Service.ExcelCache.HwdInspectionResults[craftItem.ItemId];
                        var quantityNeeded = 0u;
                        var quantityRequired = 0u;
                        if (requirements.Item2 != 0)
                        {
                            quantityNeeded = (uint)Math.Ceiling((double)craftItem.QuantityNeeded / requirements.Item2) * requirements.Item2;
                            quantityRequired = (uint)Math.Ceiling((double)craftItem.QuantityRequired / requirements.Item2) * requirements.Item2;
                        }
                        var childCraftItem = new CraftItem((uint) requirements.Item1, (GetHQRequired(requirements.Item1) ?? HQRequired) ? InventoryItem.ItemFlags.HighQuality : InventoryItem.ItemFlags.None, quantityRequired, quantityNeeded, false);
                        childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients, craftItem).OrderByDescending(c => c.RecipeId).ToList();
                        childCrafts.Add(childCraftItem);
                        return childCrafts;
                    }
                }
            }
            return childCrafts;
        }

        /// <summary>
        /// Updates an already generated craft item, passing in the items a player has on their person and within retainers to calculate the total amount that will be required.
        /// </summary>
        /// <param name="craftItem"></param>
        /// <param name="craftListConfiguration"></param>
        /// <param name="spareIngredients"></param>
        /// <param name="cascadeCrafts"></param>
        /// <param name="parentItem"></param>
        public void UpdateCraftItem(CraftItem craftItem, CraftListConfiguration craftListConfiguration, Dictionary<uint, double> spareIngredients, bool cascadeCrafts = false, CraftItem? parentItem = null)
        {
            if (craftItem.IsOutputItem)
            {
                craftItem.QuantityNeeded = craftItem.QuantityRequired;
                craftItem.QuantityNeededPreUpdate = craftItem.QuantityNeeded;
                
                //The default is to not source anything from retainers, but if the user does set it, we can pull from retainers 
                var craftRetainerRetrieval = CraftRetainerRetrievalOutput;
                if (CraftRetainerRetrievals.ContainsKey(craftItem.ItemId))
                {
                    craftRetainerRetrieval = CraftRetainerRetrievals[craftItem.ItemId];
                }
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                if (craftRetainerRetrieval is CraftRetainerRetrieval.Yes or CraftRetainerRetrieval.HQOnly)
                {
                    var quantityMissing = craftItem.QuantityMissingInventory;
                    //Service.Log.Log("quantity missing: " + quantityMissing);
                    if (quantityMissing != 0 && craftListConfiguration.ExternalSources.ContainsKey(craftItem.ItemId))
                    {
                        foreach (var externalSource in craftListConfiguration.ExternalSources[craftItem.ItemId])
                        {
                            if ((craftRetainerRetrieval is CraftRetainerRetrieval.HQOnly || (GetHQRequired(craftItem.ItemId) ?? HQRequired)) && !externalSource.IsHq) continue;
                            var stillNeeded = externalSource.UseQuantity((int)quantityMissing);
                            //Service.Log.Log("missing: " + quantityMissing);
                            //Service.Log.Log("Still needed: " + stillNeeded);
                            quantityAvailable += (quantityMissing - stillNeeded);
                        }
                    }
                }
                
                craftItem.QuantityAvailable = quantityAvailable;

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)(Math.Min(craftItem.QuantityAvailable,craftItem.QuantityNeeded) - craftItem.QuantityReady));

                craftItem.QuantityNeeded = (uint)Math.Max(0, (int)craftItem.QuantityNeeded - quantityAvailable);
                
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem, null, craftItem).OrderByDescending(c => c.RecipeId).ToList();
                for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                {
                    var childCraftItem = craftItem.ChildCrafts[index];
                    UpdateCraftItem(childCraftItem, craftListConfiguration, spareIngredients, cascadeCrafts, craftItem);
                }

                if (craftItem.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    IEnumerable<(uint, int)> ingredients;
                    if (craftItem.Recipe != null)
                    {
                        ingredients = craftItem.Recipe.Ingredients.Select(c => (c.Item.Row, c.Count));
                    }
                    else if(craftItem.Item.CompanyCraftSequenceEx != null)
                    {
                        ingredients = craftItem.Item.CompanyCraftSequenceEx.MaterialsRequired(craftItem.Phase).Select(c => ((uint)c.ItemId, (int)c.Quantity));
                    }
                    else
                    {
                        ingredients = new List<(uint Row, int Count)>();
                    }
                    foreach (var ingredient in ingredients)
                    {
                        if (ingredient.Item1 <= 0 || ingredient.Item1 <= 0)
                        {
                            continue;
                        }
                        var ingredientId = ingredient.Item1;
                        var amountNeeded = (double)ingredient.Item2;

                        for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                        {
                            var childCraftItem = craftItem.ChildCrafts[index];
                            if (childCraftItem.ItemId == ingredientId)
                            {
                                var childAmountNeeded = childCraftItem.QuantityNeeded;
                                var childAmountMissing = childCraftItem.QuantityMissingOverall;
                                var craftItemQuantityReady = childCraftItem.QuantityReady;
                                if (cascadeCrafts)
                                {
                                    craftItemQuantityReady += childCraftItem.QuantityCanCraft;
                                }
                                var craftCapable = (uint)Math.Floor(craftItemQuantityReady / amountNeeded);
                                if (childAmountMissing > 0)
                                {
                                    var key = (childCraftItem.ItemId,childCraftItem.Flags == InventoryItem.ItemFlags.HighQuality);
                                    craftItem.MissingIngredients.TryAdd(key, 0);
                                    craftItem.MissingIngredients[key] += (uint)childAmountMissing;
                                }
                                //Service.Log.Log("amount craftable for ingredient " + craftItem.ItemId + " for output item is " + craftCapable);
                                if (totalCraftCapable == null)
                                {
                                    totalCraftCapable = craftCapable;
                                }
                                else
                                {
                                    totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                                }
                            }
                        }
                    }

                    craftItem.QuantityCanCraft = Math.Min(craftItem.QuantityNeeded * craftItem.Yield, (totalCraftCapable ?? 0) * craftItem.Yield);
                }
                else if (craftItem.IngredientPreference.Type == IngredientPreferenceType.Marketboard)
                {
                    if (craftListConfiguration.CraftPricer != null)
                    {
                        var itemPricing = craftListConfiguration.GetItemPricing(craftItem.ItemId, MarketItemWorldPreference.ContainsKey(craftItem.ItemId) ? MarketItemWorldPreference[craftItem.ItemId] : null);
                        UpdateItemPricing(itemPricing, craftItem);
                    }
                }
                else
                {
                    var ingredientPreference = craftItem.IngredientPreference;
                    if (ingredientPreference.Type is IngredientPreferenceType.Item or IngredientPreferenceType.Reduction)
                    {
                        if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                        {
                            uint? totalAmountAvailable = null;
                            var items = new Dictionary<uint, double>()
                            {
                                {ingredientPreference.LinkedItemId.Value, (double)ingredientPreference.LinkedItemQuantity * craftItem.QuantityNeeded}
                            };
                            if (ingredientPreference.Type is IngredientPreferenceType.Item && ingredientPreference.LinkedItem2Quantity != null &&
                                ingredientPreference.LinkedItem2Id != null)
                            {
                                items.TryAdd((uint)ingredientPreference.LinkedItem2Id, (double)ingredientPreference.LinkedItem2Quantity.Value * craftItem.QuantityNeeded);
                            }
                            if (ingredientPreference.Type is IngredientPreferenceType.Item && ingredientPreference.LinkedItem3Quantity != null &&
                                ingredientPreference.LinkedItem3Id != null)
                            {
                                items.TryAdd((uint)ingredientPreference.LinkedItem3Id, (double)ingredientPreference.LinkedItem3Quantity.Value * craftItem.QuantityNeeded);
                            }
                            for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                            {
                                var childItem = craftItem.ChildCrafts[index];
                                if(!items.ContainsKey(childItem.ItemId)) continue;
                                var amountNeeded = items[childItem.ItemId];
                                var totalCapable = childItem.QuantityReady;
                                //Service.Log.Log("amount craftable for ingredient " + craftItem.ItemId + " for output item is " + craftCapable);
                                if (totalCapable < amountNeeded)
                                {
                                    var key = (childItem.ItemId,childItem.Flags == InventoryItem.ItemFlags.HighQuality);
                                    craftItem.MissingIngredients.TryAdd(key, 0);
                                    craftItem.MissingIngredients[key] += (uint)amountNeeded - totalCapable;
                                }
                                if (totalAmountAvailable == null)
                                {
                                    totalAmountAvailable = totalCapable;
                                }
                                else
                                {
                                    totalAmountAvailable = Math.Min(totalCapable, totalAmountAvailable.Value);
                                }
                            }
                            craftItem.QuantityCanCraft = (uint)Math.Floor((double)(totalAmountAvailable ?? 0) / ingredientPreference.LinkedItemQuantity.Value);
                        }
                    }
                }
            }
            else
            {
                craftItem.QuantityNeededPreUpdate = craftItem.QuantityNeeded;
                craftItem.QuantityAvailable = 0;
                craftItem.QuantityReady = 0;
                //First generate quantity ready from the character sources, only use as much as we need
                var quantityReady = 0u;
                var quantityNeeded = craftItem.QuantityNeeded;
                if (craftListConfiguration.CharacterSources.ContainsKey(craftItem.ItemId))
                {
                    foreach (var characterSource in craftListConfiguration.CharacterSources[craftItem.ItemId])
                    {
                        if (quantityNeeded == 0)
                        {
                            break;
                        }
                        if (craftItem.Flags is InventoryItem.ItemFlags.HighQuality && !characterSource.IsHq)
                        {
                            continue;
                        }
                        var stillNeeded = characterSource.UseQuantity((int) quantityNeeded);
                        quantityReady += (quantityNeeded - stillNeeded);
                        quantityNeeded = stillNeeded;
                        //Service.Log.Log("Quantity needed for " + ItemId + ": " + quantityNeeded);
                        //Service.Log.Log("Still needed for " + ItemId + ": " + stillNeeded);
                    }
                }
                //Service.Log.Log("Quantity Ready for " + ItemId + ": " + quantityReady);
                craftItem.QuantityReady = quantityReady;
                
                var craftRetainerRetrieval = CraftRetainerRetrieval;
                if (CraftRetainerRetrievals.ContainsKey(craftItem.ItemId))
                {
                    craftRetainerRetrieval = CraftRetainerRetrievals[craftItem.ItemId];
                }
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                if (craftRetainerRetrieval is CraftRetainerRetrieval.Yes or CraftRetainerRetrieval.HQOnly)
                {
                    var quantityMissing = quantityNeeded;
                    //Service.Log.Log("quantity missing: " + quantityMissing);
                    if (quantityMissing != 0 && craftListConfiguration.ExternalSources.ContainsKey(craftItem.ItemId))
                    {
                        foreach (var externalSource in craftListConfiguration.ExternalSources[craftItem.ItemId])
                        {
                            if (quantityMissing == 0)
                            {
                                break;
                            }
                            if ((craftRetainerRetrieval is CraftRetainerRetrieval.HQOnly || craftItem.Flags is InventoryItem.ItemFlags.HighQuality) && !externalSource.IsHq) continue;
                            var stillNeeded = externalSource.UseQuantity((int)quantityMissing);
                            quantityAvailable += (quantityMissing - stillNeeded);
                            quantityMissing = stillNeeded;
                        }
                    }
                }

                craftItem.QuantityAvailable = quantityAvailable;

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)(Math.Min(craftItem.QuantityAvailable,craftItem.QuantityNeeded - craftItem.QuantityReady)));
                var ingredientPreference = craftItem.IngredientPreference;
                
                //This final figure represents the shortfall even when we include the character and external sources
                var quantityUnavailable = (uint)Math.Max(0,(int)craftItem.QuantityNeeded - (int)craftItem.QuantityReady - (int)craftItem.QuantityAvailable);
                if (spareIngredients.ContainsKey(craftItem.ItemId))
                {
                    var amountAvailable = (uint)Math.Max(0,Math.Min(quantityUnavailable, spareIngredients[craftItem.ItemId]));
                    quantityUnavailable -= amountAvailable;
                    spareIngredients[craftItem.ItemId] -= amountAvailable;
                }
                if (craftItem.Recipe != null && craftItem.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var totalAmountNeeded = quantityUnavailable;
                    craftItem.QuantityNeeded = totalAmountNeeded;
                    craftItem.ChildCrafts = CalculateChildCrafts(craftItem, null, craftItem).OrderByDescending(c => c.RecipeId).ToList();
                    foreach (var childCraft in craftItem.ChildCrafts)
                    {
                        var amountNeeded = childCraft.QuantityNeeded;

                        childCraft.QuantityNeeded = Math.Max(0, amountNeeded);
                        UpdateCraftItem(childCraft, craftListConfiguration, spareIngredients, cascadeCrafts, craftItem);
                        var childCraftQuantityReady = childCraft.QuantityReady;
                        if (cascadeCrafts)
                        {
                            childCraftQuantityReady += childCraft.QuantityCanCraft;
                        }
                        var craftCapable = (uint)Math.Ceiling(childCraftQuantityReady / (double)craftItem.Recipe.GetRecipeItemAmount(childCraft.ItemId));
                        if (childCraft.QuantityMissingOverall > 0)
                        {
                            var key = (childCraft.ItemId,childCraft.Flags == InventoryItem.ItemFlags.HighQuality);
                            craftItem.MissingIngredients.TryAdd(key, 0);
                            craftItem.MissingIngredients[key] += childCraft.QuantityMissingOverall;
                        }
                        if (totalCraftCapable == null)
                        {
                            totalCraftCapable = craftCapable;
                        }
                        else
                        {
                            totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                        }
                    }

                    craftItem.QuantityCanCraft = Math.Min(totalCraftCapable * craftItem.Yield  ?? 0, totalAmountNeeded * craftItem.Yield);
                    
                    //If the the last craft of an item would generate extra that goes unused, see if we can unuse that amount from a retainer
                    if (craftItem.Yield != 1)
                    {
                        var amountNeeded = totalAmountNeeded + craftItem.QuantityAvailable;
                        var amountMade = (uint)(Math.Ceiling(totalAmountNeeded / (double)craftItem.Yield) * craftItem.Yield) + craftItem.QuantityAvailable;
                        var unused = (uint)Math.Max(0, (int)amountMade - amountNeeded);
                        uint returned = 0;
                        if (unused > 0)
                        {
                            if (craftRetainerRetrieval is CraftRetainerRetrieval.Yes or CraftRetainerRetrieval.HQOnly)
                            {
                                if (craftListConfiguration.ExternalSources.ContainsKey(craftItem.ItemId))
                                {
                                    foreach (var externalSource in craftListConfiguration.ExternalSources[craftItem.ItemId])
                                    {
                                        if (unused == 0)
                                        {
                                            break;
                                        }
                                        if ((craftRetainerRetrieval is CraftRetainerRetrieval.HQOnly || craftItem.Flags is InventoryItem.ItemFlags.HighQuality) && !externalSource.IsHq) continue;
                                        var amountNotReturned = externalSource.ReturnQuantity((int)unused);
                                        returned += (unused - amountNotReturned);
                                        unused = amountNotReturned;
                                    }
                                }
                            }

                            if (unused > 0)
                            {
                                spareIngredients.TryAdd(craftItem.ItemId, 0);
                                spareIngredients[craftItem.ItemId] += unused;
                            }
                        }

                        craftItem.QuantityWillRetrieve -= returned;
                    }
                }
                else if (ingredientPreference.Type is IngredientPreferenceType.Item or IngredientPreferenceType.Reduction)
                {
                    if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                    {
                        if (parentItem != null && ingredientPreference.LinkedItemId == parentItem.ItemId)
                        {
                            //Stops recursion
                            return;
                        }
                        uint? totalCraftCapable = null;
                        var totalAmountNeeded = quantityUnavailable;
                        craftItem.QuantityNeeded = totalAmountNeeded;
                        
                        
                        var items = new Dictionary<uint, double>()
                        {
                            {ingredientPreference.LinkedItemId.Value, (double)ingredientPreference.LinkedItemQuantity * craftItem.QuantityNeeded}
                        };
                        if (ingredientPreference.Type is IngredientPreferenceType.Item && ingredientPreference.LinkedItem2Quantity != null &&
                            ingredientPreference.LinkedItem2Id != null)
                        {
                            items.TryAdd((uint)ingredientPreference.LinkedItem2Id, (double)ingredientPreference.LinkedItem2Quantity.Value * craftItem.QuantityNeeded);
                        }
                        if (ingredientPreference.Type is IngredientPreferenceType.Item && ingredientPreference.LinkedItem3Quantity != null &&
                            ingredientPreference.LinkedItem3Id != null)
                        {
                            items.TryAdd((uint)ingredientPreference.LinkedItem3Id, (double)ingredientPreference.LinkedItem3Quantity.Value * craftItem.QuantityNeeded);
                        }
                        
                        craftItem.ChildCrafts = CalculateChildCrafts(craftItem, null, craftItem).OrderByDescending(c => c.RecipeId).ToList();
                        foreach (var childCraft in craftItem.ChildCrafts)
                        {
                            if(!items.ContainsKey(childCraft.ItemId)) continue;
                            var amountNeeded = (uint)items[childCraft.ItemId];
                            childCraft.QuantityNeeded = Math.Max(0, amountNeeded);
                            UpdateCraftItem(childCraft, craftListConfiguration, spareIngredients, cascadeCrafts, craftItem);
                            var childCraftQuantityReady = childCraft.QuantityReady;
                            if (cascadeCrafts)
                            {
                                childCraftQuantityReady += childCraft.QuantityCanCraft;
                            }
                            var craftCapable = (uint)Math.Ceiling((double)childCraftQuantityReady);
                            if (craftCapable < amountNeeded)
                            {
                                var key = (childCraft.ItemId,childCraft.Flags == InventoryItem.ItemFlags.HighQuality);
                                craftItem.MissingIngredients.TryAdd(key, 0);
                                craftItem.MissingIngredients[key] += (uint)amountNeeded - craftCapable;
                            }

                            craftCapable /= ingredientPreference.LinkedItemQuantity.Value;
                            if (totalCraftCapable == null)
                            {
                                totalCraftCapable = craftCapable;
                            }
                            else
                            {
                                totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                            }
                        }

                        craftItem.QuantityCanCraft = Math.Min((uint)Math.Floor((double)(totalCraftCapable ?? 0)), totalAmountNeeded);
                    }
                }
                else if (Service.ExcelCache.HwdInspectionResults.ContainsKey(craftItem.ItemId))
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var inspectionMap = Service.ExcelCache.HwdInspectionResults[craftItem.ItemId];
                    var ingredientId = inspectionMap.Item1;
                    var amount = inspectionMap.Item2;
                    if (ingredientId == 0 || amount == 0)
                    {
                        return;
                    }

                    var amountNeeded = (uint)Math.Ceiling((double)quantityUnavailable / amount) * amount;
                    craftItem.QuantityNeeded = amountNeeded;

                    for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                    {
                        var childCraft = craftItem.ChildCrafts[index];
                        if (childCraft.ItemId == ingredientId)
                        {
                            childCraft.QuantityNeeded = Math.Max(0, amountNeeded);
                            UpdateCraftItem(childCraft, craftListConfiguration, spareIngredients, cascadeCrafts, craftItem);
                            var craftItemQuantityReady = childCraft.QuantityReady;
                            if (cascadeCrafts)
                            {
                                craftItemQuantityReady += childCraft.QuantityCanCraft;
                            }                            
                            var craftCapable =
                                (uint)Math.Ceiling(craftItemQuantityReady / (double)amount);
                            if (totalCraftCapable == null)
                            {
                                totalCraftCapable = craftCapable;
                            }
                            else
                            {
                                totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                            }
                        }
                    }
                    craftItem.QuantityCanCraft = Math.Min(totalCraftCapable * craftItem.Yield  ?? 0, craftItem.QuantityNeeded - craftItem.QuantityReady);
                }
                else if (craftItem.IngredientPreference.Type == IngredientPreferenceType.Marketboard)
                {
                    if (craftListConfiguration.CraftPricer != null)
                    {
                        var totalAmountNeeded = quantityUnavailable;
                        craftItem.QuantityNeeded = totalAmountNeeded;
                        var itemPricing = craftListConfiguration.GetItemPricing(craftItem.ItemId, MarketItemWorldPreference.ContainsKey(craftItem.ItemId) ? MarketItemWorldPreference[craftItem.ItemId] : null);
                        UpdateItemPricing(itemPricing, craftItem);
                    }
                }
                else
                {
                    var totalAmountNeeded = quantityUnavailable;
                    craftItem.QuantityNeeded = totalAmountNeeded;
                    craftItem.ChildCrafts = CalculateChildCrafts(craftItem, null, craftItem).OrderByDescending(c => c.RecipeId).ToList();
                    for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                    {
                        var childCraft = craftItem.ChildCrafts[index];
                        UpdateCraftItem(childCraft, craftListConfiguration, spareIngredients, cascadeCrafts, craftItem);
                        if (childCraft.QuantityMissingOverall > 0)
                        {
                            var key = (childCraft.ItemId,childCraft.Flags == InventoryItem.ItemFlags.HighQuality);
                            craftItem.MissingIngredients.TryAdd(key, 0);
                            craftItem.MissingIngredients[key] += (uint)childCraft.QuantityMissingOverall;
                        }
                    }
                }
            }
        }
        
        public void MarkCrafted(uint itemId, InventoryItem.ItemFlags itemFlags, uint quantity)
        {
            if (GetFlattenedMaterials().Any(c =>
                !c.IsOutputItem && c.ItemId == itemId && c.Flags == itemFlags && c.QuantityMissingOverall != 0))
            {
                return;
            }

            var hqRequired = (GetHQRequired(itemId) ?? HQRequired);
            if (hqRequired && !itemFlags.HasFlag(InventoryItem.ItemFlags.HighQuality))
            {
                return;
            }
            if (CraftItems.Any(c => c.ItemId == itemId && c.QuantityRequired != 0))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.QuantityRequired != 0);
                craftItem.RemoveQuantity(quantity);
            }
            if (CraftItems.Any(c => c.ItemId == itemId && c.QuantityRequired <= 0) && CraftCompletionMode == CraftCompletionMode.Delete)
            {
                RemoveCraftItem(itemId);
            }
            BeenGenerated = false;
            NeedsRefresh = true;
        }

        public void Update(CraftListConfiguration craftListConfiguration, CraftPricer? craftPricer = null, bool cascadeCrafts = false)
        {
            var spareIngredients = new Dictionary<uint, double>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                //Service.Log.Log("Calculating items for " + craftItem.Item.Name);
                UpdateCraftItem(craftItem, craftListConfiguration, spareIngredients, cascadeCrafts, craftItem);
            }

            GetFlattenedMergedMaterials(true);

            BeenUpdated = true;
            NeedsRefresh = false;
        }
        
        public List<uint> GetMaterialsList()
        {
            var list = GetFlattenedMaterials();
            return list.Select(c => c.ItemId).Distinct().ToList();
        }

        public List<CraftItem> GetFlattenedMaterials(uint depth = 0)
        {
            var list = new List<CraftItem>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                craftItem.Depth = depth;
                list.Add(craftItem);
                var items = craftItem.GetFlattenedMaterials(depth + 1);
                for (var i = 0; i < items.Count; i++)
                {
                    var material = items[i];
                    list.Add(material);
                }
            }

            return list;
        }

        private List<CraftItem>? _flattenedMergedMaterials;

        public List<CraftItem> GetFlattenedMergedMaterials(bool clear = false)
        {
            if (_flattenedMergedMaterials == null || clear)
            {
                var list = GetFlattenedMaterials();
                _flattenedMergedMaterials = list.GroupBy(c => new { c.ItemId, c.Flags, c.Phase, c.IsOutputItem }).Select(c => c.Sum())
                    .OrderBy(c => c.Depth).ToList();
            }

            return _flattenedMergedMaterials;
        }

        public Dictionary<uint, uint> GetRequiredMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityRequired;
            }

            return dictionary;
        }
        
        public void UpdatePricing(Dictionary<uint, List<CraftPriceSource>> prices)
        {
            foreach (var item in CraftItems)
            {
                UpdateItemPricing(prices, item);
            }
        }
        
        public void UpdateItemPricing(List<CraftPriceSource> priceSources, CraftItem craftItem)
        {
            if (craftItem.Item.CanBeTraded && craftItem.QuantityNeeded != 0)
            {
                var availablePrices = new List<CraftPriceSource>();

                var craftItemNeeded = (int)craftItem.QuantityNeeded;
                foreach (var price in priceSources)
                {
                    if (price.Left != 0 && craftItemNeeded != 0)
                    {
                        var remaining = price.UseQuantity(craftItemNeeded);
                        var amountAvailable = (uint)craftItemNeeded - remaining;
                        craftItemNeeded = (int)remaining;
                        availablePrices.Add(new CraftPriceSource(price, amountAvailable));
                    }
                }

                uint marketAvailable = 0;
                long marketTotalPrice = 0;
                foreach (var craftPrice in availablePrices)
                {
                    marketAvailable += craftPrice.Quantity;
                    marketTotalPrice += craftPrice.UnitPrice * craftPrice.Quantity;
                }

                craftItem.MarketTotalPrice = (uint?)marketTotalPrice;
                craftItem.MarketAvailable = marketAvailable;
                uint extra = 0;
                if (MarketItemPriceOverride.ContainsKey(craftItem.ItemId))
                {
                    var missing = craftItem.QuantityNeeded - marketAvailable;
                    missing *= MarketItemPriceOverride[craftItem.ItemId];
                    extra += missing;
                    craftItem.MarketTotalPrice += extra;
                }
                craftItem.CraftPrices = availablePrices;
                craftItem.ChildCrafts.Add(new CraftItem(1, InventoryItem.ItemFlags.None, (uint)marketTotalPrice + extra));
            }
        }

        public void UpdateItemPricing(Dictionary<uint, List<CraftPriceSource>> priceSources, CraftItem craftItem)
        {
            if (craftItem.Item.CanBeTraded && craftItem.QuantityNeeded != 0)
            {
                if (priceSources.TryGetValue(craftItem.ItemId, out var prices))
                {
                    var availablePrices = new List<CraftPriceSource>();

                    var craftItemNeeded = (int) craftItem.QuantityNeeded;
                    foreach (var price in prices)
                    {
                        if (price.Left != 0 && craftItemNeeded != 0)
                        {
                            var remaining = price.UseQuantity(craftItemNeeded);
                            var amountAvailable = (uint)craftItemNeeded - remaining;
                            craftItemNeeded = (int) remaining;
                            availablePrices.Add(new CraftPriceSource(price, amountAvailable));
                        }
                    }

                    uint marketAvailable = 0;
                    long marketTotalPrice = 0;
                    foreach (var craftPrice in availablePrices)
                    {
                        marketAvailable += craftPrice.Quantity;
                        marketTotalPrice += craftPrice.UnitPrice * craftPrice.Quantity;
                    }

                    craftItem.MarketTotalPrice = (uint?) marketTotalPrice;
                    craftItem.MarketAvailable = marketAvailable;
                    craftItem.CraftPrices = availablePrices;
                    craftItem.ChildCrafts.Add(new CraftItem(1,InventoryItem.ItemFlags.None, (uint)marketTotalPrice));
                }
            }

            foreach (var childCraft in craftItem.ChildCrafts)
            {
                UpdateItemPricing(priceSources, childCraft);
            }
        }
        
        public Dictionary<string, uint> GetRequiredMaterialsListNamed()
        {
            return GetRequiredMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
        }

        public Dictionary<uint, uint> GetAvailableMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityAvailable;
            }

            return dictionary;
        }

        public Dictionary<string, uint> GetAvailableMaterialsListNamed()
        {
            return GetAvailableMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
        }

        public Dictionary<uint, uint> GetReadyMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityReady;
            }

            return dictionary;
        }
        
        public Dictionary<string, uint> GetReadyMaterialsListNamed()
        {
            return GetReadyMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
        }

        public Dictionary<uint, uint> GetMissingMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityMissingOverall;
            }

            return dictionary;
        }
        
        public Dictionary<string, uint> GetMissingMaterialsListNamed()
        {
            return GetMissingMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
        }

        public Dictionary<uint, uint> GetQuantityNeededList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityNeeded;
            }

            return dictionary;
        }
        
        public Dictionary<string, uint> GetQuantityNeededListNamed()
        {
            return GetQuantityNeededList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
        }

        public Dictionary<uint, uint> GetQuantityCanCraftList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityCanCraft;
            }

            return dictionary;
        }
        
        public Dictionary<string, uint> GetQuantityCanCraftListNamed()
        {
            return GetQuantityCanCraftList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
        }

        public Dictionary<(uint, bool), uint> GetQuantityToRetrieveList()
        {
            var dictionary = new Dictionary<(uint, bool), uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey((item.ItemId, item.IsOutputItem)))
                {
                    dictionary.Add((item.ItemId, item.IsOutputItem), 0);
                }

                dictionary[(item.ItemId, item.IsOutputItem)] += item.QuantityWillRetrieve;
            }

            return dictionary;
        }

        public CraftItem? GetItemById(uint itemId, bool isHq, bool canBeHq)
        {
            if ((GetHQRequired(itemId) ?? HQRequired) && !isHq && canBeHq)
            {
                return null;
            }

            var craftItems = GetFlattenedMergedMaterials().Where(c => c.ItemId == itemId).ToList();
            return craftItems.Count != 0 ? craftItems.First() : null;
        }

        public (Vector4, string) GetNextStep(CraftItem item)
        {
            if (item.NextStep == null)
            {
                item.NextStep = CalculateNextStep(item);
            }

            return item.NextStep.Value;
        }
        
        private (Vector4, string) CalculateNextStep(CraftItem item)
        {
            var unavailable = Math.Max(0, (int)item.QuantityMissingOverall);
            
            if (RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst)
            {
                var retrieve = (int)item.QuantityWillRetrieve;
                if (retrieve != 0)
                {
                    return (ImGuiColors.DalamudOrange, "Retrieve " + retrieve);
                }
            }

            var ingredientPreference = GetIngredientPreference(item.ItemId);

            if (ingredientPreference == null)
            {
                foreach (var defaultPreference in IngredientPreferenceTypeOrder)
                {
                    if (item.Item.GetIngredientPreference(defaultPreference.Item1, defaultPreference.Item2,
                            out ingredientPreference))
                    {
                        break;
                    }
                }
            }

            if (ingredientPreference != null)
            {
                string nextStepString = "";
                Vector4 stepColour = ImGuiColors.DalamudYellow;
                bool escapeSwitch = false; //TODO: Come up with a new way of doing this entire column
                if (unavailable != 0)
                {
            
                    switch (ingredientPreference.Type)
                    {
                        case IngredientPreferenceType.Botany:
                        case IngredientPreferenceType.Mining:
                            nextStepString = "Gather " + unavailable;
                            break;
                        case IngredientPreferenceType.Buy:
                            nextStepString = "Buy " + unavailable + " (Vendor)";
                            break;
                        case IngredientPreferenceType.HouseVendor:
                            nextStepString = "Buy " + unavailable + " (House Vendor)";
                            break;
                        case IngredientPreferenceType.Marketboard:
                            if (item.MarketAvailable == 0 && MarketItemPriceOverride.ContainsKey(item.ItemId))
                            {
                                nextStepString = "No MB pricing, overridden cost for " + item.MarketTotalPrice;
                            }
                            else
                            {
                                nextStepString = "Buy " + item.MarketAvailable + " (MB) for " + item.MarketTotalPrice;
                                var missing = unavailable - item.MarketAvailable;
                                if (missing != 0)
                                {
                                    nextStepString += " (missing: " + (unavailable - item.MarketAvailable) + ")";
                                }
                            }

                            break;
                        case IngredientPreferenceType.Crafting:
                            if ((int)item.QuantityWillRetrieve != 0)
                            {
                                escapeSwitch = true;
                                break;
                            }
                            if (item.QuantityCanCraft >= unavailable)
                            {
                                if (item.QuantityCanCraft != 0)
                                {
                                    if (item.Item.CanBeCrafted)
                                    {
                                        nextStepString = "Craft " + item.CraftOperationsRequired;
                                        if (item.Yield != 1)
                                        {
                                            nextStepString += " (" + item.Yield + ")";
                                        }
                                        stepColour = ImGuiColors.ParsedBlue;
                                    }
                                }
                            }
                            else
                            {
                                //Special case
                                stepColour = ImGuiColors.DalamudRed;
                                nextStepString = "Ingredients Missing";
                            }

                            break;
                        case IngredientPreferenceType.Fishing:
                            nextStepString = "Fish for " + unavailable;
                            break;
                        case IngredientPreferenceType.Item:
                            if (ingredientPreference.LinkedItemId != null &&
                                ingredientPreference.LinkedItemQuantity != null)
                            {
                                if (item.QuantityCanCraft >= unavailable)
                                {
                                    if (item.QuantityCanCraft != 0)
                                    {
                                        var linkedItem = Service.ExcelCache.GetItemExSheet()
                                            .GetRow(item.IngredientPreference.ItemId);
                                        nextStepString = "Purchase " + item.QuantityCanCraft + " " + linkedItem?.NameString ?? "Unknown";
                                        stepColour = ImGuiColors.DalamudYellow;
                                    }
                                }
                                else
                                {
                                    stepColour = ImGuiColors.DalamudRed;
                                    nextStepString = "Ingredients Missing";
                                }
                                break;
                            }

                            nextStepString = "No item selected";
                            break;
                        case IngredientPreferenceType.Venture:
                            nextStepString = "Venture: " + item.Item.RetainerFixedTaskNames;
                            ;
                            break;
                        case IngredientPreferenceType.ExplorationVenture:
                            nextStepString = "Venture: " + item.Item.RetainerRandomTaskNames;
                            ;
                            break;
                        case IngredientPreferenceType.Empty:
                            nextStepString = "Do Nothing";
                            ;
                            break;
                        case IngredientPreferenceType.Gardening:
                            nextStepString = "Harvest(Gardening): " + unavailable;
                            break;
                        case IngredientPreferenceType.ResourceInspection:
                            nextStepString = "Resource Inspection: " + unavailable;
                            break;
                        case IngredientPreferenceType.Reduction:
                            if (ingredientPreference.LinkedItemId != null &&
                                ingredientPreference.LinkedItemQuantity != null)
                            {
                                if (item.QuantityCanCraft >= unavailable)
                                {
                                    if (item.QuantityCanCraft != 0)
                                    {
                                        var linkedItem = Service.ExcelCache.GetItemExSheet()
                                            .GetRow(item.IngredientPreference.LinkedItemId.Value);
                                        nextStepString = "Reduce " + item.QuantityCanCraft + " " + linkedItem?.NameString ?? "Unknown";
                                        stepColour = ImGuiColors.DalamudYellow;
                                    }
                                }
                                else
                                {
                                    stepColour = ImGuiColors.DalamudRed;
                                    nextStepString = "Ingredients Missing";
                                }
                                break;
                            }                            
                            break;
                        case IngredientPreferenceType.Desynthesis:
                            nextStepString = "Desynthesize " + unavailable;
                            break;
                        case IngredientPreferenceType.Mobs:
                            nextStepString = "Hunt " + unavailable;
                            break;
                    }

                    if (nextStepString != "" && !escapeSwitch)
                    {
                        return (stepColour, nextStepString);
                    }
                }
            }

            var canCraft = item.QuantityCanCraft;
            if (canCraft != 0 && (int)item.QuantityWillRetrieve == 0)
            {
                return (ImGuiColors.ParsedBlue, "Craft " + (uint)Math.Ceiling((double)canCraft / item.Yield));
            }

            if (RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveLast)
            {
                var retrieve = (int)item.QuantityWillRetrieve;
                if (retrieve != 0)
                {
                    return (ImGuiColors.DalamudOrange, "Retrieve " + retrieve);
                }
            }
            if (unavailable != 0)
            {
                if (item.Item.ObtainedGathering)
                {
                    return (ImGuiColors.DalamudYellow, "Gather " + unavailable);
                }
                else if (item.Item.ObtainedGil)
                {
                    return (ImGuiColors.DalamudYellow, "Buy " + unavailable);

                }
                return (ImGuiColors.DalamudRed, "Missing " + unavailable);
            }


            if (item.IsOutputItem)
            {
                if (item.IsCompleted)
                {
                    return (ImGuiColors.HealerGreen, "Completed");
                }
                else
                {
                    return (ImGuiColors.DalamudWhite, "Waiting");
                }
            }
            return (ImGuiColors.HealerGreen, "Done");
        }
        
        public CraftList? Clone()
        {
            var clone = this.Copy();
            return clone;
        }

    }
}