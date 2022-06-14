using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CriticalCommonLib.Crafting
{
    public class CraftItem
    {
        public uint ItemId;
        public ItemFlags Flags;
        private uint _recipeRequired;
        public uint QuantityRequired;
        public uint QuantityMade;
        public bool IsOutputItem;
        public uint Yield;
        public uint RecipeId;
        private List<CraftItem>? _requiredMaterials;

        [JsonIgnore]
        public uint QuantityLeft => (uint)Math.Ceiling((double)(QuantityRequired - QuantityMade) / Yield);
        
        [JsonIgnore]
        public uint QuantityLeftYieldless => QuantityRequired - QuantityMade;
        
        public CraftItem(uint itemId, ItemFlags flags, uint quantityRequired, uint recipeRequired, bool isOutputItem)
        {
            ItemId = itemId;
            Flags = flags;
            _recipeRequired = recipeRequired;
            QuantityMade = 0;
            IsOutputItem = isOutputItem;
            var recipes = ExcelCache.GetItemRecipes(ItemId);
            if (recipes.Count != 0)
            {
                Yield = recipes.First().AmountResult;
            }
            else
            {
                Yield = 1;
            }

            QuantityRequired = quantityRequired;

        }

        [JsonIgnore]
        public Item? Item
        {
            get
            {
                return ExcelCache.GetItem(ItemId);
            }
        }

        public void RecalculateRequiredQuantities(Dictionary<uint, List<CraftItemSource>> craftSources)
        {
            var requiredMaterials = GetRequiredMaterials();
            if (!IsOutputItem)
            {
                if (craftSources.ContainsKey(ItemId))
                {
                    var amountNeeded = (int) QuantityLeftYieldless;
                    foreach (var source in craftSources[ItemId])
                    {
                        amountNeeded = source.UseQuantity(amountNeeded);
                        if (amountNeeded == 0)
                        {
                            break;
                        }
                    }

                    QuantityRequired = (uint)amountNeeded / Yield;
                }
            }

            foreach (var ingredient in requiredMaterials)
            {
                ingredient.QuantityRequired = ingredient._recipeRequired * QuantityRequired;
                ingredient.RecalculateRequiredQuantities(craftSources);
            }
        }

        private void CalculateRequiredMaterials()
        {
            var materials = new List<CraftItem>();
            var item = ExcelCache.GetItem(ItemId);
            if (item != null)
            {
                var recipes = ExcelCache.GetItemRecipes(ItemId);
                if (recipes.Count != 0)
                {
                    //Maybe add in some sort of preferential system or checks to see if they can craft it
                    var recipe = recipes.First();
                    RecipeId = recipe.RowId;
                    foreach (var ingredient in recipe.UnkData5)
                    {
                        if (ingredient.ItemIngredient != 0 && ingredient.AmountIngredient != 0)
                        {

                            
                            var craftItem = new CraftItem((uint) ingredient.ItemIngredient, ItemFlags.None,
                                ingredient.AmountIngredient * QuantityRequired,ingredient.AmountIngredient ,false);
                            materials.Add(craftItem);
                            craftItem.GetRequiredMaterials();
                        }
                    }
                }
            }

            _requiredMaterials = materials;
        }

        public static Dictionary<uint, uint> GetMaterialTotals(List<CraftItem> materials)
        {
            Dictionary<uint, uint> totals = new();
            foreach (var material in materials)
            {
                if (!totals.ContainsKey(material.ItemId))
                {
                    totals[material.ItemId] = 0;
                }

                totals[material.ItemId] += material.QuantityRequired;
            }

            return totals;
        }

        public List<CraftItem> GetFlattenedMaterials()
        {
            var materials = new List<CraftItem>();
            foreach (var craftItem in GetRequiredMaterials())
            {
                materials.Add(craftItem);
                foreach (var item in craftItem.GetFlattenedMaterials())
                {
                    materials.Add(item);
                }
            }

            return materials;
        }

        public List<CraftItem> GetRequiredMaterials()
        {
            if (_requiredMaterials != null)
            {
                return _requiredMaterials;
            }
            CalculateRequiredMaterials();
    
            return _requiredMaterials ?? new List<CraftItem>();
        }
    }
}