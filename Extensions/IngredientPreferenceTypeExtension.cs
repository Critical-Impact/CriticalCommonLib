using System;
using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Caches;
using CriticalCommonLib.Crafting;

namespace CriticalCommonLib.Extensions;

public static class IngredientPreferenceTypeExtension
{
    public static HashSet<ItemInfoType> ToItemInfoTypes(this IngredientPreferenceType ingredientPreferenceType)
    {
        switch (ingredientPreferenceType)
        {
            case IngredientPreferenceType.None:
                return [];
            case IngredientPreferenceType.Mining:
                return
                [
                    ItemInfoType.Mining,
                    ItemInfoType.HiddenMining,
                    ItemInfoType.EphemeralMining,
                    ItemInfoType.TimedMining,

                    ItemInfoType.Quarrying,
                    ItemInfoType.HiddenQuarrying,
                    ItemInfoType.EphemeralQuarrying,
                    ItemInfoType.TimedQuarrying
                ];
            case IngredientPreferenceType.Botany:
                return
                [
                    ItemInfoType.Logging,
                    ItemInfoType.HiddenLogging,
                    ItemInfoType.EphemeralLogging,
                    ItemInfoType.TimedLogging,

                    ItemInfoType.Harvesting,
                    ItemInfoType.HiddenHarvesting,
                    ItemInfoType.EphemeralHarvesting,
                    ItemInfoType.TimedHarvesting
                ];
            case IngredientPreferenceType.Fishing:
                return
                [
                    ItemInfoType.Fishing,
                    ItemInfoType.Spearfishing,
                ];
            case IngredientPreferenceType.Buy:
                return
                [
                    ItemInfoType.SpecialShop,
                    ItemInfoType.CashShop,
                    ItemInfoType.FateShop,
                    ItemInfoType.GilShop,
                    ItemInfoType.FCShop,
                    ItemInfoType.GCShop,
                    ItemInfoType.CalamitySalvagerShop,
                ];
            case IngredientPreferenceType.Crafting:
                return
                [
                    ItemInfoType.CraftRecipe,
                    ItemInfoType.FreeCompanyCraftRecipe,
                ];
            case IngredientPreferenceType.Marketboard:
                return [];
            case IngredientPreferenceType.Item:
                return
                [
                    ItemInfoType.SpecialShop,
                ];
            case IngredientPreferenceType.Venture:
                return
                [
                    ItemInfoType.BotanyVenture,
                    ItemInfoType.CombatVenture,
                    ItemInfoType.FishingVenture,
                    ItemInfoType.MiningVenture,
                ];
            case IngredientPreferenceType.Reduction:
                return
                [
                    ItemInfoType.Reduction,
                ];
            case IngredientPreferenceType.ResourceInspection:
                return
                [
                    ItemInfoType.SkybuilderInspection,
                ];
            case IngredientPreferenceType.Gardening:
                return
                [
                    ItemInfoType.Gardening,
                ];
            case IngredientPreferenceType.Desynthesis:
                return
                [
                    ItemInfoType.Desynthesis,
                ];
            case IngredientPreferenceType.Mobs:
                return
                [
                    ItemInfoType.Monster,
                ];
            case IngredientPreferenceType.HouseVendor:
                return
                [
                    ItemInfoType.GilShop,
                ];
            case IngredientPreferenceType.ExplorationVenture:
                return
                [
                    ItemInfoType.BotanyExplorationVenture,
                    ItemInfoType.CombatExplorationVenture,
                    ItemInfoType.FishingExplorationVenture,
                    ItemInfoType.MiningExplorationVenture,
                ];
        }

        return [];
    }
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