using System;
using System.Collections.Generic;
using AllaganLib.GameSheets.Caches;
using CriticalCommonLib.Crafting;

namespace CriticalCommonLib.Extensions;

public static class ItemInfoTypeExtension
{
    public static IngredientPreferenceType ToIngredientPreferenceType(this ItemInfoType ingredientPreferenceType)
    {
        switch (ingredientPreferenceType)
        {
            case ItemInfoType.CraftRecipe:
                return IngredientPreferenceType.Crafting;
            case ItemInfoType.FreeCompanyCraftRecipe:
                return IngredientPreferenceType.Crafting;
            case ItemInfoType.SpecialShop:
                return IngredientPreferenceType.Item;
            case ItemInfoType.GilShop:
                return IngredientPreferenceType.Buy;
            case ItemInfoType.AnimaShop:
                return IngredientPreferenceType.Item;
            case ItemInfoType.CalamitySalvagerShop:
                return IngredientPreferenceType.Buy;
            case ItemInfoType.FCShop:
                return IngredientPreferenceType.Item;
            case ItemInfoType.GCShop:
                return IngredientPreferenceType.Item;
            case ItemInfoType.CashShop:
                return IngredientPreferenceType.None;
            case ItemInfoType.FateShop:
                return IngredientPreferenceType.Item;
            case ItemInfoType.Mining:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.Quarrying:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.Logging:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.Harvesting:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.HiddenMining:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.HiddenQuarrying:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.HiddenLogging:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.HiddenHarvesting:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.TimedMining:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.TimedQuarrying:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.TimedLogging:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.TimedHarvesting:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.EphemeralQuarrying:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.EphemeralMining:
                return IngredientPreferenceType.Mining;
            case ItemInfoType.EphemeralLogging:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.EphemeralHarvesting:
                return IngredientPreferenceType.Botany;
            case ItemInfoType.Fishing:
                return IngredientPreferenceType.Fishing;
            case ItemInfoType.Spearfishing:
                return IngredientPreferenceType.SpearFishing;
            case ItemInfoType.Monster:
                return IngredientPreferenceType.Mobs;
            case ItemInfoType.Fate:
                return IngredientPreferenceType.None;
            case ItemInfoType.Desynthesis:
                return IngredientPreferenceType.Desynthesis;
            case ItemInfoType.Gardening:
                return IngredientPreferenceType.Gardening;
            case ItemInfoType.Loot:
                return IngredientPreferenceType.Item;
            case ItemInfoType.SkybuilderInspection:
                return IngredientPreferenceType.ResourceInspection;
            case ItemInfoType.QuickVenture:
                return IngredientPreferenceType.None;
            case ItemInfoType.MiningVenture:
                return IngredientPreferenceType.Venture;
            case ItemInfoType.MiningExplorationVenture:
                return IngredientPreferenceType.ExplorationVenture;
            case ItemInfoType.BotanyVenture:
                return IngredientPreferenceType.Venture;
            case ItemInfoType.BotanyExplorationVenture:
                return IngredientPreferenceType.ExplorationVenture;
            case ItemInfoType.CombatVenture:
                return IngredientPreferenceType.Venture;
            case ItemInfoType.CombatExplorationVenture:
                return IngredientPreferenceType.ExplorationVenture;
            case ItemInfoType.FishingVenture:
                return IngredientPreferenceType.Venture;
            case ItemInfoType.FishingExplorationVenture:
                return IngredientPreferenceType.ExplorationVenture;
            case ItemInfoType.Reduction:
                return IngredientPreferenceType.Reduction;
            case ItemInfoType.Airship:
                return IngredientPreferenceType.None; //TODO: Implement the rest of these
            case ItemInfoType.Submarine:
                return IngredientPreferenceType.None; //TODO: Implement the rest of these
            case ItemInfoType.DungeonChest:
                return IngredientPreferenceType.Duty;
            case ItemInfoType.DungeonBossDrop:
                return IngredientPreferenceType.Duty;
            case ItemInfoType.DungeonBossChest:
                return IngredientPreferenceType.Duty;
            case ItemInfoType.DungeonDrop:
                return IngredientPreferenceType.Duty;
            case ItemInfoType.CustomDelivery:
                return IngredientPreferenceType.None; //TODO: Implement the rest of these
        }

        return IngredientPreferenceType.None;
    }
}