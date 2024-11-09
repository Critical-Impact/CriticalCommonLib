using System.Collections.Generic;
using System.Linq;
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
            preferences.Add(new IngredientPreference(itemId, ingredientPreferenceType));
        }

        if (item.CanBePlacedOnMarket)
        {
            preferences.Add(new IngredientPreference(itemId, IngredientPreferenceType.Marketboard));
        }

        return preferences;
    }
}