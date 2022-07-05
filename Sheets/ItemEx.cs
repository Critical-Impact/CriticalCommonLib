using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Services;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class ItemEx : Item
    {
        private List<ushort>? _sourceIcons;
        
        public List<ushort> SourceIcons
        {
            get
            {
                if (_sourceIcons == null)
                {
                    List<ushort> sourceIcons = new List<ushort>();
                    if (CanBeBoughtWithGil)
                    {
                        sourceIcons.Add(Service.ExcelCache.GetSheet<ItemEx>().GetRow(1)!.Icon);
                    }

                    if (CanBeBoughtWithCompanyScrip)
                    {
                        //Make this dynamic at some point
                        sourceIcons.Add(Service.ExcelCache.GetSheet<ItemEx>().GetRow(22)!.Icon);
                    }

                    if (IsItemAvailableAtTimedNode)
                    {
                        sourceIcons.Add(60461);
                    }
                    else if (ObtainedGathering)
                    {
                        //Need to differentiate between mining/harvesting
                        sourceIcons.Add(60437);
                    }
                    else if (ObtainedCombatVenture)
                    {
                        sourceIcons.Add(60459);
                    }

                    _sourceIcons = sourceIcons;
                }

                return _sourceIcons;
            }
        }
        public string Sources
        {
            get
            {
                List<string> sources = new List<string>();
                if (CanBeBoughtWithGil)
                {
                    sources.Add("Gil");
                }
                if (CanBeBoughtWithCompanyScrip)
                {
                    sources.Add("GC Seals");
                }
                if (IsItemAvailableAtTimedNode)
                {
                    sources.Add("Timed Node");
                }
                else if (ObtainedGathering)
                {
                    sources.Add("Gathered");
                }
                else if (ObtainedCombatVenture)
                {
                    sources.Add("Combat Venture");
                }
                else if (ObtainedCombatVenture)
                {
                    sources.Add("Combat Venture");
                }
                else if (ObtainedFromCurrencyShop)
                {
                    sources.Add( ObtainedByCurrencies);
                }
                return String.Join(", ",sources);
            }
        }
        
        public bool ObtainedGathering
        {
            get
            {
                return Service.ExcelCache.CanBeGathered(RowId);
            }
        }

        public bool ObtainedCombatVenture
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
                var currencyItems = Service.ExcelCache.GetCurrenciesByResultItemId(RowId);
                if (currencyItems != null)
                {
                    var names = currencyItems.Select(c =>
                    {
                        var items = Service.ExcelCache.GetSheet<Item>();
                        return items.GetRow(c)?.Name ?? "Unknown";
                    }).Where(c => c != "").Distinct().ToList();
                    _currencyNames = String.Join(", ", names);
                }
                else
                {
                    return "";
                }

                return _currencyNames;
            }
        }

        public bool ObtainedFromCurrencyShop
        {
            get
            {
                return Service.ExcelCache.BoughtAtSpecialShop(RowId);
            }
        }

        public EquipRaceCategoryEx? EquipRaceCategory => Service.ExcelCache.GetSheet<EquipRaceCategoryEx>().GetRow(EquipRestriction);

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
        
        public bool CanBeBoughtWithGil => Service.ExcelCache.ItemGilShopLookup.ContainsKey(RowId);
        public bool CanBeBoughtWithCompanyScrip => Service.ExcelCache.ItemGcScripShopLookup.ContainsKey(RowId);
        
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
            var equipRaceCategory = Service.ExcelCache.GetSheet<EquipRaceCategoryEx>().GetRow(EquipRestriction);
            if (equipRaceCategory == null)
            {
                return false;
            }
            return equipRaceCategory.AllowsRaceSex(race, sex);
        }

        public bool IsItemAvailableAtTimedNode => Service.ExcelCache.IsItemAvailableAtTimedNode(RowId);

        public bool IsEventItem => EventItem != null;

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

        public List<String> Vendors
        {
            get
            {
                return Service.ExcelCache.GetVendors(RowId).Select(c => c.Item1.Singular.ToString() + " - " + c.Item2.Name).ToList();
            }
        }

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
                    return Service.ExcelCache.GetSheet<EquipSlotCategoryEx>().GetRow(EquipSlotCategory.Row);
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
                    return Service.ExcelCache.ItemRecipes[RowId].Select(c => Service.ExcelCache.GetSheet<RecipeEx>().GetRow(c)!);
                }

                return new List<RecipeEx>();
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