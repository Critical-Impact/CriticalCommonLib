using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Sheets;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Crafting
{
    public class CraftItem : ISummable<CraftItem>
    {
        public uint ItemId;
        public InventoryItem.ItemFlags Flags;

        [JsonIgnore]
        public ItemEx Item => Service.ExcelCache.GetItemExSheet().GetRow(ItemId)!;

        [JsonIgnore] public string FormattedName => Phase != null ? Name + " - Phase #" + (Phase + 1) : Name;

        [JsonIgnore] public string Name => Item.NameString;
        
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
        public RecipeEx? Recipe => RecipeId != 0 ? Service.ExcelCache.GetRecipe(RecipeId) : null;

        [JsonIgnore]
        public uint Yield => Recipe?.AmountResult ?? 1u;

        public void ClearChildCrafts()
        {
            _childCrafts = null;
        }
        
        
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

        public List<CraftItem> GetChildCrafts(Dictionary<uint, double> spareIngredients)
        {
            if (_childCrafts == null)
            {
                GenerateRequiredMaterials(spareIngredients);
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

        [JsonIgnore] private List<CraftItem>? _childCrafts = new List<CraftItem>();

        public CraftItem()
        {
            
        }
        
        public CraftItem(uint itemId, InventoryItem.ItemFlags flags, uint quantityRequired, bool isOutputItem, uint? recipeId = null, uint? phase = null, bool flat = false, Dictionary<uint, double>? spareIngredients = null)
        {
            ItemId = itemId;
            Flags = flags;
            QuantityRequired = quantityRequired;
            QuantityNeeded = quantityRequired;
            IsOutputItem = isOutputItem;
            Phase = phase;
            if (recipeId != null)
            {
                RecipeId = recipeId.Value;
            }

            if (spareIngredients == null)
            {
                spareIngredients = new Dictionary<uint, double>();
            }

            if (!flat)
            {
                GenerateRequiredMaterials(spareIngredients);
            }
        }

        public void SwitchRecipe(uint newRecipeId)
        {
            RecipeId = newRecipeId;
            _childCrafts = new List<CraftItem>();
        }

        public void SwitchPhase(uint newPhase)
        {
            Phase = newPhase;
            _childCrafts = new List<CraftItem>();
        }

        public void AddQuantity(uint quantity)
        {
            QuantityRequired += quantity;
        }

        public void SetQuantity(uint quantity)
        {
            QuantityRequired = quantity;
            QuantityNeeded = quantity;
        }

        public void RemoveQuantity(uint quantity)
        {
            QuantityRequired = Math.Max(QuantityRequired - quantity, 0);
        }

        //Generates the required materials below
        public void GenerateRequiredMaterials(Dictionary<uint, double>? spareIngredients = null)
        {
            if (spareIngredients == null)
            {
                spareIngredients = new Dictionary<uint, double>();
            }
            _childCrafts = new();
            if (QuantityRequired == 0)
            {
                return;
            }
            if (Recipe == null)
            {
                if (Service.ExcelCache.ItemRecipes.ContainsKey(ItemId))
                {
                    var recipes = Service.ExcelCache.ItemRecipes[ItemId];
                    if (recipes.Count != 0)
                    {
                        RecipeId = recipes.First();
                    }
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

                    var materialItemId = (uint)material.ItemIngredient;
                    var materialAmountIngredient = (uint)material.AmountIngredient;

                    var quantityRequired = (double)QuantityRequired;
                    

                    
                    var actualAmountRequired = (uint)(Math.Max(1, Math.Ceiling(quantityRequired / Yield)) * materialAmountIngredient);
                    var actualAmountUsed = (uint)(Math.Max(1, quantityRequired / Yield) * material.AmountIngredient);
                    
                    if (spareIngredients.ContainsKey(materialItemId))
                    {
                        //Factor in the possible extra we get and then 
                        var extraSpare = Math.Max(0,actualAmountRequired - actualAmountUsed);
                        var amountAvailable = Math.Max(0,Math.Min(quantityRequired, spareIngredients[materialItemId]));
                        actualAmountRequired -= (uint)amountAvailable + extraSpare;
                        spareIngredients[materialItemId] -= amountAvailable + extraSpare;
                    }
                    
                    var amountLeftOver = actualAmountRequired - actualAmountUsed;
                    if (amountLeftOver != 0)
                    {
                        if (!spareIngredients.ContainsKey(materialItemId))
                        {
                            spareIngredients[materialItemId] = 0;
                        }

                        spareIngredients[materialItemId] += amountLeftOver;
                    }
                    GetChildCrafts(spareIngredients).Add(new CraftItem(materialItemId, InventoryItem.ItemFlags.None, actualAmountRequired, false, spareIngredients: spareIngredients));
                }
            }
            else
            {
                var companyCraftSequence = Service.ExcelCache.GetCompanyCraftSequenceByItemId(ItemId);
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
                                        var actualItem = Service.ExcelCache.GetCompanyCraftSupplyItemSheet()
                                            .GetRow(supplyItem.SupplyItem);
                                        if (actualItem != null)
                                        {
                                            if (actualItem.ItemEx.Row != 0 && actualItem.ItemEx.Value != null && actualItem.ItemEx.Value.CanBeCrafted)
                                            {
                                                var craftItem = new CraftItem((uint) actualItem.Item.Row,
                                                    InventoryItem.ItemFlags.None,
                                                    (uint) supplyItem.SetQuantity *
                                                    supplyItem.SetsRequired * QuantityRequired, false, spareIngredients: spareIngredients);
                                                GetChildCrafts(spareIngredients).Add(craftItem);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Service.ExcelCache.HwdInspectionResults.ContainsKey(ItemId))
                {
                    var requirements = Service.ExcelCache.HwdInspectionResults[ItemId];
                    var craftItem = new CraftItem((uint) requirements.Item1,
                        InventoryItem.ItemFlags.None,
                        (uint) requirements.Item2 * QuantityRequired, false, spareIngredients: spareIngredients);
                    GetChildCrafts(spareIngredients).Add(craftItem);
                }
                
            }
        }

        public void Update(Dictionary<uint, List<CraftItemSource>> characterSources,
            Dictionary<uint, List<CraftItemSource>> externalSources)
        {
            if (IsOutputItem)
            {
                QuantityNeeded = QuantityRequired;
                for (var index = 0; index < ChildCrafts.Count; index++)
                {
                    var craftItem = ChildCrafts[index];
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

                        for (var index = 0; index < ChildCrafts.Count; index++)
                        {
                            var craftItem = ChildCrafts[index];
                            if (craftItem.ItemId == ingredientId)
                            {
                                var craftCapable = (uint)Math.Floor(craftItem.QuantityReady / amountNeeded);
                                //PluginLog.Log("amount craftable for ingredient " + craftItem.ItemId + " for output item is " + craftCapable);
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

                    QuantityCanCraft = totalCraftCapable * Yield ?? 0;
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
                        //PluginLog.Log("Quantity needed for " + ItemId + ": " + quantityNeeded);
                        //PluginLog.Log("Still needed for " + ItemId + ": " + stillNeeded);
                    }
                }
                //PluginLog.Log("Quantity Ready for " + ItemId + ": " + quantityReady);
                QuantityReady = quantityReady;
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                var quantityMissing = QuantityMissing;
                //PluginLog.Log("quantity missing: " + quantityMissing);
                if (externalSources.ContainsKey(ItemId))
                {
                    foreach (var externalSource in externalSources[ItemId])
                    {
                        //PluginLog.Log("found external source for " + ItemId + " is " + externalSource.Quantity);
                        if (quantityMissing == 0)
                        {
                            break;
                        }
                        var stillNeeded = externalSource.UseQuantity((int) quantityMissing);
                        //PluginLog.Log("missing: " + quantityMissing);
                        //PluginLog.Log("Still needed: " + stillNeeded);
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
                    var totalAmountNeeded = QuantityNeeded - QuantityReady;
                    var oldQuantityRequired = QuantityRequired;
                    QuantityRequired = totalAmountNeeded;
                    GenerateRequiredMaterials();
                    QuantityRequired = oldQuantityRequired;
                    foreach (var childCraft in ChildCrafts)
                    {
                        var amountNeeded = childCraft.QuantityRequired;

                        childCraft.QuantityNeeded = Math.Max(0, amountNeeded);
                        childCraft.Update(characterSources, externalSources);
                        var craftCapable = (uint)Math.Ceiling(childCraft.QuantityReady / (double)Recipe.GetRecipeItemAmount(childCraft.ItemId));
                        if (totalCraftCapable == null)
                        {
                            totalCraftCapable = craftCapable;
                        }
                        else
                        {
                            totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                        }
                    }

                    QuantityCanCraft = Math.Min(totalCraftCapable * Yield  ?? 0, totalAmountNeeded);
                }
                else if (Service.ExcelCache.HwdInspectionResults.ContainsKey(ItemId))
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var inspectionMap = Service.ExcelCache.HwdInspectionResults[ItemId];
                    var ingredientId = inspectionMap.Item1;
                    var amount = inspectionMap.Item2;
                    if (ingredientId == 0 || amount == 0)
                    {
                        return;
                    }

                    var amountNeeded = amount * (Math.Ceiling((double)quantityUnavailable / Yield));

                    for (var index = 0; index < ChildCrafts.Count; index++)
                    {
                        var craftItem = ChildCrafts[index];
                        if (craftItem.ItemId == ingredientId)
                        {
                            craftItem.QuantityNeeded = Math.Max(0, (uint)Math.Ceiling(amountNeeded));
                            //PluginLog.Log(craftItem.QuantityNeeded.ToString());
                            craftItem.Update(characterSources, externalSources);
                            var craftCapable =
                                (uint)Math.Ceiling(craftItem.QuantityReady / (double)amount);
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
                    QuantityCanCraft = Math.Min(totalCraftCapable * Yield  ?? 0, QuantityNeeded - QuantityReady);
                }
                else
                {
                    for (var index = 0; index < ChildCrafts.Count; index++)
                    {
                        var craftItem = ChildCrafts[index];
                        craftItem.Update(characterSources, externalSources);
                    }
                }
            }
            
        }
        
        public List<CraftItem> GetFlattenedMaterials()
        {
            var list = new List<CraftItem>();

            for (var index = 0; index < ChildCrafts.Count; index++)
            {
                var craftItem = ChildCrafts[index];
                list.Add(craftItem);
                var items = craftItem.GetFlattenedMaterials();
                for (var i = 0; i < items.Count; i++)
                {
                    var material = items[i];
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
