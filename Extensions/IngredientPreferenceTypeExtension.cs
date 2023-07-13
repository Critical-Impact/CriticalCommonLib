using CriticalCommonLib.Crafting;

namespace CriticalCommonLib.Extensions;

public static class IngredientPreferenceTypeExtension
{
    public static string FormattedName(this IngredientPreferenceType ingredientPreferenceType)
    {
        switch (ingredientPreferenceType)
        {
            case IngredientPreferenceType.Botany:
                return "Botany";
            case IngredientPreferenceType.Buy:
                return "Buy from Vendor";
            case IngredientPreferenceType.HouseVendor:
                return "Buy from House Vendor";
            case IngredientPreferenceType.Crafting:
                return "Crafting";
            case IngredientPreferenceType.Fishing:
                return "Fishing";
            case IngredientPreferenceType.Item:
                return "Item";
            case IngredientPreferenceType.Marketboard:
                return "Marketboard";
            case IngredientPreferenceType.Mining:
                return "Mining";
            case IngredientPreferenceType.Venture:
                return "Venture";
            case IngredientPreferenceType.ExplorationVenture:
                return "Venture (Exploration)";
            case IngredientPreferenceType.Desynthesis:
                return "Desynthesis";
            case IngredientPreferenceType.Reduction:
                return "Reduction";
            case IngredientPreferenceType.ResourceInspection:
                return "Resource Inspection";
            case IngredientPreferenceType.Gardening:
                return "Gardening";
            case IngredientPreferenceType.Mobs:
                return "Monsters";
            case IngredientPreferenceType.Empty:
                return "Nothing";
        }

        return ingredientPreferenceType.ToString();
    }
}