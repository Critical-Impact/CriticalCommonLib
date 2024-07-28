using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
using CriticalCommonLib.Models.ItemSources;
using CriticalCommonLib.Time;
using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Data.Parsing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Model;
using IItemSource = CriticalCommonLib.Models.ItemSources.IItemSource;

namespace CriticalCommonLib.Sheets
{
    public class ItemEx : Item, IItem
    {
        //Hardcoded fake items, this'll probably come back to bite me in the ass
        public const uint FreeCompanyCreditItemId = 80;
        
        public IEnumerable<(LazyRow<ItemEx>,uint)> _specialShopCosts = null!;
        public IEnumerable<(LazyRow<ItemEx>,uint)> _specialShopRewards = null!;
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            var specialshopCurrencies = new List<(LazyRow<ItemEx>,uint)>();
            var specialShopRewards = new List<(LazyRow<ItemEx>,uint)>();
            if (Service.ExcelCache.SpecialShopItemRewardCostLookup.ContainsKey(RowId))
            {
                foreach (var cost in Service.ExcelCache.SpecialShopItemRewardCostLookup[RowId])
                {
                    specialshopCurrencies.Add((new LazyRow<ItemEx>(gameData, cost.Item2, language), cost.Item1));
                }
            }
            if (Service.ExcelCache.SpecialShopItemCostRewardLookup.ContainsKey(RowId))
            {
                foreach (var rewardSet in Service.ExcelCache.SpecialShopItemCostRewardLookup[RowId])
                {
                    specialShopRewards.Add((new LazyRow<ItemEx>(gameData, rewardSet.Item2, language), rewardSet.Item1));
                }
            }

            _specialShopCosts = specialshopCurrencies;
            _specialShopRewards = specialShopRewards;
            
            _gatheringItems =
                new Lazy<List<GatheringItemEx>>(CalculateGatheringItems, LazyThreadSafetyMode.PublicationOnly);
            ClassJobCategoryEx = new LazyRow<ClassJobCategoryEx>(gameData, ClassJobCategory.Row, language);

            GatheringItems = new LazyRelated<GatheringItemEx, ItemEx>(this, gameData, language,
                exes =>
                {
                    return exes.Where(c => c.Item != 0).GroupBy(c => c.Item).ToDictionary(c => (uint)c.Key, c => c.Select(c => c.RowId).ToList());
                }, "GatheringItems");

            GatheringTypes = new LazyRelated<GatheringTypeEx, ItemEx>(this, gameData, language, types =>
            {
                Dictionary<uint, HashSet<uint>> gatheringTypes = new Dictionary<uint, HashSet<uint>>();
                foreach (var type in types)
                {
                    foreach (var basePoint in type.GatheringPointBases)
                    {
                        var gatheringItems = basePoint.Value?.Items;
                        if (gatheringItems != null)
                        {
                            foreach (var item in gatheringItems)
                            {
                                if (item.Value != null)
                                {
                                    var itemId = (uint)item.Value.Item;
                                    gatheringTypes.TryAdd(itemId, new HashSet<uint>());
                                    gatheringTypes[itemId].Add(type.RowId);
                                }
                            }
                        }

                    }
                }

                return gatheringTypes.ToDictionary(c => c.Key, c => c.Value.ToList());
            }, "GT");
        }

        public LazyRelated<GatheringTypeEx, ItemEx> GatheringTypes { get; set; } = null!;

        public LazyRow<ClassJobCategoryEx> ClassJobCategoryEx = null!;
        public LazyRelated<GatheringItemEx, ItemEx> GatheringItems = null!;
        public uint CabinetCategory => Service.ExcelCache.ItemToCabinetCategory.ContainsKey(RowId) ? Service.ExcelCache.ItemToCabinetCategory[RowId] : 0;

        public new ushort Icon
        {
            get
            {
                if (RowId == FreeCompanyCreditItemId)
                {
                    return Icons.FreeCompanyCreditIcon;
                }

                return base.Icon;
            }
        }

        public string GarlandToolsId
        {
            get
            {
                if (RowId == FreeCompanyCreditItemId)
                {
                    return "fccredit";
                }

                return RowId.ToString();
            }
        }

        public string NameString
        {
            get
            {
                if (_nameString == null)
                {
                    if (RowId == FreeCompanyCreditItemId)
                    {
                        _nameString = "Company Credit";
                    }
                    else
                    {
                        _nameString = Name.ToDalamudString().ToString();
                    }
                }

                return _nameString;
            }
        }
        
        private string? _nameString;
        
        private string? _searchString;
        public string SearchString
        {
            get
            {
                if (_searchString == null)
                {
                    _searchString = NameString.ToParseable();
                }

                return _searchString;
            }
        }
        
        public string DescriptionString
        {
            get
            {
                if (_descriptionString == null)
                {
                    if (RowId == FreeCompanyCreditItemId)
                    {
                        _descriptionString = "Company Credit is a currency passively earned by Free Company members as they complete various activities in the game. They are spent on items and FC actions with the OIC Quartermaster of your aligned Grand Company. They can also be used at the Resident Caretaker in residential districts or the Mammet Voyager 004A in the Free Company Workshop.";
                    }
                    else
                    {
                        _descriptionString = Description.ToDalamudString().ToString();
                    }
                }

                return _descriptionString;
            }
        }

        private string? _descriptionString;

        private List<GatheringItemEx> CalculateGatheringItems()
        {
            if (Service.ExcelCache.ItemGatheringItem.ContainsKey(RowId))
            {
                return Service.ExcelCache.ItemGatheringItem[RowId]
                    .Select(c => Service.ExcelCache.GetGatheringItemExSheet().GetRow(c)).Where(c => c != null)
                    .Select(c => c!).ToList();
                ;
            }

            return new List<GatheringItemEx>();
        }
        
        public List<ItemEx> GetSharedModels()
        {
            if (this.GetPrimaryModelKeyString() == "")
            {
                return new List<ItemEx>();
            }
            return Service.ExcelCache.AllItems.Where(c => c.Value.GetPrimaryModelKeyString() != "" && c.Value.GetPrimaryModelKeyString() == GetPrimaryModelKeyString() && c.Key != RowId).Select(c => c.Value).ToList();
        }

        private List<MobDropEx>? _mobDrops;
        public List<MobDropEx> MobDrops
        {
            get
            {
                if (_mobDrops == null)
                {
                    _mobDrops = Service.ExcelCache.GetMobDrops(RowId);
                    if (_mobDrops == null)
                    {
                        _mobDrops = new List<MobDropEx>();
                    }
                }

                return _mobDrops;
            }
        }
        
        private List<FateItem>? _fateItems;
        public List<FateItem> FateItems
        {
            get
            {
                if (_fateItems == null)
                {
                    _fateItems = Service.ExcelCache.GetFateItems(RowId);
                    if (_fateItems == null)
                    {
                        _fateItems = new List<FateItem>();
                    }
                }

                return _fateItems;
            }
        }

        public bool HasMobDrops()
        {
            return Service.ExcelCache.HasMobDrops(RowId);
        }
        
        public Quad GetPrimaryModelKey()
        {
            return (Quad)ModelMain;
        }

        public string GetPrimaryModelKeyString()
        {
            var characterType = GetModelCharacterType();
            if (characterType != 0 && !StaticData.NoModelCategories.Contains(ItemUICategory.Row))
            {
                if (Rarity != 7 && EquipSlotCategoryEx != null)
                {

                    var sEquipSlot = EquipSlotCategoryEx.PossibleSlots.First();
                    if (!StaticData.ModelHelpers.TryGetValue((int)sEquipSlot, out var helper))
                        return "";
                    if (helper == null)
                        return "";
                    var key = GetPrimaryModelKey();
                    var modelKey = string.Format(helper.ModelFileFormat,key.A, key.B, key.C, key.D, characterType, (uint)sEquipSlot);
                    return modelKey;
                }
            }

            return "";
        }

        public int GetModelCharacterType() {
            switch (EquipRestriction) {
                case 0: return 0; // Not equippable
                case 1: return 101; // Unrestricted, default to male hyur
                case 2: return 101; // Any male
                case 3: return 201; // Any female
                case 4: return 101; // Hyur male
                case 5: return 201; // Hyur female
                case 6: return 501; // Elezen male
                case 7: return 601; // Elezen female
                case 8: return 1101; // Lalafell male
                case 9: return 1201; // Lalafell female
                case 10: return 701; // Miqo'te male
                case 11: return 801; // Miqo'te female
                case 12: return 901; // Roegadyn male
                case 13: return 1001; // Roegadyn female
                case 14: return 1301; // Au Ra male
                case 15: return 1401; // Au Ra female
                case 16: return 1501; // Hrothgar male
                case 17: return 1801; // Viera female
                case 18: return 1701; // Viera male
                case 19: return 1601; // Hrothgar female
                default:
                    throw new NotImplementedException();
            }
        }

        private HashSet<GatheringSource>? _gatheringSources;
        public HashSet<GatheringSource> GetGatheringSources()
        {
            if (_gatheringSources != null)
            {
                return _gatheringSources;
            }
            var sources = new Dictionary<(uint, uint, uint),GatheringSource>();
            foreach (var gatheringItem in _gatheringItems.Value)
            {
                var level = gatheringItem.GatheringItemLevel.Value;
                if (level != null)
                {
                    foreach (var point in gatheringItem.GatheringItemPoints.Value)
                    {
                        var gatheringBase = point.GatheringPointBaseEx.Value;
                        if(gatheringBase != null)
                        {
                            foreach (var gatheringPoint in gatheringBase.GatheringPoints.Value)
                            {
                                var type = gatheringBase.GatheringType.Value;
                                var territoryType = gatheringPoint.TerritoryType.Value;
                                var placeName = gatheringPoint.PlaceName.Value;
                                if (type != null && territoryType != null && placeName != null)
                                {
                                    var key = (type.RowId, territoryType.RowId, placeName.RowId);
                                    sources.TryAdd(key, new GatheringSource(type, level, territoryType, placeName));
                                }
                            }
                        }
                    }
                }
            }

            _gatheringSources = sources.Select(c => c.Value).ToHashSet();
            return _gatheringSources;
        }
        
        private Lazy<List<GatheringItemEx>> _gatheringItems = null!;

        private HashSet<uint> CalculateGatheringTypes()
        {
            var gatheringTypes = new HashSet<uint>();
            foreach (var gatheringItem in GatheringItems)
            {
                var bases = gatheringItem.Value?.GatheringPointBases;
                if (bases != null)
                {
                    foreach(var basePoint in bases)
                    {
                        if (basePoint.Value != null)
                        {
                            gatheringTypes.Add(basePoint.Value.GatheringType.Row);
                        }
                    }
                }
            }
            return gatheringTypes;
        }

        public List<ItemSupplement>? SupplementalUseData => Service.ExcelCache.GetSupplementUses(RowId);
        public List<ItemSupplement>? SupplementalSourceData => Service.ExcelCache.GetSupplementSources(RowId);
        public List<RetainerVentureItemEx>? SupplementalRetainerVentureData => Service.ExcelCache.GetItemRetainerVentures(RowId);

        private List<IItemSource>? _uses;

        public List<IItemSource> Uses
        {
            get
            {
                if (_uses != null)
                {
                    return _uses;
                }
                List<IItemSource> uses = new List<IItemSource>();
                if (SpentSpecialShop)
                {
                    foreach (var specialShopCurrency in _specialShopRewards)
                    {
                        if (specialShopCurrency.Item1.Value != null)
                        {
                            uses.Add( new ItemSource(specialShopCurrency.Item1.Value.NameString, specialShopCurrency.Item1.Value.Icon, specialShopCurrency.Item1.Row, specialShopCurrency.Item2));
                        }
                    }
                }

                if (Service.ExcelCache.ItemToDailySupplyItem.TryGetValue(RowId, out var supplyId))
                {
                            
                    uses.Add( new GrandCompanySupplySource( RowId, supplyId));
                }
                
                if (RowId == FreeCompanyCreditItemId)
                {
                    var fccShops = Service.ExcelCache.ShopCollection?.Where(c => c is FccShopEx);
                    if (fccShops != null)
                    {
                        foreach (var fccShop in fccShops)
                        {
                            foreach (var shopListing in fccShop.ShopListings)
                            {
                                foreach (var item in shopListing.Rewards)
                                {
                                    if (item.ItemEx.Value != null)
                                    {
                                        if (shopListing.Costs.Any())
                                        {
                                            var cost = shopListing.Costs.First();
                                            uses.Add(new ItemSource(item.ItemEx.Value.NameString,
                                                item.ItemEx.Value.Icon, item.ItemEx.Row,
                                                (uint?)cost.Count));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (SupplementalUseData != null)
                {
                    var supplementalUses = SupplementalUseData.Where(c => c.SourceItemId == RowId);
                    
                    foreach (var item in supplementalUses)
                    {
                        if (item.ItemSupplementSource == ItemSupplementSource.Desynth)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.ItemId);
                            if (itemEx != null)
                            {
                                uses.Add(
                                    new ItemSource("Desynthesis - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                        else if (item.ItemSupplementSource == ItemSupplementSource.Gardening)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.ItemId);
                            if (itemEx != null)
                            {
                                uses.Add(new ItemSource("Gardening - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                        else if (item.ItemSupplementSource == ItemSupplementSource.Loot)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.ItemId);
                            if (itemEx != null)
                            {
                                uses.Add(new ItemSource("Loot - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                        else if (item.ItemSupplementSource == ItemSupplementSource.Reduction)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.ItemId);
                            if (itemEx != null)
                            {
                                uses.Add(new ItemSource("Reduction - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                    }
                }

                _uses = uses;
                return _uses;
            }
        }

        private List<IItemSource>? _sources;
        public List<IItemSource> Sources
        {
            get
            {
                if (_sources != null)
                {
                    return _sources;
                }
                
                List<IItemSource> sources = new List<IItemSource>();
                if (ObtainedGil)
                {
                    sources.Add(new ItemSource("Gil", Service.ExcelCache.GetItemExSheet().GetRow(1)!.Icon, 1, BuyFromVendorPrice));
                }
                
                if (ObtainedCompanyScrip)
                {
                    foreach (var item in GcScripShopItems)
                    {
                        var sealItem = Service.ExcelCache.GetItemExSheet().GetRow(item.GCShopEx.Value!.GrandCompany.Row + 19);
                        sources.Add(new ItemSource(sealItem!.NameString,sealItem!.Icon, sealItem.RowId, item.CostGCSeals));
                    }
                }

                if (ObtainedCompanyCredits)
                {
                    sources.Add(new ItemSource("Company Credit",Service.ExcelCache.GetItemExSheet().GetRow(FreeCompanyCreditItemId)!.Icon, FreeCompanyCreditItemId));
                }

                if (IsItemAvailableAtHiddenNode || IsItemAvailableAtTimedNode || IsItemAvailableAtEphemeralNode || ObtainedGathering)
                {
                    foreach (var gatheringType in GatheringTypes)
                    {
                        
                        if (IsItemAvailableAtHiddenNode)
                        {
                            sources.Add(new ItemSource("Hidden Item - " + gatheringType.Value!.FormattedName,
                                (uint)gatheringType.Value.IconOff, null));
                        }
                        else if (IsItemAvailableAtTimedNode)
                        {
                            sources.Add(new ItemSource("Timed Node - " + gatheringType.Value!.FormattedName,
                                (uint)gatheringType.Value.IconOff, null));
                        }
                        else if (IsItemAvailableAtEphemeralNode)
                        {
                            sources.Add(new ItemSource("Ephemeral Node - " + gatheringType.Value!.FormattedName,
                                (uint)gatheringType.Value.IconOff, null));
                        }
                        else if (ObtainedGathering)
                        {
                            sources.Add(new ItemSource(gatheringType.Value!.FormattedName,
                                (uint)gatheringType.Value.IconMain, null));
                        }

                    }
                }

                if (ObtainedFishing)
                {
                    sources.Add(new ItemSource("Fishing", Icons.FishingIcon, null));
                }
                if (ObtainedVenture && RetainerTasks != null)
                {
                    foreach (var retainerTaskNormal in RetainerTasks)
                    {
                        sources.Add(new VentureSource(retainerTaskNormal));
                    }
                }

                foreach (var specialShopCurrency in _specialShopCosts)
                {
                    if (specialShopCurrency.Item1.Value != null)
                    {
                        sources.Add( new ItemSource(specialShopCurrency.Item1.Value.NameString, specialShopCurrency.Item1.Value.Icon, specialShopCurrency.Item1.Value.RowId, specialShopCurrency.Item2));
                    }
                }

                if (ObtainedCalamitySalvager)
                {
                    sources.Add(new ItemSource("Calamity Salvager", Icons.CalamitySalvagerBag, null));
                }

                if (SupplementalSourceData != null)
                {
                    var supplementalSources = SupplementalSourceData.Where(c => c.ItemId == RowId);
                    
                    foreach (var item in supplementalSources)
                    {
                        if (item.ItemSupplementSource == ItemSupplementSource.Desynth)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.SourceItemId);
                            if (itemEx != null)
                            {
                                sources.Add(
                                    new ItemSource("Desynthesis - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                        else if (item.ItemSupplementSource == ItemSupplementSource.Gardening)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.SourceItemId);
                            if (itemEx != null)
                            {
                                sources.Add(new ItemSource("Gardening - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                        else if (item.ItemSupplementSource == ItemSupplementSource.Loot)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.SourceItemId);
                            if (itemEx != null)
                            {
                                sources.Add(new ItemSource("Loot - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                        else if (item.ItemSupplementSource == ItemSupplementSource.Reduction)
                        {
                            var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item.SourceItemId);
                            if (itemEx != null)
                            {
                                sources.Add(new ItemSource("Reduction - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                            }
                        }
                    }
                }
                
                var mobDrops = Service.ExcelCache.GetMobDrops(RowId);
                if (mobDrops != null)
                {
                    sources.Add(new ItemSource("Dropped by Mobs", Icons.MobIcon, null));
                }
                
                var fateItems = Service.ExcelCache.GetFateItems(RowId);
                if (fateItems != null)
                {
                    foreach (var fateItem in fateItems)
                    {
                        sources.Add(new FateSource(fateItem.FateId));
                    }
                }
                
                var seenDuties = new HashSet<uint>();

                var dungeonChestItems = Service.ExcelCache.GetDungeonChestItems(RowId);
                if (dungeonChestItems != null)
                {
                    foreach (var dungeonChestId in dungeonChestItems.Select(c => c.ChestId).ToHashSet())
                    {
                        var dungeonChest = Service.ExcelCache.GetDungeonChest(dungeonChestId);
                        if (dungeonChest != null)
                        {
                            var contentFinderConditionId = dungeonChest.ContentFinderConditionId;
                            if (seenDuties.Contains(contentFinderConditionId))
                            {
                                continue;
                            }
                
                            seenDuties.Add(contentFinderConditionId);
                            var duty = Service.ExcelCache.GetContentFinderConditionExSheet().GetRow(contentFinderConditionId);
                            if (duty != null)
                            {
                                sources.Add(new DutySource("Duty - " + duty.Name.ToString(), Icons.DutyIcon, duty.RowId));
                            }
                
                        }
                    }
                }

                var dungeonBossDrops = Service.ExcelCache.GetDungeonBossDrops(RowId);
                if (dungeonBossDrops != null)
                {
                    foreach (var contentFinderConditionId in dungeonBossDrops.Select(c => c.ContentFinderConditionId).ToHashSet())
                    {
                        var duty = Service.ExcelCache.GetContentFinderConditionExSheet().GetRow(contentFinderConditionId);
                        if (duty != null)
                        {
                            if (seenDuties.Contains(contentFinderConditionId))
                            {
                                continue;
                            }
                
                            seenDuties.Add(contentFinderConditionId);
                            sources.Add(new DutySource("Duty - " + duty.Name.ToString(), Icons.DutyIcon, duty.RowId));
                
                        }
                    }
                }

                var dungeonBossChests = Service.ExcelCache.GetDungeonBossChests(RowId);
                if (dungeonBossChests != null)
                {
                    foreach (var contentFinderConditionId in dungeonBossChests.Select(c => c.ContentFinderConditionId).ToHashSet())
                    {
                        var duty = Service.ExcelCache.GetContentFinderConditionExSheet().GetRow(contentFinderConditionId);
                        if (duty != null)
                        {
                            if (seenDuties.Contains(contentFinderConditionId))
                            {
                                continue;
                            }
                
                            seenDuties.Add(contentFinderConditionId);
                            sources.Add(new DutySource("Duty - " + duty.Name.ToString(), Icons.DutyIcon, duty.RowId));
                
                        }
                    }
                }

                var dungeonDrops = Service.ExcelCache.GetDungeonDrops(RowId);
                if (dungeonDrops != null)
                {
                    foreach (var contentFinderConditionId in dungeonDrops.Select(c => c.ContentFinderConditionId).ToHashSet())
                    {
                        var duty = Service.ExcelCache.GetContentFinderConditionExSheet().GetRow(contentFinderConditionId);
                        if (duty != null)
                        {
                            if (seenDuties.Contains(contentFinderConditionId))
                            {
                                continue;
                            }
                
                            seenDuties.Add(contentFinderConditionId);
                            sources.Add(new DutySource("Duty - " + duty.Name.ToString(), Icons.DutyIcon, duty.RowId));
                
                        }
                    }
                }
                
                var airshipDrops = Service.ExcelCache.GetAirshipDrops(RowId);
                if (airshipDrops != null)
                {
                    foreach (var airshipDrop in airshipDrops)
                    {
                        var airshipExplorationPoint = Service.ExcelCache.GetAirshipExplorationPointExSheet().GetRow(airshipDrop.AirshipExplorationPointId);
                        if (airshipExplorationPoint != null)
                        {
                            sources.Add(new AirshipSource("Airship Voyage - " + airshipExplorationPoint.FormattedNameShort, Icons.AirshipIcon, airshipExplorationPoint.RowId));
                        }
                    }
                }

                var submarineDrops = Service.ExcelCache.GetSubmarineDrops(RowId);
                if (submarineDrops != null)
                {
                    foreach (var submarineDrop in submarineDrops)
                    {
                        var submarineExploration = Service.ExcelCache.GetSubmarineExplorationExSheet().GetRow(submarineDrop.SubmarineExplorationId);
                        if (submarineExploration != null)
                        {
                            sources.Add(new SubmarineSource("Submarine Voyage - " + submarineExploration.Destination.ToDalamudString().ToString(), Icons.AirshipIcon, submarineExploration.RowId));
                        }
                    }
                }

                _sources = sources;
                return sources;
            }
        }
        
        public bool ObtainedWithSpecialShopCurrency(uint currencyItemId)
        {
            if (Service.ExcelCache.SpecialShopItemRewardCostLookup.ContainsKey(RowId))
            {
                return Service.ExcelCache.SpecialShopItemRewardCostLookup[RowId].Any(c => c.Item1 == currencyItemId);
            }

            return false;
        }
        
        public bool SpentSpecialShop
        {
            get
            {
                return Service.ExcelCache.ItemSpecialShopCostLookup.ContainsKey(RowId);
            }
        }
        
        public bool HandInGrandCompanySupply
        {
            get
            {
                return Service.ExcelCache.ItemToDailySupplyItem.ContainsKey(RowId);
            }
        }
        
        public bool HandInCustomDeliveries
        {
            get
            {
                return Service.ExcelCache.ItemToSatisfactionSupplyLookup.ContainsKey(RowId);
            }
        }
        
        public bool ObtainedSpecialShop
        {
            get
            {
                return Service.ExcelCache.ItemSpecialShopResultLookup.ContainsKey(RowId);
            }
        }

        private bool? _obtainedCalamitySalvager;

        public bool ObtainedCalamitySalvager
        {
            get
            {
                if (_obtainedCalamitySalvager == null)
                {
                    bool hasCalamitySalvager = false;
                    var allShops = Service.ExcelCache.ShopCollection?.GetShops(RowId);
                    if (allShops != null)
                    {
                        foreach (var shop in allShops)
                        {
                            if (!hasCalamitySalvager)
                            {
                                foreach (var npc in shop.ENpcs)
                                {
                                    if (npc.IsCalamitySalvager)
                                    {
                                        hasCalamitySalvager = true;
                                    }
                                }
                            }
                        }
                    }

                    _obtainedCalamitySalvager = hasCalamitySalvager;
                }

                return _obtainedCalamitySalvager.Value;

            }
        }

        
        public bool ObtainedGathering => Service.ExcelCache.ItemGatheringItem.ContainsKey(RowId);

        public bool ObtainedVenture => Service.ExcelCache.GetItemRetainerTaskTypes(RowId) != null;
        public bool ObtainedFixedVenture => Service.ExcelCache.GetItemRetainerTaskTypes(RowId)?.Any(c => c is RetainerTaskType.Botanist or RetainerTaskType.Mining or RetainerTaskType.Hunting or RetainerTaskType.Fishing) ?? false;
        public bool ObtainedRandomVenture => Service.ExcelCache.GetItemRetainerTaskTypes(RowId)?.Any(c => c is RetainerTaskType.FieldExploration or RetainerTaskType.HighlandExploration or RetainerTaskType.WatersideExploration or RetainerTaskType.WoodlandExploration) ?? false;

        public bool PurchasedSQStore => Service.ExcelCache.GetItemsByStoreItem(RowId) != null;

        public HashSet<RetainerTaskType>? RetainerTaskTypes => Service.ExcelCache.GetItemRetainerTaskTypes(RowId);

        private string? _retainerTaskNames;
        public string RetainerTaskNames
        {
            get
            {
                if (_retainerTaskNames == null)
                {
                    if (ObtainedVenture && RetainerTaskTypes != null)
                    {
                        _retainerTaskNames = string.Join(", ", RetainerTaskTypes.Select(c => c.FormattedName()));
                    }
                    else
                    {
                        _retainerTaskNames = "";
                    }
                }

                return _retainerTaskNames;
            }
        }

        private string? _retainerFixedTaskNames;
        public string RetainerFixedTaskNames
        {
            get
            {
                if (_retainerFixedTaskNames == null)
                {
                    if (ObtainedVenture && RetainerTaskTypes != null)
                    {
                        _retainerFixedTaskNames = string.Join(", ", RetainerTaskTypes.Where(c => c is RetainerTaskType.Botanist or RetainerTaskType.Fishing or RetainerTaskType.Hunting or RetainerTaskType.Mining).Select(c => c.FormattedName()));
                    }
                    else
                    {
                        _retainerFixedTaskNames = "";
                    }
                }

                return _retainerFixedTaskNames;
            }
        }

        private string? _retainerRandomTaskNames;
        public string RetainerRandomTaskNames
        {
            get
            {
                if (_retainerRandomTaskNames == null)
                {
                    if (ObtainedVenture && RetainerTaskTypes != null)
                    {
                        _retainerRandomTaskNames = string.Join(", ", RetainerTaskTypes.Where(c => c is RetainerTaskType.HighlandExploration or RetainerTaskType.FieldExploration or RetainerTaskType.WatersideExploration or RetainerTaskType.WoodlandExploration).Select(c => c.FormattedName()));
                    }
                    else
                    {
                        _retainerRandomTaskNames = "";
                    }
                }

                return _retainerRandomTaskNames;
            }
        }

        private string? _currencyNames;

        public string ObtainedByCurrencies
        {
            get
            {
                if (_currencyNames != null)
                {
                    return _currencyNames;
                }

                if (Service.ExcelCache.ItemSpecialShopResultLookup.ContainsKey(RowId))
                {
                    var currencyItems = Service.ExcelCache.ItemSpecialShopResultLookup[RowId];
                    var names = currencyItems.Select(c =>
                    {
                        var items = Service.ExcelCache.GetItemExSheet();
                        return items.GetRow(c)?.NameString ?? "Unknown";
                    }).Where(c => c != "").Distinct().ToList();
                    _currencyNames = String.Join(", ", names);
                }
                else
                {
                    _currencyNames = "";
                }

                return _currencyNames;
            }
        }

        public bool ObtainedFromCurrencyShop
        {
            get
            {
                return Service.ExcelCache.ItemSpecialShopResultLookup.ContainsKey(RowId);
            }
        }

        public EquipRaceCategoryEx? EquipRaceCategory => Service.ExcelCache.GetEquipRaceCategoryExSheet().GetRow(EquipRestriction);

        public CharacterRace EquipRace => EquipRaceCategory?.EquipRace ?? CharacterRace.None;

        public bool CanBeAcquired
        {
            get
            {
                var action = ItemAction?.Value;
                return ActionTypeExt.IsValidAction(action);
            }
        }

        public bool CanTryOn
        {
            get
            {
                if (EquipSlotCategory?.Value == null) return false;
                if (EquipSlotCategory.Row > 0 && EquipSlotCategory.Row != 6 &&
                    EquipSlotCategory.Row != 17 &&
                    (EquipSlotCategory.Value.OffHand <= 0 || ItemUICategory.Row == 11))
                {
                    return true;
                }

                return false;
            }
        }
        
        public bool CanBeCrafted => Service.ExcelCache.CanCraftItem(RowId) || Service.ExcelCache.IsCompanyCraft(RowId);
        public bool CanOpenCraftLog => Service.ExcelCache.CanCraftItem(RowId);
        public bool CanOpenGatheringLog => CanBeGathered;
        public bool CanBeGathered => Service.ExcelCache.CanBeGathered(RowId);
        public bool ObtainedGil => Service.ExcelCache.ItemGilShopLookup.ContainsKey(RowId);
        public bool ObtainedCompanyScrip => Service.ExcelCache.ItemGcScripShopLookup.ContainsKey(RowId);
        public bool ObtainedCompanyCredits => Service.ExcelCache.ItemFccShopLookup.ContainsKey(RowId);
        public bool ObtainedFishing => Service.ExcelCache.FishParameters.ContainsKey(RowId) || IsSpearfishingItem();

        public bool IsIshgardCraft => Service.ExcelCache.IsIshgardCraft(RowId);

        public HWDCrafterSupplyEx? GetHwdCrafterSupply()
        {
            if (!IsIshgardCraft) return null;
            var supplyId = Service.ExcelCache.GetHWDCrafterSupplyId(RowId);
            if (supplyId != null)
            {
                return Service.ExcelCache.GetHWDCrafterSupplySheet().GetRow(supplyId.Value);
            }

            return null;
        }
        
        public CharacterSex EquippableByGender
        {
            get
            {
                if (CanBeEquippedByRaceGender( CharacterRace.Any, CharacterSex.Both))
                {
                    return CharacterSex.Both;
                }

                if (CanBeEquippedByRaceGender( CharacterRace.Any, CharacterSex.Male))
                {
                    return CharacterSex.Male;
                }

                if (CanBeEquippedByRaceGender( CharacterRace.Any, CharacterSex.Female))
                {
                    return CharacterSex.Female;
                }

                return CharacterSex.NotApplicable;
            }
        }
        
        public bool CanBeEquippedByRaceGender(CharacterRace race, CharacterSex sex)
        {
            if (EquipRestriction == 0)
            {
                return false;
            }
            var equipRaceCategory = Service.ExcelCache.GetEquipRaceCategoryExSheet().GetRow(EquipRestriction);
            if (equipRaceCategory == null)
            {
                return false;
            }
            return equipRaceCategory.AllowsRaceSex(race, sex);
        }

        public bool IsItemAvailableAtTimedNode => Service.ExcelCache.IsItemAvailableAtTimedNode(RowId);
        public bool IsItemAvailableAtHiddenNode => Service.ExcelCache.IsItemAvailableAtHiddenNode(RowId);
        public bool IsItemAvailableAtEphemeralNode => Service.ExcelCache.IsItemAvailableAtEphemeralNode(RowId);

        public bool IsEventItem => EventItem != null;

        public bool IsAquariumItem => Service.ExcelCache.ItemToAquariumFish.ContainsKey(RowId);

        public EventItem? EventItem => Service.ExcelCache.GetEventItem(RowId);

        public string FormattedRarity
        {
            get
            {
                switch (Rarity)
                {
                    case 1:
                        return "Normal";
                    case 2:
                        return "Scarce";
                    case 3:
                        return "Artifact";
                    case 4:
                        return "Relic";
                    case 7:
                        return "Aetherial";
                    default:
                        return "Unknown";
                }
            }
        }

        public List<String> VendorsText
        {
            get
            {
                return Service.ExcelCache.GetVendors(RowId).Select(c => c.Item1.Singular.ToString() + " - " + c.Item2.Name).ToList();
            }
        }

        public List<IShop> Vendors
        {
            get
            {
                return Service.ExcelCache.ShopCollection?.GetShops(RowId) ?? new List<IShop>();
            }
        }

        private Dictionary<uint, List<IShop>>? _vendorsBySourceItemId;
        
        public List<IShop> VendorsBySourceItemId(uint itemId)
        {
            if (_vendorsBySourceItemId == null)
            {
                _vendorsBySourceItemId = new();
            }

            if (!_vendorsBySourceItemId.ContainsKey(itemId))
            {

                if (itemId == 1)
                {

                    _vendorsBySourceItemId[itemId] = Vendors.Where(c => c is GilShopEx or GilShop).ToList();
                }
                else
                {
                    List<IShop> vendors = new();
                    foreach (var vendor in Vendors)
                    {
                        var isGc = false;
                        if (itemId is 20 or 21 or 22)
                        {
                            if (vendor is GCShopEx gcShop)
                            {
                                if (gcShop.GrandCompany.Row == itemId - 19)
                                {
                                    isGc = true;
                                }
                            }
                        }
                            
                        foreach (var listing in vendor.ShopListings)
                        {
                            if (isGc)
                            {
                                if(listing is GCScripShopItemEx gcItemEx && gcItemEx.CostGCSeals != 0)
                                {
                                    vendors.Add(vendor);
                                    break;
                                }
                            }
                            else if (listing.Costs.Any(c => c.ItemEx.Row == itemId))
                            {
                                vendors.Add(vendor);
                                break;
                            }
                        }
                    }

                    _vendorsBySourceItemId[itemId] = vendors;
                }
            }

            return _vendorsBySourceItemId[itemId];
        }

        public bool IsCompanyCraft => Service.ExcelCache.IsCompanyCraft(RowId);
        
        public bool CanBeDesynthed => Desynth != 0;

        public Dictionary<uint, uint> GetFlattenedCraftItems(bool includeSelf = false, uint quantity = 1)
        {
            return Service.ExcelCache.GetFlattenedItemRecipe(RowId, includeSelf, quantity);
        }

        public EquipSlotCategoryEx? EquipSlotCategoryEx
        {
            get
            {
                if (EquipSlotCategory.Row != 0)
                {
                    return Service.ExcelCache.GetEquipSlotCategoryExSheet().GetRow(EquipSlotCategory.Row);
                }

                return null;
            }
        }

        /// <summary>
        /// Recipes where this item is the result
        /// </summary>
        public IEnumerable<RecipeEx> RecipesAsResult
        {
            get
            {
                if (Service.ExcelCache.ItemRecipes.ContainsKey(RowId))
                {
                    return Service.ExcelCache.ItemRecipes[RowId].Select(c => Service.ExcelCache.GetRecipeExSheet().GetRow(c)!);
                }

                return new List<RecipeEx>();
            }
        }

        /// <summary>
        /// Recipes where this item is a part of the ingredients
        /// </summary>
        public IEnumerable<RecipeEx> RecipesAsRequirement
        {
            get
            {
                if (Service.ExcelCache.IsCraftItem(RowId) && Service.ExcelCache.CraftLookupTable.ContainsKey(RowId))
                {
                    return Service.ExcelCache.CraftLookupTable[RowId]
                        .Select(c =>
                            Service.ExcelCache.RecipeLookupTable.ContainsKey(c)
                                ? Service.ExcelCache.RecipeLookupTable[c].ToList()
                                : new List<uint>()).SelectMany(c => c).Distinct().Select(c =>
                            Service.ExcelCache.GetRecipeExSheet().GetRow(c)!);
                    ;
                }
                //Service.ExcelCache.CompanyCraftSequenceByResultItemIdLookup
                //TODO: Finish him
                return new List<RecipeEx>();
            }
        }
        
        public CompanyCraftSequenceEx? CompanyCraftSequenceEx => Service.ExcelCache.CompanyCraftSequenceByResultItemIdLookup.ContainsKey(RowId) ? Service.ExcelCache.GetCompanyCraftSequenceSheet().GetRow(Service.ExcelCache.CompanyCraftSequenceByResultItemIdLookup[RowId]) : null;

        public List<RetainerTaskEx>? RetainerTasks => Service.ExcelCache.GetItemRetainerTasks(RowId);
        public List<RetainerTaskEx>? RetainerRandomTasks => Service.ExcelCache.GetItemRetainerTasks(RowId)?.Where(c => c.IsRandom).ToList() ?? null;
        public List<RetainerTaskEx>? RetainerFixedTasks => Service.ExcelCache.GetItemRetainerTasks(RowId)?.Where(c => !c.IsRandom).ToList() ?? null;

        public uint SellToVendorPrice
        {
            get
            {
                return PriceLow;
            }
        }

        public uint BuyFromVendorPrice
        {
            get
            {
                return PriceMid;
            }
        }
        
        public uint SellToVendorPriceHQ
        {
            get
            {
                return PriceLow + 1;
            }
        }

        public uint BuyFromVendorPriceHQ
        {
            get
            {
                return PriceMid + 1;
            }
        }

        private List<IngredientPreference>? _ingredientPreferences;
        public List<IngredientPreference> IngredientPreferences
        {
            get
            {
                if (_ingredientPreferences == null)
                {
                    _ingredientPreferences = CalculateIngredientPreferences();
                }

                return _ingredientPreferences;
            }
        }

        public bool GetIngredientPreference(IngredientPreferenceType type, uint? itemId, out IngredientPreference? ingredientPreference)
        {
            ingredientPreference = IngredientPreferences.FirstOrDefault(c => c!.Type == type && (itemId == null || itemId == c.LinkedItemId),null);
            return ingredientPreference != null;
        }

        private List<IngredientPreference> CalculateIngredientPreferences()
        {
            List<IngredientPreference> ingredientPreferences = new List<IngredientPreference>();
            //There's a certain amount of duplication here, maybe we can use ItemSources to calculate this data
            if (Vendors.Count != 0 && ObtainedGil)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Buy));
            }

            if (CanBeCrafted)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Crafting));
            }

            if (CanBeGathered)
            {
                var hasMining = false;
                var hasBotany = false;
                foreach (var gatheringType in GatheringTypes)
                {
                    if (!hasMining && gatheringType.Row is 0 or 1)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Mining));
                        hasMining = true;
                    }
                    if (!hasBotany && gatheringType.Row is 2 or 3)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Botany));
                        hasBotany = true;
                    }
                }
            }

            if (ObtainedFishing)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Fishing));
            }

            if (CanBePlacedOnMarket)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Marketboard));
            }

            if (ObtainedFixedVenture)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Venture));
            }

            if (ObtainedRandomVenture)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.ExplorationVenture));
            }

            if (MobDrops.Count != 0)
            {
                ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Mobs));
            }
            
            if (SupplementalSourceData != null)
            {
                var supplementalSources = SupplementalSourceData.Where(c => c.ItemId == RowId);
                
                foreach (var item in supplementalSources)
                {
                    if (item.ItemSupplementSource == ItemSupplementSource.Desynth)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Desynthesis, item.SourceItemId, 1));
                    }
                    else if (item.ItemSupplementSource == ItemSupplementSource.Gardening)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Gardening, item.SourceItemId, 1));
                    }
                    else if (item.ItemSupplementSource == ItemSupplementSource.Reduction)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Reduction, item.SourceItemId, 1));
                    }
                    else if (item.ItemSupplementSource == ItemSupplementSource.SkybuilderHandIn)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.ResourceInspection, item.SourceItemId, 1));
                    }
                }
            }

            var hasHouseVendor = false;
            var allShops = Service.ExcelCache.ShopCollection?.GetShops(RowId);
            if (allShops != null)
            {
                foreach (var shop in allShops)
                {
                    if (!hasHouseVendor)
                    {
                        foreach (var npc in shop.ENpcs)
                        {
                            if (npc.IsHouseVendor)
                            {
                                hasHouseVendor = true;
                                ingredientPreferences.Add(new IngredientPreference(RowId,
                                    IngredientPreferenceType.HouseVendor));
                                break;
                            }
                        }
                    }

                    foreach (var listing in shop.ShopListings)
                    {
                        if (listing.Rewards.Any(c => c.ItemEx.Row == RowId))
                        {
                            var listingCosts = listing.Costs.ToArray();
                            if (!listingCosts.Any()) continue;
                            var firstIngredient = listingCosts.First();
                            if (firstIngredient.ItemEx.Row == 1) continue; //Gil
                            var ingredientPreference = new IngredientPreference(RowId, IngredientPreferenceType.Item,
                                firstIngredient.ItemEx.Row, (uint?)firstIngredient.Count);
                            if (listingCosts.Count() == 2)
                            {
                                ingredientPreference.SetSecondItem(listingCosts[1].ItemEx.Row,
                                    (uint)listingCosts[1].Count);
                            }

                            if (listingCosts.Count() == 3)
                            {
                                ingredientPreference.SetSecondItem(listingCosts[1].ItemEx.Row,
                                    (uint)listingCosts[1].Count);
                                ingredientPreference.SetThirdItem(listingCosts[2].ItemEx.Row,
                                    (uint)listingCosts[2].Count);
                            }

                            if (!ingredientPreferences.Any(c => c.SameItems(ingredientPreference)))
                            {
                                ingredientPreferences.Add(ingredientPreference);
                            }
                        }
                    }
                }
            }



            if (ObtainedCompanyScrip)
            {
                foreach (var item in GcScripShopItems)
                {
                    if (item.GCShopEx.Value!.GrandCompany.Row == 1)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Item, 20, item.CostGCSeals));
                    }

                    else if (item.GCShopEx.Value!.GrandCompany.Row == 2)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Item, 21, item.CostGCSeals));
                    }

                    else if (item.GCShopEx.Value!.GrandCompany.Row == 3)
                    {
                        ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Item, 22, item.CostGCSeals));
                    }
                }
            }
            ingredientPreferences.Add(new IngredientPreference(RowId, IngredientPreferenceType.Empty));

            return ingredientPreferences;
        }

        public List<GCScripShopItemEx> GcScripShopItems
        {
            get
            {
                if (Service.ExcelCache.ItemGcScripShopLookup.ContainsKey(RowId))
                {
                    var ids = Service.ExcelCache.ItemGcScripShopLookup[RowId];
                    var items = new List<GCScripShopItemEx>();
                    foreach (var id in ids)
                    {
                        var item = Service.ExcelCache.GetGCScripShopItemSheet().GetRow(id.Item1, id.Item2);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                    }

                    return items;
                }

                return new List<GCScripShopItemEx>();
            }
        }
        
        public decimal GetPatch()
        {
            return Service.ExcelCache.GetItemPatch(RowId);
        }

        public bool IsSpearfishingItem()
        {
            return Service.ExcelCache.ItemToSpearfishingItemLookup.ContainsKey(RowId);
        }

        public bool IsCrystal => ItemUICategory.Row == 59;

        //TODO: Expand this out further
        public bool IsCurrency => (IsVenture || IsCompanySeal || SpentSpecialShop || IsGil) && ItemUICategory.Row != 59;

        public bool IsVenture => RowId == 21072;

        public bool IsGil => RowId == 1;

        public bool IsCompanySeal => RowId is 20 or 21 or 22;

        public int GenerateHashCode(bool ignoreFlags = false)
        {
            return (int)RowId;
        }

        public bool CanBeTraded => this is { IsUntradable: false };

        public bool CanBePlacedOnMarket => ItemSearchCategory.Row != 0;
        
        public string FormattedSearchCategory =>
            ItemSearchCategory?.Value == null
                ? ""
                : ItemSearchCategory.Value.Name.ToString().Replace("\u0002\u001F\u0001\u0003", "-");

        public string FormattedUiCategory =>
            ItemUICategory?.Value == null
                ? ""
                : ItemUICategory.Value.Name.ToString().Replace("\u0002\u001F\u0001\u0003", "-");

        private BitfieldUptime? _uptime;
        private bool _uptimeCalculated;
        public BitfieldUptime? GetGatheringUptime()
        {
            if (!IsItemAvailableAtTimedNode || _uptimeCalculated)
            {
                return _uptime;
            }
            
            var gatheringItems = GatheringItems;
            foreach (var gatherItem in gatheringItems)
            {
                if(gatherItem.Value == null) continue;
                foreach (var basePoint in gatherItem.Value.GatheringPointBases)
                {
                    if (basePoint.Value == null) continue;
                    var gatheringPoints = basePoint.Value.LazyGatheringPoints;
                    foreach (var gatheringPoint in gatheringPoints)
                    {
                        var gatheringPointValue = gatheringPoint.Value;
                        var gatheringPointTransient = gatheringPointValue?.GatheringPointTransient.GetValue().Value;
                        var timetable = gatheringPointTransient?.GatheringRarePopTimeTable.Value;
                        if (_uptime != null) break;
                        if (gatheringPointTransient != null && timetable != null)
                        {
                            // Check for ephemeral nodes
                            if (timetable.RowId == 0)
                            {
                                var time = new BitfieldUptime(gatheringPointTransient.EphemeralStartTime, gatheringPointTransient.EphemeralEndTime);
                                _uptime = time;
                            }
                            else
                            {
                                _uptime = new(timetable);
                            }
                        }
                        else
                        {
                            _uptime = BitfieldUptime.AllHours;
                        }
                    }
                }
            }

            _uptimeCalculated = true;
            return _uptime;
        }

        private Dictionary<(IngredientPreferenceType, uint?), HashSet<uint>>? _sourceMaps;

        public HashSet<uint> GetSourceMaps(IngredientPreferenceType preferenceType, uint? itemId)
        {
            if (_sourceMaps == null)
            {
                _sourceMaps = new();
            }

            var sourceKey = (preferenceType, itemId);
            if (!_sourceMaps.ContainsKey(sourceKey))
            {
                HashSet<uint> maps = new();
                if (preferenceType == IngredientPreferenceType.Buy)
                {
                    var vendorsBySourceItemId = VendorsBySourceItemId(1);
                    if (vendorsBySourceItemId.Count != 0)
                    {
                        foreach (var map in vendorsBySourceItemId.SelectMany(c => c.ENpcs)
                                     .SelectMany(c => c.Locations).Select(c => c.MapEx.Value).Distinct())
                        {
                            if (map == null) continue;
                            maps.Add(map.RowId);
                        }
                    }
                }
                //TODO: Add in house vendors here somehow or maybe from wherever calls this
                else if (preferenceType == IngredientPreferenceType.Item && itemId != null)
                {
                    var vendorsBySourceItemId = VendorsBySourceItemId(itemId.Value);
                    if (vendorsBySourceItemId.Count != 0)
                    {
                        foreach (var map in vendorsBySourceItemId.SelectMany(c => c.ENpcs)
                                     .SelectMany(c => c.Locations).Select(c => c.MapEx.Value).Distinct())
                        {
                            if (map == null) continue;
                            maps.Add(map.RowId);
                        }
                    }
                }
                else if (preferenceType == IngredientPreferenceType.Mobs)
                {
                    var locations = MobDrops.SelectMany(c => c.MobSpawnPositions).Select(c => c.MapEx.Value).Distinct()
                        .ToList();
                    if (locations.Count != 0)
                    {
                        foreach (var map in locations)
                        {
                            if (map == null) continue;
                            maps.Add(map.RowId);
                        }
                    }
                }
                else if (preferenceType == IngredientPreferenceType.Botany)
                {
                    var gatheringSources = GetGatheringSources();
                    foreach (var gatheringSource in gatheringSources)
                    {
                        if (gatheringSource.IsHarvesting || gatheringSource.IsLogging)
                        {
                            maps.Add(gatheringSource.TerritoryType.Map.Row);
                        }
                    }
                }
                else if (preferenceType == IngredientPreferenceType.Mining)
                {
                    var gatheringSources = GetGatheringSources();
                    foreach (var gatheringSource in gatheringSources)
                    {
                        if (gatheringSource.IsMining || gatheringSource.IsQuarrying)
                        {
                            maps.Add(gatheringSource.TerritoryType.Map.Row);
                        }
                    }
                }

                _sourceMaps[sourceKey] = maps;
            }
            return _sourceMaps[sourceKey];
        }

        public uint ItemId
        {
            get => RowId;
            set {  }
        }

        public ItemEx Item => this;
    }
}