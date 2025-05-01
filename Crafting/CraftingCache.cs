using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.ItemSources;
using AllaganLib.GameSheets.Service;
using AllaganLib.GameSheets.Sheets;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Crafting;

public class CraftingCache
{
    private readonly ItemSheet _itemSheet;
    private readonly ConcurrentDictionary<uint, List<IngredientPreference>?> _ingredientPreferences;

    public CraftingCache(SheetManager sheetManager)
    {
        _itemSheet = sheetManager.GetSheet<ItemSheet>();
        _ingredientPreferences = new ConcurrentDictionary<uint, List<IngredientPreference>?>();
    }

    public List<IngredientPreference> GetIngredientPreferences(uint itemId)
    {
        if (!_ingredientPreferences.ContainsKey(itemId))
        {
            _ingredientPreferences[itemId] = CalculateIngredientPreferences(itemId);
        }

        var ingredientPreference = _ingredientPreferences[itemId];
        if (ingredientPreference == null)
        {
            return [];
        }

        return ingredientPreference;
    }

    public bool GetIngredientPreference(uint itemId, IngredientPreferenceType type, uint? linkedItemId, out IngredientPreference? ingredientPreference, IngredientPreferenceType? notAllowedType = null)
    {
        if (!_ingredientPreferences.ContainsKey(itemId))
        {
            _ingredientPreferences[itemId] = CalculateIngredientPreferences(itemId);
        }

        if (_ingredientPreferences[itemId] == null)
        {
            ingredientPreference = null;
            return false;
        }

        ingredientPreference = _ingredientPreferences[itemId]?.FirstOrDefault(c => (notAllowedType == null || c!.Type != notAllowedType) &&  c!.Type == type && (linkedItemId == null || linkedItemId == c.LinkedItemId),null);
        return ingredientPreference != null;
    }

    public bool GetIngredientPreferences(uint itemId, IngredientPreferenceType type, uint? linkedItemId, out List<IngredientPreference>? ingredientPreferences, IngredientPreferenceType? notAllowedType = null)
    {
        if (!_ingredientPreferences.ContainsKey(itemId))
        {
            _ingredientPreferences[itemId] = CalculateIngredientPreferences(itemId);
        }

        if (_ingredientPreferences[itemId] == null)
        {
            ingredientPreferences = null;
            return false;
        }

        ingredientPreferences = _ingredientPreferences[itemId]?.Where(c => (notAllowedType == null || c!.Type != notAllowedType) &&  c!.Type == type && (linkedItemId == null || linkedItemId == c.LinkedItemId)).ToList();
        return ingredientPreferences != null;
    }

    private List<IngredientPreference>? CalculateIngredientPreferences(uint itemId)
    {
        var item = _itemSheet.GetRowOrDefault(itemId);
        if (item == null)
        {
            return null;
        }

        List<IngredientPreference> preferences = new();
        // Come up with a better way of doing this
        var fateShopAdded = false;
        foreach (var source in item.Sources)
        {
            var ingredientPreferenceType = source.Type.ToIngredientPreferenceType();
            if (ingredientPreferenceType == IngredientPreferenceType.None)
            {
                continue;
            }
            if (preferences.Any(c => c.Type == ingredientPreferenceType))
            {
                switch (ingredientPreferenceType)
                {
                    case IngredientPreferenceType.Mining:
                    case IngredientPreferenceType.Botany:
                    case IngredientPreferenceType.Fishing:
                    case IngredientPreferenceType.SpearFishing:
                    case IngredientPreferenceType.Buy:
                    case IngredientPreferenceType.Crafting:
                    case IngredientPreferenceType.Marketboard:
                    case IngredientPreferenceType.Venture:
                    case IngredientPreferenceType.ResourceInspection:
                    case IngredientPreferenceType.Mobs:
                    case IngredientPreferenceType.HouseVendor:
                    case IngredientPreferenceType.ExplorationVenture:
                    case IngredientPreferenceType.Empty:
                        continue;
                }
            }

            if (source is ItemSpecialShopSource specialShopSource)
            {
                var costs = specialShopSource.ShopListing.Costs.ToList();
                if (costs.Count != 0)
                {
                    var specialShopPreference =
                        new IngredientPreference(itemId, ingredientPreferenceType, costs[0].Item.RowId, costs[0].Count);
                    if (costs.Count >= 2)
                    {
                        specialShopPreference.SetSecondItem(costs[1].Item.RowId, costs[1].Count);
                    }
                    if (costs.Count >= 3)
                    {
                        specialShopPreference.SetThirdItem(costs[2].Item.RowId, costs[2].Count);
                    }
                    preferences.Add(specialShopPreference);
                }
            }
            else if (source is ItemFateShopSource fateShopSource)
            {
                if (fateShopAdded)
                {
                    continue;
                }

                fateShopAdded = true;
                var costs = fateShopSource.ShopListing.Costs.ToList();
                if (costs.Count != 0)
                {
                    var fateShopPreference =
                        new IngredientPreference(itemId, ingredientPreferenceType, costs[0].Item.RowId, costs[0].Count);
                    if (costs.Count >= 2)
                    {
                        fateShopPreference.SetSecondItem(costs[1].Item.RowId, costs[1].Count);
                    }
                    if (costs.Count >= 3)
                    {
                        fateShopPreference.SetThirdItem(costs[2].Item.RowId, costs[2].Count);
                    }
                    preferences.Add(fateShopPreference);
                }
            }
            else
            {
                preferences.Add(new IngredientPreference(itemId, ingredientPreferenceType, source.CostItem?.RowId,
                    source.Quantity));
            }
        }

        if (item.CanBePlacedOnMarket)
        {
            preferences.Add(new IngredientPreference(itemId, IngredientPreferenceType.Marketboard));
        }

        return preferences;
    }
}