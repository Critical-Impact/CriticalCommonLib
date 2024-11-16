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
        this.ItemId = itemId;
        this.Type = type;
        this.LinkedItemId = linkedItemId;
        this.LinkedItemQuantity = linkedItemQuantity;
        this.RecipeCraftTypeId = recipeCraftTypeId;
    }

    public void SetSecondItem(uint linkedItemId, uint linkedItemQuantity)
    {
        if (this.LinkedItem2Id == null)
        {
            this.LinkedItem2Id = linkedItemId;
        }

        if (this.LinkedItem2Quantity == null)
        {
            this.LinkedItem2Quantity = linkedItemQuantity;
        }
    }

    public void SetThirdItem(uint linkedItemId, uint linkedItemQuantity)
    {
        if (this.LinkedItem3Id == null)
        {
            this.LinkedItem3Id = linkedItemId;
        }

        if (this.LinkedItem3Quantity == null)
        {
            this.LinkedItem3Quantity = linkedItemQuantity;
        }
    }

    public IngredientPreference(IngredientPreference ingredientPreference)
    {
        this.ItemId = ingredientPreference.ItemId;
        this.Type = ingredientPreference.Type;
        this.LinkedItemId = ingredientPreference.LinkedItemId;
        this.LinkedItemQuantity = ingredientPreference.LinkedItemQuantity;
        this.RecipeCraftTypeId = ingredientPreference.RecipeCraftTypeId;
        if (ingredientPreference.LinkedItem2Id != null && ingredientPreference.LinkedItem2Quantity != null)
        {
            this.SetSecondItem((uint)ingredientPreference.LinkedItem2Id, (uint)ingredientPreference.LinkedItem2Quantity);
        }
        if (ingredientPreference.LinkedItem3Id != null && ingredientPreference.LinkedItem3Quantity != null)
        {
            this.SetThirdItem((uint)ingredientPreference.LinkedItem3Id, (uint)ingredientPreference.LinkedItem3Quantity);
        }
    }
    [JsonIgnore]
    public string FormattedName
    {
        get
        {
            switch (this.Type)
            {
                case IngredientPreferenceType.Item:
                    if (this.LinkedItemId != null && this.LinkedItemQuantity != null)
                    {
                        string? itemName2 = null;
                        string? itemName3 = null;
                        if (this.LinkedItem2Id != null && this.LinkedItem2Quantity != null)
                        {
                            if (this.LinkedItem3Id != null && this.LinkedItem3Quantity != null)
                            {
                                itemName3 = (Service.ExcelCache.GetItemSheet().GetRow(this.LinkedItem3Id.Value)?.NameString ?? "Unknown Item")  + " - " + this.LinkedItem3Quantity.Value;
                            }
                            itemName2 = (Service.ExcelCache.GetItemSheet().GetRow(this.LinkedItem2Id.Value)?.NameString ?? "Unknown Item") + " - " + this.LinkedItem2Quantity.Value;
                        }

                        var itemName =Service.ExcelCache.GetItemSheet().GetRow(this.LinkedItemId.Value)?.NameString ?? "Unknown Item";
                        if (itemName3 != null)
                        {
                            itemName = itemName + "," + itemName2 + "," + itemName3;
                        }
                        else if (itemName2 != null)
                        {
                            itemName = itemName + "," + itemName2;
                        }
                        return itemName + " - " + this.LinkedItemQuantity.Value;
                    }

                    return "No item selected";
                case IngredientPreferenceType.Reduction:
                    if (this.LinkedItemId != null && this.LinkedItemQuantity != null)
                    {
                        var itemName =Service.ExcelCache.GetItemSheet().GetRow(this.LinkedItemId.Value)?.NameString ?? "Unknown Item";
                        return "Reduction (" + itemName + " - " + this.LinkedItemQuantity.Value + ")";
                    }

                    return "No item selected";
            }

            return this.Type.FormattedName();
        }
    }
    [JsonIgnore]
    public int? SourceIcon
    {
        get
        {
            return this.Type switch
            {
                IngredientPreferenceType.Buy => Icons.BuyIcon,
                IngredientPreferenceType.HouseVendor => Icons.BuyIcon,
                IngredientPreferenceType.Botany => Icons.BotanyIcon,
                IngredientPreferenceType.Crafting => Service.ExcelCache.GetCraftTypeSheet().GetRow(this.RecipeCraftTypeId ?? 0)?.Icon ?? Icons.CraftIcon,
                IngredientPreferenceType.Desynthesis => Icons.DesynthesisIcon,
                IngredientPreferenceType.Fishing => Icons.FishingIcon,
                IngredientPreferenceType.Item => this.LinkedItemId != null ? Service.ExcelCache.GetItemSheet().GetRowOrDefault(this.LinkedItemId.Value)?.Icon ?? Icons.SpecialItemIcon: Icons.SpecialItemIcon,
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
        if (this.ItemId != ingredientPreference.ItemId)
        {
            return false;
        }
        if (this.LinkedItemId != ingredientPreference.LinkedItemId)
        {
            return false;
        }
        if (this.LinkedItem2Id != ingredientPreference.LinkedItem2Id)
        {
            return false;
        }
        if (this.LinkedItem3Id != ingredientPreference.LinkedItem3Id)
        {
            return false;
        }
        if (this.LinkedItemQuantity != ingredientPreference.LinkedItemQuantity)
        {
            return false;
        }
        if (this.LinkedItem2Quantity != ingredientPreference.LinkedItem2Quantity)
        {
            return false;
        }
        if (this.LinkedItem3Quantity != ingredientPreference.LinkedItem3Quantity)
        {
            return false;
        }

        return true;
    }
}