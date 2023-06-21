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

        private List<(IngredientPreferenceType,uint?)>? _ingredientPreferenceTypeOrder;
        private Dictionary<uint, IngredientPreference>? _ingredientPreferences;
        private Dictionary<uint, bool>? _hqRequired;
        private Dictionary<uint, CraftRetainerRetrieval>? _craftRetainerRetrievals;
        private Dictionary<uint, uint>? _craftRecipePreferences;


        public bool HideComplete
        {
            get => _hideComplete;
            set
            {
                _hideComplete = value;
                ClearGroupCache();
            }
        }

        public RetainerRetrieveOrder RetainerRetrieveOrder { get; set; } = RetainerRetrieveOrder.RetrieveFirst;
        public CraftRetainerRetrieval CraftRetainerRetrieval { get; private set; } = CraftRetainerRetrieval.Yes;
        public CurrencyGroupSetting CurrencyGroupSetting { get; private set; } = CurrencyGroupSetting.Separate;
        public CrystalGroupSetting CrystalGroupSetting { get; private set; } = CrystalGroupSetting.Separate;
        public PrecraftGroupSetting PrecraftGroupSetting { get; private set; } = PrecraftGroupSetting.ByDepth;
        public EverythingElseGroupSetting EverythingElseGroupSetting { get; private set; } = EverythingElseGroupSetting.Together;

        public void SetCrystalGroupSetting(CrystalGroupSetting newValue)
        {
            CrystalGroupSetting = newValue;
            ClearGroupCache();
        }

        public void SetCurrencyGroupSetting(CurrencyGroupSetting newValue)
        {
            CurrencyGroupSetting = newValue;
            ClearGroupCache();
        }
        
        public void SetPrecraftGroupSetting(PrecraftGroupSetting newValue)
        {
            PrecraftGroupSetting = newValue;
            ClearGroupCache();
        }
        
        public void SetEverythingElseGroupSetting(EverythingElseGroupSetting newValue)
        {
            EverythingElseGroupSetting = newValue;
            ClearGroupCache();
        }

        public void ClearGroupCache()
        {
            _craftGroupings = null;
        }


        private List<CraftGrouping>? _craftGroupings;
        private bool _hideComplete = false;

        public List<CraftGrouping> GetOutputList()
        {
            if (_craftGroupings == null)
            {
                _craftGroupings = GenerateGroupedCraftItems();
            }

            return _craftGroupings;
        }

        private List<CraftGrouping> GenerateGroupedCraftItems()
        {
            var craftGroupings = new List<CraftGrouping>();
            var groupedItems = GetFlattenedMergedMaterials();
            
            if(HideComplete)
            {
                groupedItems = groupedItems.Where(c => !HideComplete || !c.IsCompleted).ToList();
            }
            
            var outputCrafts = new CraftGrouping(CraftGroupType.Output, groupedItems.Where(c => c.IsOutputItem).ToList());
            craftGroupings.Add(outputCrafts);
            
            


            if (PrecraftGroupSetting == PrecraftGroupSetting.Together)
            {
                var craftItems = groupedItems.Where(c =>  !c.IsOutputItem && c.Item.CanBeCrafted && c.IngredientPreference.Type == IngredientPreferenceType.Crafting);
                var preCrafts = craftItems.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList();
                var preCraftGrouping = new CraftGrouping(CraftGroupType.Precraft, preCrafts);
                craftGroupings.Add(preCraftGrouping);
            }
            else if (PrecraftGroupSetting == PrecraftGroupSetting.ByDepth)
            {
                var craftItems = groupedItems.Where(c =>  !c.IsOutputItem && c.Item.CanBeCrafted && c.IngredientPreference.Type == IngredientPreferenceType.Crafting);
                var preCrafts = craftItems.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).GroupBy(c => c.Depth);
                foreach (var precraft in preCrafts)
                {
                    var preCraftGrouping = new CraftGrouping(CraftGroupType.Precraft, precraft.ToList(), precraft.Key);
                    craftGroupings.Add(preCraftGrouping);
                }
            }
            else if( PrecraftGroupSetting == PrecraftGroupSetting.ByClass)
            {
                var craftItems = groupedItems.Where(c =>  !c.IsOutputItem && c.Item.CanBeCrafted && c.IngredientPreference.Type == IngredientPreferenceType.Crafting);
                var preCrafts = craftItems.OrderBy(c => c.Depth).GroupBy(c => c.Recipe?.CraftType.Row ?? 0);
                foreach (var precraft in preCrafts)
                {
                    var preCraftGrouping = new CraftGrouping(CraftGroupType.Precraft, precraft.ToList(),null, precraft.Key);
                    craftGroupings.Add(preCraftGrouping);
                }
            }
            
            var everythingElse = groupedItems.Where(c => c.QuantityRequired != 0 && c.IngredientPreference.Type != IngredientPreferenceType.Crafting && !c.IsOutputItem);
            if (CrystalGroupSetting == CrystalGroupSetting.Separate && CurrencyGroupSetting == CurrencyGroupSetting.Separate)
            {
                everythingElse = everythingElse.Where(c => !c.Item.IsCrystal && !c.Item.IsCurrency);
            }
            if (CrystalGroupSetting == CrystalGroupSetting.Separate)
            {
                everythingElse = everythingElse.Where(c => !c.Item.IsCrystal);
            }
            else if (CurrencyGroupSetting == CurrencyGroupSetting.Separate)
            {
                everythingElse = everythingElse.Where(c => !c.Item.IsCrystal);
            }

            if (EverythingElseGroupSetting == EverythingElseGroupSetting.Together)
            {
                everythingElse = everythingElse.OrderBy(c => c.IngredientPreference.ToString()).ThenBy(c => c.Depth);
                var everythingElseGrouping = new CraftGrouping(CraftGroupType.EverythingElse, everythingElse.ToList());
                craftGroupings.Add(everythingElseGrouping);
            }
            else
            {
                //TODO: Optimize this
                
                //Map between territory type ID and item IDs
                var potentialLocations = new Dictionary<uint, HashSet<uint>>();
                //Map between item and craft item
                var items = new Dictionary<uint, CraftItem>();
                foreach (var item in everythingElse)
                {
                    if (item.IngredientPreference.Type == IngredientPreferenceType.Botany ||
                        item.IngredientPreference.Type == IngredientPreferenceType.Buy ||
                        item.IngredientPreference.Type == IngredientPreferenceType.Mining ||
                        item.IngredientPreference.Type == IngredientPreferenceType.Mobs ||
                        item.IngredientPreference.Type == IngredientPreferenceType.Item)
                    {
                        items[item.ItemId] = item;
                        foreach (var vendor in item.Item.Vendors)
                        {
                            foreach (var npc in vendor.ENpcs)
                            {
                                foreach (var location in npc.Locations)
                                {
                                    potentialLocations.TryAdd(location.TerritoryTypeEx.Row, new HashSet<uint>());
                                    potentialLocations[location.TerritoryTypeEx.Row].Add(item.ItemId);
                                }
                            }
                        }

                        foreach (var mobDrop in item.Item.MobDrops)
                        {
                            foreach (var mobSpawns in mobDrop.GroupedMobSpawns)
                            {
                                potentialLocations.TryAdd(mobSpawns.Key.RowId, new HashSet<uint>());
                                potentialLocations[mobSpawns.Key.RowId].Add(item.ItemId);
                            }
                        }

                        foreach (var gatheringSource in item.Item.GetGatheringSources())
                        {
                            potentialLocations.TryAdd(gatheringSource.TerritoryType.RowId, new HashSet<uint>());
                            potentialLocations[gatheringSource.TerritoryType.RowId].Add(item.ItemId);
                        }

                        foreach (var gatheringSource in item.Item.GetGatheringSources())
                        {
                            potentialLocations.TryAdd(gatheringSource.TerritoryType.RowId, new HashSet<uint>());
                            potentialLocations[gatheringSource.TerritoryType.RowId].Add(item.ItemId);
                        }
                    }
                }
                
                var sortedLocations = potentialLocations.OrderBy(c => c.Value.Count);
                var seenItems = new HashSet<uint>();
                foreach (var sortedLocation in sortedLocations)
                {
                    var locationItems = new List<CraftItem>();
                    foreach (var itemId in sortedLocation.Value)
                    {
                        if (!seenItems.Contains(itemId) && items.ContainsKey(itemId))
                        {
                            locationItems.Add(items[itemId]);
                            seenItems.Add(itemId);
                        }
                    }
                    var locationGrouping = new CraftGrouping(CraftGroupType.EverythingElse, locationItems.ToList(), null, null, sortedLocation.Key);
                    craftGroupings.Add(locationGrouping);
                }
                var locationLessItems = new List<CraftItem>();
                foreach (var item in everythingElse)
                {
                    if (!seenItems.Contains(item.ItemId))
                    {
                        locationLessItems.Add(item);
                    }
                }

                if (locationLessItems.Count != 0)
                {
                    var locationGrouping = new CraftGrouping(CraftGroupType.EverythingElse, locationLessItems.ToList());
                    craftGroupings.Add(locationGrouping);
                }
            }


            
            if (CrystalGroupSetting == CrystalGroupSetting.Separate)
            {
                var crystalItems = groupedItems.Where(c => c.QuantityRequired != 0 && !c.Item.CanBeCrafted && c.Item.IsCrystal && !c.IsOutputItem);
                crystalItems = crystalItems.OrderBy(c => c.IngredientPreference.ToString()).ThenBy(c => c.Depth);
                var crystalGrouping = new CraftGrouping(CraftGroupType.Crystals, crystalItems.ToList());
                craftGroupings.Add(crystalGrouping);
            }
            
            if (CurrencyGroupSetting == CurrencyGroupSetting.Separate)
            {
                var currencyItems = groupedItems.Where(c => c.QuantityRequired != 0 && !c.Item.CanBeCrafted && c.Item.IsCurrency && !c.IsOutputItem);
                currencyItems = currencyItems.OrderBy(c => c.IngredientPreference.ToString()).ThenBy(c => c.Depth);
                var currencyGrouping = new CraftGrouping(CraftGroupType.Currency, currencyItems.ToList());
                craftGroupings.Add(currencyGrouping);
            }
            

            return craftGroupings.Where(c => c.CraftItems.Count != 0).ToList();
        }

        public void ResetIngredientPreferences()
        {
            _ingredientPreferenceTypeOrder = new List<(IngredientPreferenceType,uint?)>()
            {   
                (IngredientPreferenceType.Crafting,null),
                (IngredientPreferenceType.Mining,null),
                (IngredientPreferenceType.Botany,null),
                (IngredientPreferenceType.Fishing,null),
                (IngredientPreferenceType.Venture,null),
                (IngredientPreferenceType.Buy,null),
                (IngredientPreferenceType.ResourceInspection,null),
                (IngredientPreferenceType.Mobs,null),
                (IngredientPreferenceType.Desynthesis,null),
                (IngredientPreferenceType.Reduction,null),
                (IngredientPreferenceType.Gardening,null),
                (IngredientPreferenceType.Item,20),
                (IngredientPreferenceType.Item,21),
                (IngredientPreferenceType.Item,22),
                (IngredientPreferenceType.Marketboard,null),
                (IngredientPreferenceType.Item,null),
            };
        }

        public List<(IngredientPreferenceType,uint?)> IngredientPreferenceTypeOrder
        {
            get
            {
                if (_ingredientPreferenceTypeOrder == null)
                {
                    _ingredientPreferenceTypeOrder = new();
                    ResetIngredientPreferences();
                }

                return _ingredientPreferenceTypeOrder;
            }
            set => _ingredientPreferenceTypeOrder = value;
        }

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

        public Dictionary<uint, IngredientPreference> IngredientPreferences
        {
            get
            {
                if (_ingredientPreferences == null)
                {
                    _ingredientPreferences = new Dictionary<uint, IngredientPreference>();
                }
                return _ingredientPreferences;
            }
        }

        public Dictionary<uint, CraftRetainerRetrieval> CraftRetainerRetrievals
        {
            get
            {
                if (_craftRetainerRetrievals == null)
                {
                    _craftRetainerRetrievals = new Dictionary<uint, CraftRetainerRetrieval>();
                }
                return _craftRetainerRetrievals;
            }
        }

        public Dictionary<uint, uint> CraftRecipePreferences
        {
            get
            {
                if (_craftRecipePreferences == null)
                {
                    _craftRecipePreferences = new Dictionary<uint, uint>();
                }
                return _craftRecipePreferences;
            }
        }

        public Dictionary<uint, bool> HQRequireds
        {
            get
            {
                if (_hqRequired == null)
                {
                    _hqRequired = new Dictionary<uint, bool>();
                }
                return _hqRequired;
            }
        }
        
        public bool? GetHQRequired(uint itemId)
        {
            if (!HQRequireds.ContainsKey(itemId))
            {
                return null;
            }

            return HQRequireds[itemId];
        }

        public void UpdateHQRequired(uint itemId, bool? newValue)
        {
            if (newValue == null)
            {
                HQRequireds.Remove(itemId);
            }
            else
            {
                HQRequireds[itemId] = newValue.Value;
            }
        }

        public CraftRetainerRetrieval? GetCraftRetainerRetrieval(uint itemId)
        {
            if (!CraftRetainerRetrievals.ContainsKey(itemId))
            {
                return null;
            }

            return CraftRetainerRetrievals[itemId];
        }
        
        public void UpdateCraftRetainerRetrieval(uint itemId, CraftRetainerRetrieval? newValue)
        {
            if (newValue == null)
            {
                CraftRetainerRetrievals.Remove(itemId);
            }
            else
            {
                CraftRetainerRetrievals[itemId] = newValue.Value;
            }
        }
        
        public void UpdateCraftRecipePreference(uint itemId, uint? newRecipeId)
        {
            if (newRecipeId == null)
            {
                CraftRecipePreferences.Remove(itemId);
            }
            else
            {
                CraftRecipePreferences[itemId] = newRecipeId.Value;
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
            if (item != null)
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
        
        public IngredientPreference? GetIngredientPreference(uint itemId)
        {
            return IngredientPreferences.ContainsKey(itemId) ? IngredientPreferences[itemId] : null;
        }

        public void UpdateIngredientPreference(uint itemId, IngredientPreference? ingredientPreference)
        {
            if (ingredientPreference == null)
            {
                IngredientPreferences.Remove(itemId);
            }
            else
            {
                IngredientPreferences[itemId] = ingredientPreference;
            }
            GenerateCraftChildren();
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
            
            IngredientPreference? ingredientPreference = null;
            if (IngredientPreferences.ContainsKey(craftItem.ItemId))
            {
                if (IngredientPreferences[craftItem.ItemId].Type == IngredientPreferenceType.None)
                {
                    UpdateIngredientPreference(craftItem.ItemId, null);
                }
                else
                {
                    ingredientPreference = IngredientPreferences[craftItem.ItemId];
                }
            }
            
            if(ingredientPreference == null)
            {
                foreach (var defaultPreference in IngredientPreferenceTypeOrder)
                {
                    if (craftItem.Item.GetIngredientPreference(defaultPreference.Item1, defaultPreference.Item2,out ingredientPreference))
                    {
                        break;
                    }
                }
            }
            
            if (ingredientPreference != null)
            {
                craftItem.IngredientPreference = new IngredientPreference(ingredientPreference);
                switch (ingredientPreference.Type)
                {
                    case IngredientPreferenceType.Botany:
                    case IngredientPreferenceType.Fishing:
                    case IngredientPreferenceType.Mining:
                        return childCrafts;
                    case IngredientPreferenceType.Buy:
                        if (craftItem.Item.BuyFromVendorPrice != 0)
                        {
                            var childCraftItem = new CraftItem(1, InventoryItem.ItemFlags.None, (uint)craftItem.Item.BuyFromVendorPrice * craftItem.QuantityRequired);
                            childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients);
                            childCrafts.Add(childCraftItem);
                        }

                        return childCrafts;
                    case IngredientPreferenceType.Marketboard:
                        //TODO:Might need to have some sort of system that allows prices to be brought in
                        return childCrafts;
                    case IngredientPreferenceType.Venture:
                        var quantity = 1u;
                        if (craftItem.Item.RetainerTasks != null && craftItem.Item.RetainerTasks.Count != 0)
                        {
                            var retainerTask = craftItem.Item.RetainerTasks.First();
                            if (retainerTask.Quantity != 0)
                            {
                                quantity = retainerTask.Quantity;
                            }
                        }
                        //TODO: Work out the exact amount of ventures required.
                        var ventureItem = new CraftItem(21072, InventoryItem.ItemFlags.None, (uint)Math.Ceiling(craftItem.QuantityRequired / (double)quantity));
                        ventureItem.ChildCrafts = CalculateChildCrafts(ventureItem, spareIngredients);
                        childCrafts.Add(ventureItem);
                        return childCrafts;
                    case IngredientPreferenceType.Item:
                        if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                        {
                            var childCraftItem = new CraftItem(ingredientPreference.LinkedItemId.Value, InventoryItem.ItemFlags.None, craftItem.QuantityRequired * (uint)ingredientPreference.LinkedItemQuantity, craftItem.QuantityNeeded * (uint)ingredientPreference.LinkedItemQuantity);
                            childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients);
                            childCrafts.Add(childCraftItem);
                        }

                        return childCrafts;
                }
            }
            
            if (craftItem.Recipe == null)
            {
                if (CraftRecipePreferences.ContainsKey(craftItem.ItemId))
                {
                    craftItem.RecipeId = CraftRecipePreferences[craftItem.ItemId];
                }
                else if (Service.ExcelCache.ItemRecipes.ContainsKey(craftItem.ItemId))
                {
                    var recipes = Service.ExcelCache.ItemRecipes[craftItem.ItemId];
                    if (recipes.Count != 0)
                    {
                        craftItem.RecipeId = recipes.First();
                    }
                }
            }
            
            if (craftItem.QuantityRequired == 0)
            {
                return childCrafts;
            }

            if (craftItem.Recipe != null)
            {
                craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.Crafting);
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
                    craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.Crafting);
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
                else if (Service.ExcelCache.HwdInspectionResults.ContainsKey(craftItem.ItemId) && ingredientPreference != null && ingredientPreference.Type == IngredientPreferenceType.ResourceInspection)
                {
                    craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.ResourceInspection);
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
                else if (craftItem.Item.BuyFromVendorPrice != 0 && craftItem.Item.ObtainedGil && ingredientPreference != null && ingredientPreference.Type == IngredientPreferenceType.Buy)
                {
                    craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.Buy);
                    var childCraft = new CraftItem(1, InventoryItem.ItemFlags.None, (uint) craftItem.Item.BuyFromVendorPrice * craftItem.QuantityRequired);
                    childCraft.ChildCrafts = CalculateChildCrafts(childCraft, spareIngredients);
                    childCrafts.Add(childCraft);
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
                
                //The default is to not source anything from retainers, but if the user does set it, we can pull from retainers 
                var craftRetainerRetrieval = CraftRetainerRetrieval.No;
                if (CraftRetainerRetrievals.ContainsKey(craftItem.ItemId))
                {
                    craftRetainerRetrieval = CraftRetainerRetrievals[craftItem.ItemId];
                }
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                if (craftRetainerRetrieval is CraftRetainerRetrieval.Yes or CraftRetainerRetrieval.HQOnly)
                {
                    var quantityMissing = craftItem.QuantityMissing;
                    //PluginLog.Log("quantity missing: " + quantityMissing);
                    if (quantityMissing != 0 && externalSources.ContainsKey(craftItem.ItemId))
                    {
                        foreach (var externalSource in externalSources[craftItem.ItemId])
                        {
                            if(craftRetainerRetrieval == CraftRetainerRetrieval.HQOnly && !externalSource.IsHq) continue;
                            var stillNeeded = externalSource.UseQuantity((int)quantityMissing);
                            //PluginLog.Log("missing: " + quantityMissing);
                            //PluginLog.Log("Still needed: " + stillNeeded);
                            quantityAvailable += (quantityMissing - stillNeeded);
                        }
                    }
                }
                
                craftItem.QuantityAvailable = quantityAvailable;

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)craftItem.QuantityAvailable - craftItem.QuantityReady);

                craftItem.QuantityNeeded = Math.Max(0, craftItem.QuantityNeeded - quantityAvailable);
                
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem,spareIngredients);
                for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                {
                    var childCraftItem = craftItem.ChildCrafts[index];
                    UpdateCraftItem(childCraftItem, characterSources, externalSources,spareIngredients, cascadeCrafts);
                }

                if (craftItem.Recipe != null && craftItem.IngredientPreference.Type == IngredientPreferenceType.Crafting)
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
                else
                {
                    var ingredientPreference = craftItem.IngredientPreference;
                    if (ingredientPreference.Type == IngredientPreferenceType.Item)
                    {
                        if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                        {
                            uint? totalAmountAvailable = null;
                            var amountNeeded = (double)ingredientPreference.LinkedItemQuantity;
                            for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                            {
                                var childItem = craftItem.ChildCrafts[index];
                                var totalCapable = (uint)Math.Floor(childItem.QuantityReady / amountNeeded);
                                //PluginLog.Log("amount craftable for ingredient " + craftItem.ItemId + " for output item is " + craftCapable);
                                if (totalAmountAvailable == null)
                                {
                                    totalAmountAvailable = totalCapable;
                                }
                                else
                                {
                                    totalAmountAvailable = Math.Min(totalCapable, totalAmountAvailable.Value);
                                }
                            }
                            craftItem.QuantityCanCraft = totalAmountAvailable * craftItem.Yield ?? 0;
                        }
                    }
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
                
                var craftRetainerRetrieval = CraftRetainerRetrieval;
                if (CraftRetainerRetrievals.ContainsKey(craftItem.ItemId))
                {
                    craftRetainerRetrieval = CraftRetainerRetrievals[craftItem.ItemId];
                }
                
                //Second generate the amount that is available elsewhere(retainers and such)
                var quantityAvailable = 0u;
                if (craftRetainerRetrieval is CraftRetainerRetrieval.Yes or CraftRetainerRetrieval.HQOnly)
                {
                    var quantityMissing = craftItem.QuantityMissing;
                    //PluginLog.Log("quantity missing: " + quantityMissing);
                    if (quantityMissing != 0 && externalSources.ContainsKey(craftItem.ItemId))
                    {
                        foreach (var externalSource in externalSources[craftItem.ItemId])
                        {
                            if(craftRetainerRetrieval == CraftRetainerRetrieval.HQOnly && !externalSource.IsHq) continue;
                            var stillNeeded = externalSource.UseQuantity((int)quantityMissing);
                            //PluginLog.Log("missing: " + quantityMissing);
                            //PluginLog.Log("Still needed: " + stillNeeded);
                            quantityAvailable += (quantityMissing - stillNeeded);
                        }
                    }
                }

                craftItem.QuantityAvailable = quantityAvailable;

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)craftItem.QuantityAvailable - craftItem.QuantityReady);
                var ingredientPreference = craftItem.IngredientPreference;
                
                //This final figure represents the shortfall even when we include the character and external sources
                var quantityUnavailable = craftItem.QuantityUnavailable- craftItem.QuantityReady;
                if (craftItem.Recipe != null && craftItem.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var totalAmountNeeded = craftItem.QuantityUnavailable - craftItem.QuantityReady;
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
                else if (ingredientPreference.Type == IngredientPreferenceType.Item)
                {
                    if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                    {
                        uint? totalCraftCapable = null;
                        var totalAmountNeeded = craftItem.QuantityUnavailable - craftItem.QuantityReady;
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
                            var craftCapable = (uint)Math.Ceiling((double)childCraftQuantityReady);
                            if (totalCraftCapable == null)
                            {
                                totalCraftCapable = craftCapable;
                            }
                            else
                            {
                                totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                            }
                        }

                        craftItem.QuantityCanCraft = Math.Min(totalCraftCapable ?? 0, totalAmountNeeded);
                    }
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
                    var totalAmountNeeded = craftItem.QuantityUnavailable- craftItem.QuantityReady;
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

        public List<CraftItem> GetFlattenedMaterials(uint depth = 0)
        {
            var list = new List<CraftItem>();
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                craftItem.Depth = depth;
                list.Add(craftItem);
                var items = craftItem.GetFlattenedMaterials(depth + 1);
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
            return list.GroupBy(c => new {c.ItemId, c.Flags, c.Phase, c.IsOutputItem}).Select(c => c.Sum()).OrderBy(c => c.Depth).ToList();
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

        public Dictionary<(uint, bool), uint> GetQuantityToRetrieveList()
        {
            var dictionary = new Dictionary<(uint, bool), uint>();
            var flattenedMaterials = GetFlattenedMaterials();
            for (var index = 0; index < flattenedMaterials.Count; index++)
            {
                var item = flattenedMaterials[index];
                if (!dictionary.ContainsKey((item.ItemId, item.IsOutputItem)))
                {
                    dictionary.Add((item.ItemId, item.IsOutputItem), 0);
                }

                dictionary[(item.ItemId, item.IsOutputItem)] += item.QuantityWillRetrieve;
            }

            return dictionary;
        }

    }
}