using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CriticalCommonLib.Crafting
{
    public class CraftItem : ISummable<CraftItem>
    {
        public uint ItemId;
        public ItemFlags Flags;

        [JsonIgnore]
        public Item? Item => ExcelCache.GetItem(ItemId);

        [JsonIgnore] public string FormattedName => Phase != null ? Name + " - Phase #" + (Phase + 1) : Name;

        [JsonIgnore] public string Name => Item?.Name ?? "Unknown";
        
        //The total amount that is required
        public uint QuantityRequired { get; set; } 

        //The total amount that is need given the parent materials needs
        [JsonIgnore]
        public uint QuantityNeeded;
        
        //The total amount available in your characters inventory
        [JsonIgnore]
        public uint QuantityReady;

        //The total amount available in a set of given inventories(normally retainers) excluding your active characters inventory
        [JsonIgnore]
        public uint QuantityAvailable;

        //The total amount that can be crafted, calculated from the child craft items 
        [JsonIgnore]
        public uint QuantityCanCraft;

        //The total amount missing from the users inventory
        [JsonIgnore]
        public uint QuantityMissing => (uint)Math.Max(0,(int)QuantityNeeded - QuantityReady);

        //The total amount to retrieve from retainers
        [JsonIgnore]
        public uint QuantityToRetrieve => IsOutputItem ? 0 : Math.Min(QuantityNeeded, QuantityAvailable);

        //The total amount missing from the users inventory
        [JsonIgnore]
        public uint QuantityUnavailable => (uint)Math.Max(0,(int)QuantityNeeded - QuantityReady - QuantityAvailable);

        public uint RecipeId;

        public bool IsOutputItem;
        
        //Only for company crafts
        public uint? Phase;
        
        [JsonIgnore]
        public Recipe? Recipe => RecipeId != 0 ? ExcelCache.GetRecipe(RecipeId) : null;

        [JsonIgnore]
        public uint Yield => Recipe?.AmountResult ?? 1u;

        [JsonIgnore]
        public List<CraftItem> ChildCrafts
        {
            get
            {
                if (_childCrafts == null)
                {
                    GenerateRequiredMaterials();
                    if (_childCrafts != null)
                    {
                        return _childCrafts;
                    }
                }
                else
                {
                    return _childCrafts;
                }
                return new List<CraftItem>();
            }
        }

        [JsonIgnore] private List<CraftItem>? _childCrafts = new List<CraftItem>();

        public CraftItem()
        {
            
        }
        
        public CraftItem(uint itemId, ItemFlags flags, uint quantityRequired, bool isOutputItem, uint? recipeId = null, uint? phase = null, bool flat = false)
        {
            ItemId = itemId;
            Flags = flags;
            QuantityRequired = quantityRequired;
            IsOutputItem = isOutputItem;
            Phase = phase;
            if (recipeId != null)
            {
                RecipeId = recipeId.Value;
            }

            if (ExcelCache.Initialised && !flat)
            {
                GenerateRequiredMaterials();
            }
        }

        public void SwitchPhase(uint newPhase)
        {
            Phase = newPhase;
            _childCrafts = new List<CraftItem>();
            GenerateRequiredMaterials();
        }

        public void AddQuantity(uint quantity)
        {
            QuantityRequired += quantity;
            GenerateRequiredMaterials();
        }

        public void SetQuantity(uint quantity)
        {
            QuantityRequired = quantity;
            QuantityNeeded = quantity;
            GenerateRequiredMaterials();
        }

        public void RemoveQuantity(uint quantity)
        {
            QuantityRequired = Math.Max(QuantityRequired - quantity, 0);
            GenerateRequiredMaterials();
        }

        //Generates the required materials below
        public void GenerateRequiredMaterials()
        {
            if (!ExcelCache.Initialised)
            {
                return;
            }
            
            _childCrafts = new();
            if (Recipe == null)
            {
                var recipes = ExcelCache.GetItemRecipes(ItemId);
                if (recipes.Count != 0)
                {
                    RecipeId = recipes.First().RowId;
                }
            }

            if (Recipe != null)
            {
                foreach (var material in Recipe.UnkData5)
                {
                    if (material.ItemIngredient == 0 || material.AmountIngredient == 0)
                    {
                        continue;
                    }
                    var actualAmountRequired = (QuantityRequired / Yield) * material.AmountIngredient;
                    ChildCrafts.Add(new CraftItem((uint) material.ItemIngredient, ItemFlags.None, actualAmountRequired, false));
                }
            }
            else
            {
                var companyCraftSequence = ExcelCache.GetCompanyCraftSequenceByItemId(ItemId);
                if (companyCraftSequence != null)
                {
                    foreach (var lazyPart in companyCraftSequence.CompanyCraftPart)
                    {
                        var part = lazyPart.Value;
                        if (part != null)
                        {
                            for (var index = 0; index < part.CompanyCraftProcess.Length; index++)
                            {
                                if (Phase != null && Phase != index)
                                {
                                    continue;
                                }
                                var lazyProcess = part.CompanyCraftProcess[index];
                                var process = lazyProcess.Value;
                                if (process != null)
                                {
                                    foreach (var supplyItem in process.UnkData0)
                                    {
                                        var actualItem = ExcelCache.GetSheet<CompanyCraftSupplyItem>()
                                            .GetRow(supplyItem.SupplyItem);
                                        if (actualItem != null)
                                        {
                                            if (actualItem.Item.Row != 0)
                                            {
                                                var craftItem = new CraftItem((uint) actualItem.Item.Row,
                                                    ItemFlags.None,
                                                    (uint) supplyItem.SetQuantity *
                                                    supplyItem.SetsRequired * QuantityRequired, false);
                                                ChildCrafts.Add(craftItem);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Update(Dictionary<uint, List<CraftItemSource>> characterSources,
            Dictionary<uint, List<CraftItemSource>> externalSources)
        {
            if (IsOutputItem)
            {
                QuantityNeeded = QuantityRequired;
                foreach (var craftItem in ChildCrafts)
                {
                    craftItem.QuantityNeeded = craftItem.QuantityRequired;
                    craftItem.Update(characterSources, externalSources);
                }

                if (Recipe != null)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    foreach (var ingredient in Recipe.UnkData5)
                    {
                        if (ingredient.AmountIngredient == 0 || ingredient.ItemIngredient == 0)
                        {
                            continue;
                        }
                        var ingredientId = (uint) ingredient.ItemIngredient;
                        var amountNeeded = (double)ingredient.AmountIngredient / Yield;
                        
                        foreach (var craftItem in ChildCrafts)
                        {
                            if (craftItem.ItemId == ingredientId)
                            {
                                var craftCapable = (uint)Math.Floor(craftItem.QuantityReady / amountNeeded);
                                PluginLog.Log("amount craftable for ingredient " + craftItem.ItemId + " for output item is " + craftCapable);
                                if (totalCraftCapable == null)
                                {
                                    totalCraftCapable = craftCapable;
                                }
                                else
                                {
                                    totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                                }
                            }
                        }
                    }

                    QuantityCanCraft = totalCraftCapable ?? 0;
                }
            }
            else
            {
                QuantityAvailable = 0;
                QuantityReady = 0;
                //First generate quantity ready from the character sources, only use as much as we need
                var quantityReady = 0u;
                var quantityNeeded = QuantityNeeded;
                if (characterSources.ContainsKey(ItemId))
                {
                    foreach (var characterSource in characterSources[ItemId])
                    {
                        if (quantityNeeded == 0)
                        {
                            break;
                        }
                        var stillNeeded = characterSource.UseQuantity((int) quantityNeeded);
                        quantityReady += (quantityNeeded - stillNeeded);
                        PluginLog.Log("Quantity needed for " + ItemId + ": " + quantityNeeded);
                        PluginLog.Log("Still needed for " + ItemId + ": " + stillNeeded);
                    }
                }
                PluginLog.Log("Quantity Ready for " + ItemId + ": " + quantityReady);
                QuantityReady = quantityReady;
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                var quantityMissing = QuantityMissing;
                PluginLog.Log("quantity missing: " + quantityMissing);
                if (externalSources.ContainsKey(ItemId))
                {
                    foreach (var externalSource in externalSources[ItemId])
                    {
                        if (quantityMissing == 0)
                        {
                            break;
                        }
                        var stillNeeded = externalSource.UseQuantity((int) quantityMissing);
                        PluginLog.Log("missing: " + quantityMissing);
                        PluginLog.Log("Still needed: " + stillNeeded);
                        quantityAvailable += (quantityMissing - stillNeeded);
                    }
                }

                QuantityAvailable = quantityAvailable;

                //This final figure represents the shortfall even when we include the character and external sources
                var quantityUnavailable = QuantityUnavailable;
                if (Recipe != null)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    foreach (var ingredient in Recipe.UnkData5)
                    {
                        var ingredientId = ingredient.ItemIngredient;
                        if (ingredientId == 0 || ingredient.AmountIngredient == 0)
                        {
                            continue;
                        }
                        PluginLog.Log("Recipe: " +RecipeId.ToString());
                        PluginLog.Log("Ingredient Id: " + ingredientId.ToString());
                        PluginLog.Log("Amount: " + ingredient.AmountIngredient.ToString());
                        PluginLog.Log("Yield: " + Yield.ToString());
                        PluginLog.Log("Unavaiable: " + quantityUnavailable.ToString());

                        var amountNeeded = ingredient.AmountIngredient * (Math.Ceiling((double)quantityUnavailable / Yield));
                        
                        foreach (var craftItem in ChildCrafts)
                        {
                            if (craftItem.ItemId == ingredientId)
                            {
                                craftItem.QuantityNeeded = Math.Max(0,(uint)Math.Ceiling(amountNeeded));
                                PluginLog.Log(craftItem.QuantityNeeded.ToString());
                                craftItem.Update(characterSources, externalSources);
                                var craftCapable = (uint)Math.Ceiling(craftItem.QuantityReady / (double)ingredient.AmountIngredient);
                                if (totalCraftCapable == null)
                                {
                                    totalCraftCapable = craftCapable;
                                }
                                else
                                {
                                    totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                                }
                            }
                        }
                    }
                    QuantityCanCraft = Math.Min(totalCraftCapable ?? 0, QuantityNeeded - QuantityReady);
                }
                else
                {
                    foreach (var craftItem in ChildCrafts)
                    {
                        craftItem.Update(characterSources, externalSources);
                    }
                }
            }
            
        }
        
        public List<CraftItem> GetFlattenedMaterials()
        {
            var list = new List<CraftItem>();
            
            foreach (var craftItem in ChildCrafts)
            {
                list.Add(craftItem);
                foreach (var material in craftItem.GetFlattenedMaterials())
                {
                    list.Add(material);
                }
            }

            return list;
        }

        public CraftItem Add(CraftItem a, CraftItem b)
        {
            var craftItem = new CraftItem(a.ItemId, a.Flags, a.QuantityRequired + b.QuantityRequired, a.IsOutputItem, a.RecipeId, a.Phase, true);
            craftItem.QuantityNeeded = a.QuantityNeeded + b.QuantityNeeded;
            craftItem.QuantityReady = a.QuantityReady + b.QuantityReady;
            craftItem.QuantityAvailable = a.QuantityAvailable + b.QuantityAvailable;
            craftItem.QuantityCanCraft = a.QuantityCanCraft + b.QuantityCanCraft;
            return craftItem;
        }
    }
}
