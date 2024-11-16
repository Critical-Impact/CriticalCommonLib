using System;
using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.ItemSources;
using AllaganLib.GameSheets.Service;
using AllaganLib.GameSheets.Sheets;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Crafting;

public class CraftingCache
{
    private readonly SheetManager _sheetManager;
    private readonly ItemSheet _itemSheet;
    private Dictionary<uint, List<IngredientPreference>?> ingredientPreferences;

    public CraftingCache(SheetManager sheetManager)
    {
        _sheetManager = sheetManager;
        _itemSheet = sheetManager.GetSheet<ItemSheet>();
        ingredientPreferences = new Dictionary<uint, List<IngredientPreference>?>();
    }

    public List<IngredientPreference> GetIngredientPreferences(uint itemId)
    {
        if (!ingredientPreferences.ContainsKey(itemId))
        {
            ingredientPreferences[itemId] = CalculateIngredientPreferences(itemId);
        }

        var ingredientPreference = ingredientPreferences[itemId];
        if (ingredientPreference == null)
        {
            return [];
        }

        return ingredientPreference;
    }

    public bool GetIngredientPreference(uint itemId, IngredientPreferenceType type, uint? linkedItemId, out IngredientPreference? ingredientPreference)
    {
        if (!ingredientPreferences.ContainsKey(itemId))
        {
            ingredientPreferences[itemId] = CalculateIngredientPreferences(itemId);
        }

        if (ingredientPreferences[itemId] == null)
        {
            ingredientPreference = null;
            return false;
        }

        ingredientPreference = ingredientPreferences[itemId]?.FirstOrDefault(c => c!.Type == type && (linkedItemId == null || linkedItemId == c.LinkedItemId),null);
        return ingredientPreference != null;
    }

    private List<IngredientPreference>? CalculateIngredientPreferences(uint itemId)
    {
        var item = _itemSheet.GetRowOrDefault(itemId);
        if (item == null)
        {
            return null;
        }

        List<IngredientPreference> preferences = new();

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
                    case IngredientPreferenceType.Buy:
                    case IngredientPreferenceType.Crafting:
                    case IngredientPreferenceType.Marketboard:
                    case IngredientPreferenceType.Venture:
                    case IngredientPreferenceType.ResourceInspection:
                    case IngredientPreferenceType.Mobs:
                    case IngredientPreferenceType.HouseVendor:
                    case IngredientPreferenceType.ExplorationVenture:
                    case IngredientPreferenceType.Desynthesis:
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