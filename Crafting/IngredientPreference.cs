using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Crafting;

public class IngredientPreference
{
    public uint ItemId { get; }
    public IngredientPreferenceType Type { get; }
    public uint? LinkedItemId { get; }
    public uint? LinkedItemQuantity { get; }
    public uint? RecipeCraftTypeId { get; }

    public IngredientPreference()
    {
        
    }

    public IngredientPreference(uint itemId, IngredientPreferenceType type, uint? linkedItemId = null, uint? linkedItemQuantity = null, uint? recipeCraftTypeId = null)
    {
        ItemId = itemId;
        Type = type;
        LinkedItemId = linkedItemId;
        LinkedItemQuantity = linkedItemQuantity;
        RecipeCraftTypeId = recipeCraftTypeId;
    }

    public IngredientPreference(IngredientPreference ingredientPreference)
    {
        ItemId = ingredientPreference.ItemId;
        Type = ingredientPreference.Type;
        LinkedItemId = ingredientPreference.LinkedItemId;
        LinkedItemQuantity = ingredientPreference.LinkedItemQuantity;
        RecipeCraftTypeId = ingredientPreference.RecipeCraftTypeId;
    }
    
    public string FormattedName
    {
        get
        {
            switch (Type)
            {
                case IngredientPreferenceType.Item:
                    if (LinkedItemId != null && LinkedItemQuantity != null)
                    {
                        var itemName =Service.ExcelCache.GetItemExSheet().GetRow(LinkedItemId.Value)?.NameString ?? "Unknown Item";
                        return itemName + " - " + LinkedItemQuantity.Value;
                    }

                    return "No item selected";
            }

            return Type.FormattedName();
        }
    }
    
    public int? SourceIcon
    {
        get
        {
            return Type switch
            {
                IngredientPreferenceType.Buy => Icons.BuyIcon,
                IngredientPreferenceType.Botany => Icons.BotanyIcon,
                IngredientPreferenceType.Crafting => Service.ExcelCache.GetCraftTypeSheet().GetRow(RecipeCraftTypeId ?? 0)?.Icon ?? Icons.CraftIcon,
                IngredientPreferenceType.Desynthesis => Icons.DesynthesisIcon,
                IngredientPreferenceType.Fishing => Icons.FishingIcon,
                IngredientPreferenceType.Item => Icons.SpecialItemIcon,
                IngredientPreferenceType.Marketboard => Icons.MarketboardIcon,
                IngredientPreferenceType.Mining => Icons.MiningIcon,
                IngredientPreferenceType.Mobs => Icons.MobIcon,
                IngredientPreferenceType.None => null,
                IngredientPreferenceType.Reduction => Icons.ReductionIcon,
                IngredientPreferenceType.Venture => Icons.VentureIcon,
                IngredientPreferenceType.ResourceInspection => Icons.SkybuildersScripIcon,
                IngredientPreferenceType.Gardening => Icons.SproutIcon,
                _ => Icons.QuestionMarkIcon
            };
        }
    }
}