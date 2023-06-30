using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.MarketBoard;
using CriticalCommonLib.Models;
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
        private Dictionary<uint, IngredientPreference> _ingredientPreferences = new Dictionary<uint, IngredientPreference>();
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

        [JsonProperty]
        public RetainerRetrieveOrder RetainerRetrieveOrder { get; set; } = RetainerRetrieveOrder.RetrieveFirst;
        [JsonProperty]
        public CraftRetainerRetrieval CraftRetainerRetrieval { get; private set; } = CraftRetainerRetrieval.Yes;
        [JsonProperty]
        public CurrencyGroupSetting CurrencyGroupSetting { get; private set; } = CurrencyGroupSetting.Separate;
        [JsonProperty]
        public CrystalGroupSetting CrystalGroupSetting { get; private set; } = CrystalGroupSetting.Separate;
        [JsonProperty]
        public PrecraftGroupSetting PrecraftGroupSetting { get; private set; } = PrecraftGroupSetting.ByDepth;
        [JsonProperty]
        public EverythingElseGroupSetting EverythingElseGroupSetting { get; private set; } = EverythingElseGroupSetting.Together;
        [JsonProperty]
        public RetrieveGroupSetting RetrieveGroupSetting { get; private set; } = RetrieveGroupSetting.Together;

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
        
        public void SetRetrieveGroupSetting(RetrieveGroupSetting newValue)
        {
            RetrieveGroupSetting = newValue;
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

            var sortedItems = new Dictionary<(CraftGroupType, uint?), List<CraftItem>>();

            void AddToGroup(CraftItem craftItem, CraftGroupType type, uint? identifier = null)
            {
                (CraftGroupType, uint?) key = (type, identifier);
                sortedItems.TryAdd(key, new List<CraftItem>());
                sortedItems[key].Add(craftItem);
            }

            foreach (var item in groupedItems)
            {
                if (item.IsOutputItem)
                {
                    AddToGroup(item, CraftGroupType.Output);
                    continue;
                }

                //Early Retrieval
                if (RetrieveGroupSetting == RetrieveGroupSetting.Together && RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst && item.QuantityWillRetrieve != 0)
                {
                    AddToGroup(item, CraftGroupType.Retrieve);
                    continue;
                }
                
                //Late Retrieval
                if (RetrieveGroupSetting == RetrieveGroupSetting.Together && RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveLast && item.QuantityWillRetrieve != 0 && item.QuantityMissingInventory == item.QuantityWillRetrieve)
                {
                    AddToGroup(item, CraftGroupType.Retrieve);
                    continue;
                }
                
                //Precrafts
                if (item.Item.CanBeCrafted && item.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    if (PrecraftGroupSetting == PrecraftGroupSetting.Together)
                    {
                        AddToGroup(item, CraftGroupType.Precraft);
                        continue;
                    }
                    else if (PrecraftGroupSetting == PrecraftGroupSetting.ByDepth)
                    {
                        AddToGroup(item, CraftGroupType.PrecraftDepth, item.Depth);
                        continue;
                    }
                    else if (PrecraftGroupSetting == PrecraftGroupSetting.ByClass)
                    {
                        AddToGroup(item, CraftGroupType.PrecraftClass, item.Recipe?.CraftType.Row ?? 0);
                        continue;
                    }
                }
                
                if(CurrencyGroupSetting == CurrencyGroupSetting.Separate && item.Item.IsCurrency)
                {
                    AddToGroup(item, CraftGroupType.Currency);
                    continue;
                }
                if(CrystalGroupSetting == CrystalGroupSetting.Separate && item.Item.IsCrystal)
                {
                    AddToGroup(item, CraftGroupType.Crystals);
                    continue;
                }
                
                if (EverythingElseGroupSetting == EverythingElseGroupSetting.Together)
                {
                    AddToGroup(item, CraftGroupType.EverythingElse);
                }
                else if (EverythingElseGroupSetting == EverythingElseGroupSetting.ByClosestZone)
                {
                    if (item.IngredientPreference.Type == IngredientPreferenceType.Buy || item.IngredientPreference.Type == IngredientPreferenceType.Item)
                    {
                        foreach (var location in from vendor in item.Item.Vendors from npc in vendor.ENpcs from location in npc.Locations select location)
                        {
                            AddToGroup(item, CraftGroupType.EverythingElse, location.TerritoryTypeEx.Row);
                            break;
                        }
                    }
                    else if (item.IngredientPreference.Type == IngredientPreferenceType.Mobs)
                    {
                        foreach (var mobSpawns in item.Item.MobDrops.SelectMany(mobDrop => mobDrop.GroupedMobSpawns))
                        {
                            AddToGroup(item, CraftGroupType.EverythingElse, mobSpawns.Key.RowId);
                            break;
                        }
                    }
                    else if (item.IngredientPreference.Type == IngredientPreferenceType.Botany ||
                        item.IngredientPreference.Type == IngredientPreferenceType.Mining)
                    {
                        foreach (var gatheringSource in item.Item.GetGatheringSources())
                        {
                            if(gatheringSource.TerritoryType.RowId == 0 || gatheringSource.PlaceName.RowId == 0) continue;
                            AddToGroup(item, CraftGroupType.EverythingElse, gatheringSource.TerritoryType.RowId);
                            break;
                        }
                    }
                    else
                    {
                        AddToGroup(item, CraftGroupType.EverythingElse);
                    }
                }

            }

            uint OrderByCraftGroupType(CraftGrouping craftGroup)
            {
                switch (craftGroup.CraftGroupType)
                {
                    case CraftGroupType.Output:
                    {
                        return 0;
                    }
                    case CraftGroupType.Precraft:
                    {
                        return 10;
                    }
                    case CraftGroupType.EverythingElse:
                    {
                        return 20;
                    }
                    case CraftGroupType.Retrieve:
                    {
                        return RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst ? 30u : 15u;
                    }
                    case CraftGroupType.Crystals:
                    {
                        return 40;
                    }
                    case CraftGroupType.Currency:
                    {
                        return 50;
                    }
                }

                return 6;
            }

            foreach (var sortedGroup in sortedItems)
            {
                if (sortedGroup.Key.Item1 == CraftGroupType.Output)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Output, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Currency)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Currency, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Crystals)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Crystals, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Retrieve)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Retrieve, sortedGroup.Value));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.Precraft)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Precraft, sortedGroup.Value.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList()));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.PrecraftDepth)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Precraft, sortedGroup.Value.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList(), sortedGroup.Key.Item2));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.PrecraftClass)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.Precraft, sortedGroup.Value.OrderBy(c => c.Depth).ThenBy(c => c.Recipe?.CraftType.Row ?? 0).ToList(),null, sortedGroup.Key.Item2));
                }
                else if (sortedGroup.Key.Item1 == CraftGroupType.EverythingElse)
                {
                    craftGroupings.Add(new CraftGrouping(CraftGroupType.EverythingElse, sortedGroup.Value,null,null, sortedGroup.Key.Item2));
                }
            }

            craftGroupings = craftGroupings.OrderBy(OrderByCraftGroupType).ToList();

            return craftGroupings.Where(c => c.CraftItems.Count != 0).ToList();
        }

        private bool FilterRetrieveItems(bool groupRetrieve, CraftItem craftItem)
        {
            return !groupRetrieve || (craftItem.QuantityWillRetrieve != 0 && RetainerRetrieveOrder == RetainerRetrieveOrder.RetrieveFirst) || craftItem.QuantityWillRetrieve == 0;
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
            set => _ingredientPreferenceTypeOrder = value.Distinct().ToList();
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

        [JsonProperty]
        public Dictionary<uint, IngredientPreference> IngredientPreferences
        {
            get => _ingredientPreferences;
            private set => _ingredientPreferences = value;
        }

        [JsonProperty]
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
            set => _craftRetainerRetrievals = value;
        }

        [JsonProperty]
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
            set => _craftRecipePreferences = value;
        }

        [JsonProperty]
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
            set => _hqRequired = value;
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

        public CraftList AddCraftItem(string itemName, uint quantity = 1,
            InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
        {
            if (Service.ExcelCache.ItemsByName.ContainsKey(itemName))
            {
                var itemId = Service.ExcelCache.ItemsByName[itemName];
                AddCraftItem(itemId, quantity, flags, phase);
            }
            else
            {
                throw new Exception("Item with name " + itemName + " could not be found");
            }

            return this;
        }

        public CraftList AddCraftItem(uint itemId, uint quantity = 1, InventoryItem.ItemFlags flags = InventoryItem.ItemFlags.None, uint? phase = null)
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
            return this;
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

        public void SetCraftPhase(uint itemId, uint? newPhase)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.IsOutputItem))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.IsOutputItem);
                craftItem.SwitchPhase(newPhase);
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
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem, leftOvers).OrderByDescending(c => c.RecipeId).ToList();
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
            craftItem.MissingIngredients = new Dictionary<(uint, bool), uint>();
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
                            childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
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
                        ventureItem.ChildCrafts = CalculateChildCrafts(ventureItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
                        childCrafts.Add(ventureItem);
                        return childCrafts;
                    case IngredientPreferenceType.Item:
                        if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                        {
                            var childCraftItem = new CraftItem(ingredientPreference.LinkedItemId.Value, (GetHQRequired(ingredientPreference.LinkedItemId.Value) ?? false) ? InventoryItem.ItemFlags.HQ : InventoryItem.ItemFlags.None, craftItem.QuantityRequired * (uint)ingredientPreference.LinkedItemQuantity, craftItem.QuantityNeeded * (uint)ingredientPreference.LinkedItemQuantity);
                            childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
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

                    var tempAmountNeeded = actualAmountRequired;
                    if (spareIngredients.ContainsKey(materialItemId))
                    {
                        //Factor in the possible extra we get and then 
                        var amountAvailable = Math.Max(0,Math.Min(quantityNeeded, spareIngredients[materialItemId]));
                        //actualAmountRequired -= amountAvailable;
                        tempAmountNeeded -= amountAvailable;
                        spareIngredients[materialItemId] -= amountAvailable;
                    }
                    


                    var childCraftItem = new CraftItem(materialItemId, (GetHQRequired(materialItemId) ?? false) ? InventoryItem.ItemFlags.HQ : InventoryItem.ItemFlags.None, (uint)actualAmountRequired, (uint)tempAmountNeeded, false);
                    childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem).OrderByDescending(c => c.RecipeId).ToList();
                    childCraftItem.QuantityNeeded = (uint)actualAmountNeeded;
                    childCrafts.Add(childCraftItem);
                }
            }
            else
            {
                var companyCraftSequence = craftItem.Item.CompanyCraftSequenceEx;;
                if (companyCraftSequence != null)
                {
                    craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.Crafting);
                    var materialsRequired = companyCraftSequence.MaterialsRequired(craftItem.Phase);
                    for (var index = 0; index < materialsRequired.Count; index++)
                    {
                        var materialRequired = materialsRequired[index];
                        var childCraftItem = new CraftItem(materialRequired.ItemId,
                            (GetHQRequired(materialRequired.ItemId) ?? false)
                                ? InventoryItem.ItemFlags.HQ
                                : InventoryItem.ItemFlags.None, materialRequired.Quantity * craftItem.QuantityRequired,
                            materialRequired.Quantity * craftItem.QuantityNeeded, false);
                        childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients)
                            .OrderByDescending(c => c.RecipeId).ToList();
                        childCrafts.Add(childCraftItem);
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
                    var childCraftItem = new CraftItem((uint) requirements.Item1, (GetHQRequired(requirements.Item1) ?? false) ? InventoryItem.ItemFlags.HQ : InventoryItem.ItemFlags.None, quantityRequired, quantityNeeded, false);
                    childCraftItem.ChildCrafts = CalculateChildCrafts(childCraftItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
                    childCrafts.Add(childCraftItem);
                }
                else if (craftItem.Item.BuyFromVendorPrice != 0 && craftItem.Item.ObtainedGil && ingredientPreference != null && ingredientPreference.Type == IngredientPreferenceType.Buy)
                {
                    craftItem.IngredientPreference = new IngredientPreference(craftItem.ItemId, IngredientPreferenceType.Buy);
                    var childCraft = new CraftItem(1, InventoryItem.ItemFlags.None, (uint) craftItem.Item.BuyFromVendorPrice * craftItem.QuantityRequired);
                    childCraft.ChildCrafts = CalculateChildCrafts(childCraft, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
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
                craftItem.QuantityNeededPreUpdate = craftItem.QuantityNeededPreUpdate;
                
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
                    var quantityMissing = craftItem.QuantityMissingInventory;
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

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)(Math.Min(craftItem.QuantityAvailable,craftItem.QuantityNeeded) - craftItem.QuantityReady));

                craftItem.QuantityNeeded = Math.Max(0, craftItem.QuantityNeeded - quantityAvailable);
                
                craftItem.ChildCrafts = CalculateChildCrafts(craftItem).OrderByDescending(c => c.RecipeId).ToList();
                for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                {
                    var childCraftItem = craftItem.ChildCrafts[index];
                    UpdateCraftItem(childCraftItem, characterSources, externalSources,spareIngredients, cascadeCrafts);
                }

                if (craftItem.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    IEnumerable<(uint, int)> ingredients;
                    if (craftItem.Recipe != null)
                    {
                        ingredients = craftItem.Recipe.Ingredients.Select(c => (c.Item.Row, c.Count));
                    }
                    else if(craftItem.Item.CompanyCraftSequenceEx != null)
                    {
                        ingredients = craftItem.Item.CompanyCraftSequenceEx.MaterialsRequired(craftItem.Phase).Select(c => ((uint)c.ItemId, (int)c.Quantity));
                    }
                    else
                    {
                        ingredients = new List<(uint Row, int Count)>();
                    }
                    foreach (var ingredient in ingredients)
                    {
                        if (ingredient.Item1 <= 0 || ingredient.Item1 <= 0)
                        {
                            continue;
                        }
                        var ingredientId = ingredient.Item1;
                        var amountNeeded = (double)ingredient.Item2;

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
                                if (craftCapable < craftItem.QuantityNeeded)
                                {
                                    var key = (childCraftItem.ItemId,childCraftItem.Flags == InventoryItem.ItemFlags.HQ);
                                    craftItem.MissingIngredients.TryAdd(key, 0);
                                    craftItem.MissingIngredients[key] += (uint)amountNeeded - craftCapable;
                                }
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

                    craftItem.QuantityCanCraft = Math.Min(craftItem.QuantityNeeded * craftItem.Yield, (totalCraftCapable ?? 0) * craftItem.Yield);
                }
                else
                {
                    var ingredientPreference = craftItem.IngredientPreference;
                    if (ingredientPreference.Type == IngredientPreferenceType.Item)
                    {
                        if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                        {
                            uint? totalAmountAvailable = null;
                            var amountNeeded = (double)ingredientPreference.LinkedItemQuantity * craftItem.QuantityNeeded;
                            for (var index = 0; index < craftItem.ChildCrafts.Count; index++)
                            {
                                var childItem = craftItem.ChildCrafts[index];
                                var totalCapable = childItem.QuantityReady;
                                //PluginLog.Log("amount craftable for ingredient " + craftItem.ItemId + " for output item is " + craftCapable);
                                if (totalCapable < amountNeeded)
                                {
                                    var key = (childItem.ItemId,childItem.Flags == InventoryItem.ItemFlags.HQ);
                                    craftItem.MissingIngredients.TryAdd(key, 0);
                                    craftItem.MissingIngredients[key] += (uint)amountNeeded - totalCapable;
                                }
                                if (totalAmountAvailable == null)
                                {
                                    totalAmountAvailable = totalCapable;
                                }
                                else
                                {
                                    totalAmountAvailable = Math.Min(totalCapable, totalAmountAvailable.Value);
                                }
                            }
                            craftItem.QuantityCanCraft = (uint)Math.Floor((double)(totalAmountAvailable ?? 0) / ingredientPreference.LinkedItemQuantity.Value);
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
                    var quantityMissing = quantityNeeded;
                    //PluginLog.Log("quantity missing: " + quantityMissing);
                    if (quantityMissing != 0 && externalSources.ContainsKey(craftItem.ItemId))
                    {
                        foreach (var externalSource in externalSources[craftItem.ItemId])
                        {
                            if (quantityMissing == 0)
                            {
                                break;
                            }
                            if(craftRetainerRetrieval == CraftRetainerRetrieval.HQOnly && !externalSource.IsHq) continue;
                            var stillNeeded = externalSource.UseQuantity((int)quantityMissing);
                            quantityAvailable += (quantityMissing - stillNeeded);
                            quantityMissing = stillNeeded;
                        }
                    }
                }

                craftItem.QuantityAvailable = quantityAvailable;

                craftItem.QuantityWillRetrieve = (uint)Math.Max(0,(int)(Math.Min(craftItem.QuantityAvailable,craftItem.QuantityNeeded - craftItem.QuantityReady)));
                var ingredientPreference = craftItem.IngredientPreference;
                
                //This final figure represents the shortfall even when we include the character and external sources
                var quantityUnavailable = (uint)Math.Max(0,(int)craftItem.QuantityNeeded - (int)craftItem.QuantityReady - (int)craftItem.QuantityAvailable);
                if (spareIngredients != null && spareIngredients.ContainsKey(craftItem.ItemId))
                {
                    var amountAvailable = (uint)Math.Max(0,Math.Min(quantityUnavailable, spareIngredients[craftItem.ItemId]));
                    quantityUnavailable -= amountAvailable;
                    spareIngredients[craftItem.ItemId] -= amountAvailable;
                }
                if (craftItem.Recipe != null && craftItem.IngredientPreference.Type == IngredientPreferenceType.Crafting)
                {
                    //Determine the total amount we can currently make based on the amount ready within our main inventory 
                    uint? totalCraftCapable = null;
                    var totalAmountNeeded = quantityUnavailable;
                    craftItem.QuantityNeeded = totalAmountNeeded;
                    craftItem.ChildCrafts = CalculateChildCrafts(craftItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
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
                        if (craftCapable < craftItem.QuantityNeeded)
                        {
                            var key = (childCraft.ItemId,childCraft.Flags == InventoryItem.ItemFlags.HQ);
                            craftItem.MissingIngredients.TryAdd(key, 0);
                            craftItem.MissingIngredients[key] += (uint)amountNeeded - craftCapable;
                        }
                        if (totalCraftCapable == null)
                        {
                            totalCraftCapable = craftCapable;
                        }
                        else
                        {
                            totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                        }
                    }

                    craftItem.QuantityCanCraft = Math.Min(totalCraftCapable * craftItem.Yield  ?? 0, totalAmountNeeded * craftItem.Yield);
                }
                else if (ingredientPreference.Type == IngredientPreferenceType.Item)
                {
                    if (ingredientPreference.LinkedItemId != null && ingredientPreference.LinkedItemQuantity != null)
                    {
                        uint? totalCraftCapable = null;
                        var totalAmountNeeded = quantityUnavailable;
                        craftItem.QuantityNeeded = totalAmountNeeded;
                        craftItem.ChildCrafts = CalculateChildCrafts(craftItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
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
                            if (craftCapable < amountNeeded)
                            {
                                var key = (childCraft.ItemId,childCraft.Flags == InventoryItem.ItemFlags.HQ);
                                craftItem.MissingIngredients.TryAdd(key, 0);
                                craftItem.MissingIngredients[key] += (uint)amountNeeded - craftCapable;
                            }
                            if (totalCraftCapable == null)
                            {
                                totalCraftCapable = craftCapable;
                            }
                            else
                            {
                                totalCraftCapable = Math.Min(craftCapable, totalCraftCapable.Value);
                            }
                        }

                        craftItem.QuantityCanCraft = Math.Min((uint)Math.Floor((double)(totalCraftCapable ?? 0) / ingredientPreference.LinkedItemQuantity.Value), totalAmountNeeded);
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
                    var totalAmountNeeded = quantityUnavailable;
                    craftItem.QuantityNeeded = totalAmountNeeded;
                    craftItem.ChildCrafts = CalculateChildCrafts(craftItem, spareIngredients).OrderByDescending(c => c.RecipeId).ToList();
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
                !c.IsOutputItem && c.ItemId == itemId && c.Flags == itemFlags && c.QuantityMissingOverall != 0))
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

        public void Update(CraftItemSourceStore sourceStore, bool cascadeCrafts = false)
        {
            var characterSources = sourceStore.CharacterMaterials;
            var externalSources = sourceStore.ExternalSources;
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
        
        public Dictionary<string, uint> GetRequiredMaterialsListNamed()
        {
            return GetRequiredMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
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

        public Dictionary<string, uint> GetAvailableMaterialsListNamed()
        {
            return GetAvailableMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
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
        
        public Dictionary<string, uint> GetReadyMaterialsListNamed()
        {
            return GetReadyMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
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

                dictionary[item.ItemId] += item.QuantityMissingOverall;
            }

            return dictionary;
        }
        
        public Dictionary<string, uint> GetMissingMaterialsListNamed()
        {
            return GetMissingMaterialsList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
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
        
        public Dictionary<string, uint> GetQuantityNeededListNamed()
        {
            return GetQuantityNeededList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
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
        
        public Dictionary<string, uint> GetQuantityCanCraftListNamed()
        {
            return GetQuantityCanCraftList().ToDictionary(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Key)!.NameString,
                c => c.Value);
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

        public CraftItem? GetItemById(uint itemId, bool isHq)
        {
            if (HQRequireds.ContainsKey(itemId))
            {
                if (HQRequireds[itemId] != isHq)
                {
                    return null;
                }
            }

            var craftItems = GetFlattenedMergedMaterials().Where(c => c.ItemId == itemId).ToList();
            return craftItems.Count != 0 ? craftItems.First() : null;
        }

    }
}