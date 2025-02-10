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