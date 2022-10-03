using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina;
using Lumina.Data;
using Lumina.Data.Parsing;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Generated;

namespace CriticalCommonLib.Sheets
{
    public class ItemEx : Item
    {
        public IEnumerable<(LazyRow<ItemEx>,uint)> _specialShopCosts;
        public IEnumerable<(LazyRow<ItemEx>,uint)> _specialShopRewards;
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
            _gatheringTypes = new Lazy<HashSet<uint>>(CalculateGatheringTypes, LazyThreadSafetyMode.PublicationOnly);
            _gatheringItems =
                new Lazy<List<GatheringItemEx>>(CalculateGatheringItems, LazyThreadSafetyMode.PublicationOnly);
        }

        public string NameString
        {
            get
            {
                if (_nameString == null)
                {
                    _nameString = Name.ToDalamudString().ToString();
                }

                return _nameString;
            }
        }

        private string? _nameString;

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
            return Service.ExcelCache.GetItemExSheet().Where(c => c.GetPrimaryModelKeyString() != "" && c.GetPrimaryModelKeyString() == GetPrimaryModelKeyString() && c.RowId != RowId).ToList();
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
                default:
                    throw new NotImplementedException();
            }
        }

        public HashSet<GatheringSource> GetGatheringSources()
        {
            var sources = new HashSet<GatheringSource>();
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
                                    var gatheringSource = new GatheringSource(type, level, territoryType, placeName);
                                    sources.Add(gatheringSource);
                                }
                            }
                        }
                    }
                }
            }

            return sources;
        }
        
        private Lazy<List<GatheringItemEx>> _gatheringItems = null!;
        private Lazy<HashSet<uint>> _gatheringTypes = null!;

        private HashSet<uint> CalculateGatheringTypes()
        {
            var gatheringTypes = new HashSet<uint>();
            if (Service.ExcelCache.ItemGatheringItem.ContainsKey(RowId))
            {
                var gatheringItemIds = Service.ExcelCache.ItemGatheringItem[RowId];
                foreach (var gatheringItemId in gatheringItemIds)
                {
                    if (Service.ExcelCache.GatheringItemToGatheringItemPoint.ContainsKey(gatheringItemId))
                    {
                        var itemPoints = Service.ExcelCache.GatheringItemToGatheringItemPoint[gatheringItemId];
                        foreach (var itemPoint in itemPoints)
                        {
                            if (Service.ExcelCache.GatheringItemPointToGatheringPointBase.ContainsKey(itemPoint))
                            {
                                var gatheringPoints = Service.ExcelCache.GatheringItemPointToGatheringPointBase[itemPoint];
                                foreach (var gatheringPointBase in gatheringPoints)
                                {
                                    if (Service.ExcelCache.GatheringPointBaseToGatheringType.ContainsKey(
                                            gatheringPointBase))
                                    {
                                        var type = Service.ExcelCache.GatheringPointBaseToGatheringType[
                                            gatheringPointBase];
                                        gatheringTypes.Add(type);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (Service.ExcelCache.ItemGatheringTypes.ContainsKey(RowId))
            {
                foreach (var gatheringType in Service.ExcelCache.ItemGatheringTypes[RowId])
                {
                    gatheringTypes.Add(gatheringType);
                }
            }
            return gatheringTypes;
        }

        private List<ItemSource>? _uses;

        public List<ItemSource> Uses
        {
            get
            {
                if (_uses != null)
                {
                    return _uses;
                }
                List<ItemSource> uses = new List<ItemSource>();
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

                if (ItemSupplement.ReverseDesynthItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.ReverseDesynthItems[RowId])
                    {
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            uses.Add(new ItemSource("Desynthesis - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ItemSupplement.ReverseGardeningItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.ReverseGardeningItems[RowId])
                    {
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            uses.Add(new ItemSource("Gardening - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ItemSupplement.ReverseLootItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.ReverseLootItems[RowId])
                    {
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            uses.Add(new ItemSource("Loot - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ItemSupplement.ReverseReduceItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.ReverseReduceItems[RowId])
                    {
                        
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            uses.Add(new ItemSource("Reduction - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                _uses = uses;
                return _uses;
            }
        }

        private List<ItemSource>? _sources;
        public List<ItemSource> Sources
        {
            get
            {
                if (_sources != null)
                {
                    return _sources;
                }
                
                List<ItemSource> sources = new List<ItemSource>();
                if (ObtainedGil)
                {
                    
                    sources.Add(new ItemSource("Gil", Service.ExcelCache.GetItemExSheet().GetRow(1)!.Icon, 1));
                }
                if (ItemSupplement.DesynthItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.DesynthItems[RowId])
                    {
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            sources.Add(new ItemSource("Desynthesis - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ItemSupplement.GardeningItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.GardeningItems[RowId])
                    {
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            sources.Add(new ItemSource("Gardening - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ItemSupplement.LootItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.LootItems[RowId])
                    {
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            sources.Add(new ItemSource("Loot - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ItemSupplement.ReduceItems.ContainsKey(RowId))
                {
                    foreach (var item in ItemSupplement.ReduceItems[RowId])
                    {
                        
                        var itemEx = Service.ExcelCache.GetItemExSheet().GetRow(item);
                        if (itemEx != null)
                        {
                            sources.Add(new ItemSource("Reduction - " + itemEx.NameString, itemEx.Icon, itemEx.RowId));
                        }
                    }
                }
                if (ObtainedCompanyScrip)
                {
                    unsafe
                    {
                        var gc = UIState.Instance()->PlayerState.GrandCompany;
                        switch (gc)
                        {
                            case 1:
                                sources.Add(new ItemSource(Service.ExcelCache.GetItemExSheet().GetRow(20)!.NameString, Service.ExcelCache.GetItemExSheet().GetRow(20)!.Icon, 20));
                                break;
                            case 2:
                                sources.Add(new ItemSource(Service.ExcelCache.GetItemExSheet().GetRow(21)!.NameString, Service.ExcelCache.GetItemExSheet().GetRow(21)!.Icon, 21));
                                break;
                            case 3:
                                sources.Add(new ItemSource(Service.ExcelCache.GetItemExSheet().GetRow(22)!.NameString, Service.ExcelCache.GetItemExSheet().GetRow(22)!.Icon, 22));

                                break;
                        }
                    }
                }

                if (ObtainedCompanyCredits)
                {
                    sources.Add(new ItemSource("Company Credits",Service.ExcelCache.GetItemExSheet().GetRow(6559)!.Icon, 6559));
                }
                if (IsItemAvailableAtTimedNode)
                {
                    sources.Add(new ItemSource("Timed Node", 60461, null));
                }
                else if (ObtainedGathering)
                {
                    foreach (var gatheringType in _gatheringTypes.Value)
                    {
                        //Mining
                        if (gatheringType == 0)
                        {
                            sources.Add(new ItemSource("Mining", 60438, null));
                        }
                        //Quarrying
                        if (gatheringType == 1)
                        {
                            sources.Add(new ItemSource("Quarrying", 60437, null));
                        }
                        //Logging
                        if (gatheringType == 2)
                        {
                            sources.Add(new ItemSource("Logging", 60433, null));
                        }
                        //Harvesting
                        if (gatheringType == 3)
                        {
                            sources.Add(new ItemSource("Harvesting", 60432, null));
                        }
                    }
                }
                else if (ObtainedFishing)
                {
                    sources.Add(new ItemSource("Fishing", 60465, null));
                }
                else if (ObtainedVenture)
                {
                    sources.Add(new ItemSource("Venture", Service.ExcelCache.GetItemExSheet().GetRow(21072)!.Icon, 21072));
                }
                foreach (var specialShopCurrency in _specialShopCosts)
                {
                    if (specialShopCurrency.Item1.Value != null)
                    {
                        sources.Add( new ItemSource(specialShopCurrency.Item1.Value.NameString, specialShopCurrency.Item1.Value.Icon, specialShopCurrency.Item1.Value.RowId, specialShopCurrency.Item2));
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
        
        public bool ObtainedSpecialShop
        {
            get
            {
                return Service.ExcelCache.ItemSpecialShopResultLookup.ContainsKey(RowId);
            }
        }

        
        public bool ObtainedGathering
        {
            get
            {
                return Service.ExcelCache.ItemGatheringItem.ContainsKey(RowId);
            }
        }

        public bool ObtainedVenture
        {
            get
            {
                return Service.ExcelCache.ItemToRetainerTaskNormalLookup.ContainsKey(RowId);
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
        public bool ObtainedFishing => Service.ExcelCache.FishParameters.ContainsKey(RowId);
        
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
                return Service.ExcelCache.ShopCollection.GetShops(RowId);
            }
        }

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

                return new List<RecipeEx>();
            }
        }

        public IEnumerable<RetainerTaskNormalEx> RetainerTasks
        {
            get
            {
                if (Service.ExcelCache.ItemToRetainerTaskNormalLookup.ContainsKey(RowId))
                {
                    return Service.ExcelCache.ItemToRetainerTaskNormalLookup[RowId]
                        .Select(c => Service.ExcelCache.GetRetainerTaskNormalExSheet().GetRow(c)!);
                }
                return new List<RetainerTaskNormalEx>();
            }
        }

        public bool CanBeTraded => this is { IsUntradable: false } && ItemSearchCategory.Row != 0;

        public string FormattedSearchCategory =>
            ItemSearchCategory?.Value == null
                ? ""
                : ItemSearchCategory.Value.Name.ToString().Replace("\u0002\u001F\u0001\u0003", "-");

        public string FormattedUiCategory =>
            ItemUICategory?.Value == null
                ? ""
                : ItemUICategory.Value.Name.ToString().Replace("\u0002\u001F\u0001\u0003", "-");
    }
}