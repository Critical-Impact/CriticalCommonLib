using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using Newtonsoft.Json;

namespace CriticalCommonLib.Crafting;

public class IngredientPreference
{
    [JsonProperty]
    public uint ItemId { get; private set; }
    [JsonProperty]
    public IngredientPreferenceType Type { get; private set; }
    [JsonProperty]
    public uint? LinkedItemId { get; private set; }
    [JsonProperty]
    public uint? LinkedItemQuantity { get; private set; }
    [JsonProperty]
    public uint? LinkedItem2Id { get; private set; }
    [JsonProperty]
    public uint? LinkedItem2Quantity { get; private set; }
    [JsonProperty]
    public uint? LinkedItem3Id { get; private set; }
    [JsonProperty]
    public uint? LinkedItem3Quantity { get; private set; }
    [JsonProperty]
    public uint? RecipeCraftTypeId { get; private set; }

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

    public void SetSecondItem(uint linkedItemId, uint linkedItemQuantity)
    {
        if (LinkedItem2Id == null)
        {
            LinkedItem2Id = linkedItemId;
        }

        if (LinkedItem2Quantity == null)
        {
            LinkedItem2Quantity = linkedItemQuantity;
        }
    }

    public void SetThirdItem(uint linkedItemId, uint linkedItemQuantity)
    {
        if (LinkedItem3Id == null)
        {
            LinkedItem3Id = linkedItemId;
        }

        if (LinkedItem3Quantity == null)
        {
            LinkedItem3Quantity = linkedItemQuantity;
        }
    }

    public IngredientPreference(IngredientPreference ingredientPreference)
    {
        ItemId = ingredientPreference.ItemId;
        Type = ingredientPreference.Type;
        LinkedItemId = ingredientPreference.LinkedItemId;
        LinkedItemQuantity = ingredientPreference.LinkedItemQuantity;
        RecipeCraftTypeId = ingredientPreference.RecipeCraftTypeId;
        if (ingredientPreference.LinkedItem2Id != null && ingredientPreference.LinkedItem2Quantity != null)
        {
            SetSecondItem((uint)ingredientPreference.LinkedItem2Id, (uint)ingredientPreference.LinkedItem2Quantity);
        }
        if (ingredientPreference.LinkedItem3Id != null && ingredientPreference.LinkedItem3Quantity != null)
        {
            SetThirdItem((uint)ingredientPreference.LinkedItem3Id, (uint)ingredientPreference.LinkedItem3Quantity);
        }
    }
    [JsonIgnore]
    public string FormattedName
    {
        get
        {
            switch (Type)
            {
                case IngredientPreferenceType.Item:
                    if (LinkedItemId != null && LinkedItemQuantity != null)
                    {
                        string? itemName2 = null;
                        string? itemName3 = null;
                        if (LinkedItem2Id != null && LinkedItem2Quantity != null)
                        {
                            if (LinkedItem3Id != null && LinkedItem3Quantity != null)
                            {
                                itemName3 = (Service.ExcelCache.GetItemExSheet().GetRow(LinkedItem3Id.Value)?.NameString ?? "Unknown Item")  + " - " + LinkedItem3Quantity.Value;
                            }
                            itemName2 = (Service.ExcelCache.GetItemExSheet().GetRow(LinkedItem2Id.Value)?.NameString ?? "Unknown Item") + " - " + LinkedItem2Quantity.Value;
                        }

                        var itemName =Service.ExcelCache.GetItemExSheet().GetRow(LinkedItemId.Value)?.NameString ?? "Unknown Item";
                        if (itemName3 != null)
                        {
                            itemName = itemName + "," + itemName2 + "," + itemName3;
                        }
                        else if (itemName2 != null)
                        {
                            itemName = itemName + "," + itemName2;
                        }
                        return itemName + " - " + LinkedItemQuantity.Value;
                    }

                    return "No item selected";
            }

            return Type.FormattedName();
        }
    }
    [JsonIgnore]
    public int? SourceIcon
    {
        get
        {
            return Type switch
            {
                IngredientPreferenceType.Buy => Icons.BuyIcon,
                IngredientPreferenceType.HouseVendor => Icons.BuyIcon,
                IngredientPreferenceType.Botany => Icons.BotanyIcon,
                IngredientPreferenceType.Crafting => Service.ExcelCache.GetCraftTypeSheet().GetRow(RecipeCraftTypeId ?? 0)?.Icon ?? Icons.CraftIcon,
                IngredientPreferenceType.Desynthesis => Icons.DesynthesisIcon,
                IngredientPreferenceType.Fishing => Icons.FishingIcon,
                IngredientPreferenceType.Item => LinkedItemId != null ? Service.ExcelCache.GetItemExSheet().GetRow(LinkedItemId.Value)?.Icon ?? Icons.SpecialItemIcon: Icons.SpecialItemIcon,
                IngredientPreferenceType.Marketboard => Icons.MarketboardIcon,
                IngredientPreferenceType.Mining => Icons.MiningIcon,
                IngredientPreferenceType.Mobs => Icons.MobIcon,
                IngredientPreferenceType.None => null,
                IngredientPreferenceType.Reduction => Icons.ReductionIcon,
                IngredientPreferenceType.Venture => Icons.VentureIcon,
                IngredientPreferenceType.ExplorationVenture => Icons.VentureIcon,
                IngredientPreferenceType.Empty => Icons.RedXIcon,
                IngredientPreferenceType.ResourceInspection => Icons.SkybuildersScripIcon,
                IngredientPreferenceType.Gardening => Icons.SproutIcon,
                _ => Icons.QuestionMarkIcon
            };
        }
    }

    public bool SameItems(IngredientPreference ingredientPreference)
    {
        if (ItemId != ingredientPreference.ItemId)
        {
            return false;
        }
        if (LinkedItemId != ingredientPreference.LinkedItemId)
        {
            return false;
        }
        if (LinkedItem2Id != ingredientPreference.LinkedItem2Id)
        {
            return false;
        }
        if (LinkedItem3Id != ingredientPreference.LinkedItem3Id)
        {
            return false;
        }
        if (LinkedItemQuantity != ingredientPreference.LinkedItemQuantity)
        {
            return false;
        }
        if (LinkedItem2Quantity != ingredientPreference.LinkedItem2Quantity)
        {
            return false;
        }
        if (LinkedItem3Quantity != ingredientPreference.LinkedItem3Quantity)
        {
            return false;
        }

        return true;
    }
}