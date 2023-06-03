using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.MarketBoard;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;

namespace CriticalCommonLib.Crafting
{
    public class CraftList
    {
        private List<CraftItem>? _craftItems = new();

        [JsonIgnore] public bool BeenUpdated = false;
        [JsonIgnore] public bool BeenGenerated = false;
        [JsonIgnore] public uint MinimumNQCost = 0;
        [JsonIgnore] public uint MinimumHQCost = 0;

        public List<CraftItem> CraftItems
        {
            get
            {
                if (_craftItems == null)
                {
                    _craftItems = new List<CraftItem>();
                }
                return _craftItems;
            }
        }

        public void CalculateCosts(IMarketCache marketCache)
        {
            //Fix me later
            var minimumNQCost = 0u;
            var minimumHQCost = 0u;
            var list = GetFlattenedMergedMaterials();
            for (var index = 0; index < list.Count; index++)
            {
                var craftItem = list[index];
                if (!craftItem.IsOutputItem)
                {
                    var priceData = marketCache.GetPricing(craftItem.ItemId, false);
                    if (priceData != null)
                    {
                        if (priceData.minPriceHQ != 0)
                        {
                            minimumHQCost += (uint)(priceData.minPriceHQ * craftItem.QuantityNeeded);
                        }
                        else
                        {
                            minimumHQCost += (uint)(priceData.minPriceNQ * craftItem.QuantityNeeded);
                        }

                        minimumNQCost += (uint)(priceData.minPriceNQ * craftItem.QuantityNeeded);
                    }
                }
            }

            MinimumNQCost = minimumNQCost;
            MinimumHQCost = minimumHQCost;
        }

        public void AddCraftItem(uint itemId, uint quantity = 1, InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
        {
            var item = Service.ExcelCache.GetItemExSheet().GetRow(itemId);
            if (item != null && item.CanBeCrafted)
            {
                if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase))
                {
                    var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                    craftItem.AddQuantity(quantity);
                }
                else
                {
                    var newCraftItems = CraftItems.ToList();
                    newCraftItems.Add(new CraftItem(itemId, flags, quantity, null, true, null, phase));
                    _craftItems = newCraftItems;
                }
                GenerateCraftChildren();
            }
        }

        public void AddCompanyCraftItem(uint itemId, uint quantity, uint phase, bool includeItem = false, CompanyCraftStatus status = CompanyCraftStatus.Normal)
        {
            if (includeItem)
            {
                AddCraftItem(itemId, quantity, InventoryItem.ItemFlags.None, phase);
                return;
            }
        }

        public void SetCraftRecipe(uint itemId, uint newRecipeId)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.IsOutputItem))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.IsOutputItem);
                craftItem.SwitchRecipe(newRecipeId);
                GenerateCraftChildren();
            }
        }

        public void SetCraftRequiredQuantity(uint itemId, uint quantity, InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                craftItem.SetQuantity(quantity);
                GenerateCraftChildren();
            }
        }
        
        public void RemoveCraftItem(uint itemId, InventoryItem.ItemFlags itemFlags)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags))
            {
                var withRemoved = CraftItems.ToList();
                withRemoved.RemoveAll(c => c.ItemId == itemId && c.Flags == itemFlags);
                _craftItems = withRemoved;
                GenerateCraftChildren();
            }
        }
        
        public void RemoveCraftItem(uint itemId, uint quantity, InventoryItem.ItemFlags itemFlags)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags))
            {
                var withRemoved = CraftItems.ToList();
                var totalRequired = withRemoved.Where(c =>  c.ItemId == itemId && c.Flags == itemFlags).Sum( c => c.QuantityRequired);
                if (totalRequired > quantity)
                {
                    SetCraftRequiredQuantity(itemId, (uint)(totalRequired - quantity), itemFlags);
                }
                else
                {
                    withRemoved.RemoveAll(c => c.ItemId == itemId && c.Flags == itemFlags);
                    _craftItems = withRemoved;
                }
                GenerateCraftChildren();
            }
        }

        public void GenerateCraftChildren()
        {
            var leftOvers = new Dictionary<uint, double>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                craftItem.ClearChildCrafts();
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem, leftOvers);
            }
            BeenGenerated = true;
        }
        
        /// <summary>
        /// Generates the required materials within a craft item.
        /// </summary>
        /// <param name="craftItem"></param>
        /// <param name="spareIngredients"></param>
        /// <returns></returns>
        private List<CraftItem> CalculateChildCrafts(CraftItem craftItem, Dictionary<uint, double>? spareIngredients = null)
        {
            if (spareIngredients == null)
            {
                spareIngredients = new Dictionary<uint, double>();
            }
            var childCrafts = new List<CraftItem>();

            if (craftItem.QuantityRequired == 0)
            {
                return childCrafts;
            }
            if (craftItem.Recipe == null)
            {
                if (Service.ExcelCache.ItemRecipes.ContainsKey(craftItem.ItemId))
                {
                    var recipes = Service.ExcelCache.ItemRecipes[craftItem.ItemId];
                    if (recipes.Count != 0)
                    {
                        craftItem.RecipeId = recipes.First();
                    }
                }
            }

            if (craftItem.Recipe != null)
            {
                var craftAmountNeeded = Math.Max(0, Math.Ceiling((double)craftItem.QuantityNeeded / craftItem.Yield)) * craftItem.Yield;
                var craftAmountUsed = craftItem.QuantityNeeded;
                var amountLeftOver = craftAmountNeeded - craftAmountUsed;
                if (amountLeftOver > 0)
                {
                    if (!spareIngredients.ContainsKey(craftItem.ItemId))
                    {
                        spareIngredients[craftItem.ItemId] = 0;
                    }

                    spareIngredients[craftItem.ItemId] += amountLeftOver;
                }
                
                foreach (var material in craftItem.Recipe.UnkData5)
                {
                    if (material.ItemIngredient == 0 || material.AmountIngredient == 0)
                    {
                        continue;
                    }

                    var materialItemId = (uint)material.ItemIngredient;
                    var materialAmountIngredient = (uint)material.AmountIngredient;

                    var quantityNeeded = (double)craftItem.QuantityNeeded;
                    var quantityRequired = (double)craftItem.QuantityRequired;
                    
                    var actualAmountNeeded = Math.Max(0, Math.Ceiling(quantityNeeded / craftItem.Yield)) * materialAmountIngredient;
                    var actualAmountUsed = Math.Max(0, quantityNeeded / craftItem.Yield) * material.AmountIngredient;

                    var actualAmountRequired = Math.Max(0, Math.Ceiling(quantityRequired / craftItem.Yield)) * materialAmountIngredient;
                    
                    if (spareIngredients.ContainsKey(materialItemId))
                    {
                        //Factor in the possible extra we get and then 
                        var amountAvailable = Math.Max(0,Math.Min(quantityNeeded, spareIngredients[materialItemId]));
                        actualAmountRequired -= amountAvailable;
                        actualAmountNeeded -= amountAvailable;
                        spareIngredients[materialItemId] -= amountAvailable;
                    }
                    


                    var childCraftItem = new CraftItem(materialItemId, InventoryItem.ItemFlags.None, (uint)actualAmountRequired, (uint)actualAmountNeeded, false);
                    childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients);
                    childCrafts.Add(childCraftItem);
                }
            }
            else
            {
                var companyCraftSequence = Service.ExcelCache.GetCompanyCraftSequenceByItemId(craftItem.ItemId);
                if (companyCraftSequence != null)
                {
                    foreach (var lazyPart in companyCraftSequence.CompanyCraftPart)
                    {
                        var part = lazyPart.Value;
                        if (part != null)
                        {
                            for (var index = 0; index < part.CompanyCraftProcess.Length; index++)
                            {
                                if (craftItem.Phase != null && craftItem.Phase != index)
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
                                            if (actualItem.ItemEx.Row != 0 && actualItem.ItemEx.Value != null)
                                            {
                                                var childCraftItem = new CraftItem((uint) actualItem.Item.Row, InventoryItem.ItemFlags.None, (uint) supplyItem.SetQuantity * supplyItem.SetsRequired * craftItem.QuantityRequired, (uint) supplyItem.SetQuantity * supplyItem.SetsRequired * craftItem.QuantityNeeded, false);
                                                childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients);
                                                childCrafts.Add(childCraftItem);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (Service.ExcelCache.HwdInspectionResults.ContainsKey(craftItem.ItemId))
                {
                    var requirements = Service.ExcelCache.HwdInspectionResults[craftItem.ItemId];
                    var quantityNeeded = 0u;
                    var quantityRequired = 0u;
                    if (requirements.Item2 != 0)
                    {
                        quantityNeeded = (uint)Math.Ceiling((double)craftItem.QuantityNeeded / requirements.Item2) * requirements.Item2;
                        quantityRequired = (uint)Math.Ceiling((double)craftItem.QuantityRequired / requirements.Item2) * requirements.Item2;
                    }
                    var childCraftItem = new CraftItem((uint) requirements.Item1, InventoryItem.ItemFlags.None, quantityRequired, quantityNeeded, false);
                    childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients);
                    childCrafts.Add(childCraftItem);
                }
            }

            return childCrafts;
        }

        /// <summary>
        /// Updates an already generated craft item, passing in the items a player has on their person and within retainers to calculate the total amount that will be required.
        /// </summary>
        /// <param name="characterSources"></param>
        /// <param name="externalSources"></param>
        /// <param name="cascadeCrafts"></param>
        public void UpdateCraftItem(CraftItem craftItem, Dictionary<uint, List<CraftItemSource>> characterSources,
            Dictionary<uint, List<CraftItemSource>> externalSources, Dictionary<uint, double>? spareIngredients = null, bool cascadeCrafts = false)
        {
            if (craftItem.IsOutputItem)
            {
                craftItem.QuantityNeeded = craftItem.QuantityRequired;
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem,spareIngredients);
                for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                {
                    var childCraftItem = craftItem.ChildCrafts[index];
                    UpdateCraftItem(childCraftItem, characterSources, externalSources,spareIngredients, cascadeCrafts);
                }

                if (craftItem.Recipe != null)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    foreach (var ingredient in craftItem.Recipe.UnkData5)
                    {
                        if (ingredient.AmountIngredient == 0 || ingredient.ItemIngredient == 0)
                        {
                            continue;
                        }
                        var ingredientId = (uint) ingredient.ItemIngredient;
                        var amountNeeded = (double)ingredient.AmountIngredient / craftItem.Yield;

                        for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                        {
                            var childCraftItem = craftItem.ChildCrafts[index];
                            if (childCraftItem.ItemId == ingredientId)
                            {
                                var craftItemQuantityReady = childCraftItem.QuantityReady;
                                if (cascadeCrafts)
                                {
                                    craftItemQuantityReady += childCraftItem.QuantityCanCraft;
                                }
                                var craftCapable = (uint)Math.Floor(craftItemQuantityReady / amountNeeded);
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

                    craftItem.QuantityCanCraft = totalCraftCapable * craftItem.Yield ?? 0;
                }
            }
            else
            {
                craftItem.QuantityAvailable = 0;
                craftItem.QuantityReady = 0;
                //First generate quantity ready from the character sources, only use as much as we need
                var quantityReady = 0u;
                var quantityNeeded = craftItem.QuantityNeeded;
                if (characterSources.ContainsKey(craftItem.ItemId))
                {
                    foreach (var characterSource in characterSources[craftItem.ItemId])
                    {
                        if (quantityNeeded == 0)
                        {
                            break;
                        }
                        var stillNeeded = characterSource.UseQuantity((int) quantityNeeded);
                        quantityReady += (quantityNeeded - stillNeeded);
                        quantityNeeded = stillNeeded;
                        //PluginLog.Log("Quantity needed for " + ItemId + ": " + quantityNeeded);
                        //PluginLog.Log("Still needed for " + ItemId + ": " + stillNeeded);
                    }
                }
                //PluginLog.Log("Quantity Ready for " + ItemId + ": " + quantityReady);
                craftItem.QuantityReady = quantityReady;
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                var quantityMissing = craftItem.QuantityMissing;
                //PluginLog.Log("quantity missing: " + quantityMissing);
                if (externalSources.ContainsKey(craftItem.ItemId))
                {
                    foreach (var externalSource in externalSources[craftItem.ItemId])
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

                craftItem.QuantityAvailable = quantityAvailable;

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)craftItem.QuantityAvailable - craftItem.QuantityReady);

                //This final figure represents the shortfall even when we include the character and external sources
                var quantityUnavailable = craftItem.QuantityUnavailable;
                if (craftItem.Recipe != null)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var totalAmountNeeded = craftItem.QuantityUnavailable;
                    craftItem.QuantityNeeded = totalAmountNeeded;
                    craftItem.ChildCrafts = CalculateChildCrafts(craftItem);
                    foreach (var childCraft in craftItem.ChildCrafts)
                    {
                        var amountNeeded = childCraft.QuantityNeeded;

                        childCraft.QuantityNeeded = Math.Max(0, amountNeeded);
                        UpdateCraftItem(childCraft, characterSources, externalSources,spareIngredients, cascadeCrafts);
                        var childCraftQuantityReady = childCraft.QuantityReady;
                        if (cascadeCrafts)
                        {
                            childCraftQuantityReady += childCraft.QuantityCanCraft;
                        }
                        var craftCapable = (uint)Math.Ceiling(childCraftQuantityReady / (double)craftItem.Recipe.GetRecipeItemAmount(childCraft.ItemId));
                        if (totalCraftCapable == null)
                        {
                            totalCraftCapable = craftCapable;
                        }
                        else
                        {
                            totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                        }
                    }

                    craftItem.QuantityCanCraft = Math.Min(totalCraftCapable * craftItem.Yield  ?? 0, totalAmountNeeded);
                }
                else if (Service.ExcelCache.HwdInspectionResults.ContainsKey(craftItem.ItemId))
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var inspectionMap = Service.ExcelCache.HwdInspectionResults[craftItem.ItemId];
                    var ingredientId = inspectionMap.Item1;
                    var amount = inspectionMap.Item2;
                    if (ingredientId == 0 || amount == 0)
                    {
                        return;
                    }

                    var amountNeeded = (uint)Math.Ceiling((double)quantityUnavailable / amount) * amount;

                    for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                    {
                        var childCraft = craftItem.ChildCrafts[index];
                        if (childCraft.ItemId == ingredientId)
                        {
                            childCraft.QuantityNeeded = Math.Max(0, amountNeeded);
                            UpdateCraftItem(childCraft, characterSources, externalSources,spareIngredients, cascadeCrafts);
                            var craftItemQuantityReady = childCraft.QuantityReady;
                            if (cascadeCrafts)
                            {
                                craftItemQuantityReady += childCraft.QuantityCanCraft;
                            }                            
                            var craftCapable =
                                (uint)Math.Ceiling(craftItemQuantityReady / (double)amount);
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
                    craftItem.QuantityCanCraft = Math.Min(totalCraftCapable * craftItem.Yield  ?? 0, craftItem.QuantityNeeded - craftItem.QuantityReady);
                }
                else
                {
                    var totalAmountNeeded = craftItem.QuantityUnavailable;
                    craftItem.QuantityNeeded = totalAmountNeeded;
                    craftItem.ChildCrafts = CalculateChildCrafts(craftItem);
                    for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                    {
                        var childCraft = craftItem.ChildCrafts[index];
                        UpdateCraftItem(childCraft, characterSources, externalSources,spareIngredients, cascadeCrafts);
                    }
                }
            }
            
        }
        
        public void MarkCrafted(uint itemId, InventoryItem.ItemFlags itemFlags, uint quantity, bool removeEmpty = true)
        {
            if (GetFlattenedMaterials().Any(c =>
                !c.IsOutputItem && c.ItemId == itemId && c.Flags == itemFlags && c.QuantityMissing != 0))
            {
                return;
            }
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags && c.QuantityRequired != 0))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == itemFlags && c.QuantityRequired != 0);
                craftItem.RemoveQuantity(quantity);
            }
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags && c.QuantityRequired <= 0) && removeEmpty)
            {
                RemoveCraftItem(itemId, itemFlags);
            }
            GenerateCraftChildren();
        }

        public void Update(Dictionary<uint, List<CraftItemSource>> characterSources,
            Dictionary<uint, List<CraftItemSource>> externalSources, bool cascadeCrafts = false)
        {
            if (!BeenGenerated)
            {
                GenerateCraftChildren();
            }
            var spareIngredients = new Dictionary<uint, double>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                //PluginLog.Log("Calculating items for " + craftItem.Item.Name);
                UpdateCraftItem(craftItem, characterSources, externalSources,spareIngredients, cascadeCrafts);
            }

            BeenUpdated = true;
        }

        public List<CraftItem> GetFlattenedMaterials()
        {
            var list = new List<CraftItem>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
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

        public List<CraftItem> GetFlattenedMergedMaterials()
        {
            var list = GetFlattenedMaterials();
            return list.GroupBy(c => new {c.ItemId, c.Flags, c.Phase, c.IsOutputItem}).Select(c => c.Sum()).ToList();
        }

        public Dictionary<uint, uint> GetRequiredMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityRequired;
            }

            return dictionary;
        }

        public Dictionary<uint, uint> GetAvailableMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityAvailable;
            }

            return dictionary;
        }

        public Dictionary<uint, uint> GetReadyMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityReady;
            }

            return dictionary;
        }

        public Dictionary<uint, uint> GetMissingMaterialsList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.RequiredQuantityUnavailable;
            }

            return dictionary;
        }

        public Dictionary<uint, uint> GetQuantityNeededList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityNeeded;
            }

            return dictionary;
        }

        public Dictionary<uint, uint> GetQuantityCanCraftList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityCanCraft;
            }

            return dictionary;
        }

        public Dictionary<uint, uint> GetQuantityToRetrieveList()
        {
            var dictionary = new Dictionary<uint, uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey(item.ItemId))
                {
                    dictionary.Add(item.ItemId, 0);
                }

                dictionary[item.ItemId] += item.QuantityToRetrieve;
            }

            return dictionary;
        }

    }
}