using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
using Newtonsoft.Json;
using BitfieldUptime = AllaganLib.Shared.Time.BitfieldUptime;

namespace CriticalCommonLib.Crafting
{
    public class CraftItem : ISummable<CraftItem>, IItem
    {
        [field: JsonIgnore]
        [JsonIgnore]
        public ItemSheet ItemSheet { get; }

        [field: JsonIgnore]
        [JsonIgnore]
        public RecipeSheet RecipeSheet { get; }
        public uint ItemId { get; set; }

        public FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags Flags;

        [JsonIgnore] public ItemRow Item => ItemSheet.GetRow(this.ItemId)!;

        [JsonIgnore] public string FormattedName => this.Phase != null && this.PhaseNames.Length != 1 ? this.Name + " - " + this.GetPhaseName(this.Phase.Value) : this.Name;

        [JsonIgnore] public string Name => this.Item.Base.Name.ExtractText();
        [JsonIgnore] public (Vector4, string)? NextStep { get; set; }

        [JsonIgnore] public List<BitfieldUptime>? UpTimes { get; set; }

        [JsonIgnore] public uint? MapId { get; set; }

        [JsonIgnore] public List<CraftPriceSource>? CraftPrices { get; set; }

        [JsonIgnore] public uint? MarketTotalPrice { get; set; }

        [JsonIgnore] public uint? MarketTotalAvailable { get; set; }
        [JsonIgnore] public uint? MarketAvailable { get; set; }
        [JsonIgnore] public IngredientPreferenceType? LimitType { get; set; }

        [JsonIgnore]
        public decimal? MarketUnitPrice
        {
            get
            {
                if (this.MarketTotalPrice == null || this.MarketAvailable == null || this.MarketAvailable == 0)
                {
                    return 0;
                }
                return (uint)Math.Ceiling((decimal)this.MarketTotalPrice.Value / this.MarketAvailable.Value);
            }
        }

        [JsonIgnore]
        public uint? MarketWorldId { get; set; }

        [JsonIgnore]
        private string[] PhaseNames
        {
            get
            {
                return this._phaseNames ??= this.Item.CompanyCraftSequence?.CompanyCraftParts.Where(c => c.RowId != 0)
                                                .Select(c => c.Base.CompanyCraftType.ValueNullable?.Name.ToString() ?? "Unknown").ToArray() ??
                                            Array.Empty<string>();
            }
        }

        public string GetPhaseName(uint phaseIndex)
        {
            return phaseIndex >= this.PhaseNames.Length ? "" : this.PhaseNames[phaseIndex];
        }

        private string[]? _phaseNames;

        /// <summary>
        /// Has the amount that we actually need been calculated, this is used by CraftList.UpdateStockItems to determine if the quantity in QuantityToStock
        /// </summary>
        public bool InitialQuantityToStockCalculated { get; set; }

        /// <summary>
        /// The total amount that we want to stock if in stock mode otherwise not used
        /// </summary>
        public uint QuantityToStock { get; set; }

        /// <summary>
        /// The total amount that is required for the item or the amount left if in stock mode
        /// </summary>
        public uint QuantityRequired { get; set; }

        /// <summary>
        /// The total amount that is needed once the amount ready and in external sources is factored in
        /// </summary>
        [JsonIgnore]
        public uint QuantityNeeded;

        /// <summary>
        /// The total amount that is needed before ready amounts and external sources are factored in
        /// </summary>
        [JsonIgnore]
        public uint QuantityNeededPreUpdate;

        /// <summary>
        /// The total amount available in your characters inventory
        /// </summary>
        [JsonIgnore]
        public uint QuantityReady;

        /// <summary>
        /// The total amount available in external sources(retainers, etc)
        /// </summary>
        [JsonIgnore]
        public uint QuantityAvailable;

        //The total amount that can be crafted, calculated from the child craft items, also counts for items where a trade in occurs, with the yield factored in
        [JsonIgnore]
        public uint QuantityCanCraft;

        //The total amount that will be retrieved
        [JsonIgnore]
        public uint QuantityWillRetrieve;

        [JsonIgnore]
        public ConcurrentDictionary<(uint,bool), uint> MissingIngredients = new ConcurrentDictionary<(uint,bool), uint>();

        [JsonIgnore]
        public ConcurrentDictionary<(uint,bool), uint> Ingredients = new ConcurrentDictionary<(uint,bool), uint>();

        //The total amount that will be retrieved
        [JsonIgnore]
        public IngredientPreference IngredientPreference
        {
            get
            {
                if (this._ingredientPreference == null)
                {
                    this._ingredientPreference = new IngredientPreference();
                }
                return this._ingredientPreference;
            }
            set => this._ingredientPreference = value;
        }

        private IngredientPreference? _ingredientPreference;

        /// <summary>
        /// The total amount missing from the users inventory
        /// </summary>
        [JsonIgnore]
        public uint QuantityMissingInventory => (uint)Math.Max(0,(int)this.QuantityNeeded + this.QuantityWillRetrieve);

        /// <summary>
        /// The total amount missing from the users inventory including if we got items from retainers
        /// </summary>
        [JsonIgnore]
        public uint QuantityMissingOverall => (uint)Math.Max(0,(int)this.QuantityNeeded);

        /// <summary>
        /// The amount of crafts that need to be performed to get the quantity required factoring in the yield of each craft operation
        /// </summary>
        [JsonIgnore]
        public uint CraftOperationsRequired => (uint)Math.Ceiling((double)this.QuantityCanCraft / this.Yield);

        [JsonIgnore] public bool IsCompleted => this.QuantityMissingInventory == 0;

        public uint RecipeId;

        public bool IsOutputItem;

        //Only for company crafts
        public uint? Phase;

        public uint? Depth;

        [JsonIgnore]
        public RecipeRow? Recipe
        {
            get
            {
                if (this.Item.CanBeCrafted && this.RecipeId == 0)
                {
                    var recipes = this.Item.Recipes;
                    if (recipes.Count != 0)
                    {
                        this.RecipeId = recipes.First().RowId;
                    }
                }
                return this.RecipeId != 0 ? RecipeSheet.GetRow(this.RecipeId) : null;
            }
        }

        [JsonIgnore]
        public uint Yield => this.Recipe?.Base.AmountResult ?? 1u;

        [JsonIgnore]
        public uint PreferenceYield => this.IngredientPreference.Type == IngredientPreferenceType.Crafting ? this.Recipe?.Base.AmountResult ?? 1u : 1u;

        public void ClearChildCrafts()
        {
            this.ChildCrafts = new List<CraftItem>();
        }


        [JsonIgnore]
        public List<CraftItem> ChildCrafts;

        public delegate CraftItem Factory();


        public CraftItem(ItemSheet itemSheet, RecipeSheet recipeSheet)
        {
            ItemSheet = itemSheet;
            RecipeSheet = recipeSheet;
            this.ChildCrafts = new List<CraftItem>();
        }

        public void FromRaw(uint itemId, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags flags, uint quantityRequired, uint? quantityNeeded = null, bool isOutputItem = false, uint? recipeId = null, uint? phase = null, bool flat = false)
        {
            this.ItemId = itemId;
            this.Flags = flags;
            this.QuantityRequired = quantityRequired;
            this.QuantityNeeded = quantityNeeded ?? quantityRequired;
            this.IsOutputItem = isOutputItem;
            this.Phase = phase;
            if (recipeId != null)
            {
                this.RecipeId = recipeId.Value;
            }
            this.QuantityNeededPreUpdate = quantityNeeded ?? quantityRequired;

            this.ChildCrafts = new List<CraftItem>();
            if (this.Item.AvailableAtTimedNode || this.Item.AvailableAtHiddenNode || this.Item.AvailableAtEphemeralNode)
            {
                this.UpTimes = this.Item.GatheringUpTimes;
            }

            if (!this.Item.Base.CanBeHq && this.Flags == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HighQuality) Flags = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None;
        }

        public void SwitchRecipe(uint newRecipeId)
        {
            this.RecipeId = newRecipeId;
            this.ChildCrafts = new List<CraftItem>();
        }

        public void SwitchPhase(uint? newPhase)
        {
            this.Phase = newPhase;
            this.ChildCrafts = new List<CraftItem>();
        }

        public void AddQuantity(uint quantity)
        {
            this.QuantityRequired += quantity;
        }

        public void AddQuantityToStock(uint quantityToStock)
        {
            this.QuantityToStock += quantityToStock;
        }

        public void SetQuantity(uint quantity)
        {
            this.QuantityRequired = quantity;
            this.QuantityNeeded = quantity;
            this.QuantityNeededPreUpdate = quantity;
        }

        public void SetQuantityToStock(uint quantity)
        {
            this.QuantityToStock = quantity;
        }

        public void RemoveQuantity(uint quantity)
        {
            this.QuantityRequired = (uint)Math.Max((int)this.QuantityRequired - (int)quantity, 0);
        }

        public void RemoveQuantityToStock(uint quantityToStock)
        {
            this.QuantityToStock = (uint)Math.Max((int)this.QuantityToStock - (int)quantityToStock, 0);
        }

        public uint GetRoundedQuantity(uint quantity)
        {
            if (this.Yield != 1)
            {
                uint rem = quantity % this.Yield;
                uint result = quantity - rem;
                if (rem >= (this.Yield / 2))
                {
                    result += this.Yield;
                }

                quantity = result;
            }

            return quantity;
        }

        public List<CraftItem> GetFlattenedMaterials(uint depth = 0)
        {
            var list = new List<CraftItem>();

            for (var index = 0; index < this.ChildCrafts.Count; index++)
            {
                var craftItem = this.ChildCrafts[index];
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

        public CraftItem Add(CraftItem a, CraftItem b)
        {
            var craftItem = new CraftItem(a.ItemSheet, a.RecipeSheet);
            craftItem.FromRaw(a.ItemId, a.Flags, a.QuantityRequired + b.QuantityRequired, a.QuantityNeeded + b.QuantityNeeded, a.IsOutputItem, a.RecipeId, a.Phase, true);
            craftItem.QuantityNeeded = a.QuantityNeeded + b.QuantityNeeded;
            craftItem.QuantityNeededPreUpdate = a.QuantityNeededPreUpdate + b.QuantityNeededPreUpdate;
            craftItem.QuantityReady = a.QuantityReady + b.QuantityReady;
            craftItem.QuantityAvailable = a.QuantityAvailable + b.QuantityAvailable;
            craftItem.QuantityCanCraft = a.QuantityCanCraft + b.QuantityCanCraft;
            craftItem.QuantityWillRetrieve = a.QuantityWillRetrieve + b.QuantityWillRetrieve;
            craftItem.MarketTotalPrice = (a.MarketTotalPrice ?? 0) + (b.MarketTotalPrice ?? 0);
            craftItem.MarketAvailable = (a.MarketAvailable ?? 0) + (b.MarketAvailable ?? 0);
            craftItem.MarketTotalAvailable = (a.MarketTotalAvailable ?? 0) + (b.MarketTotalAvailable ?? 0);
            craftItem.QuantityToStock = a.QuantityToStock + b.QuantityToStock;
            craftItem.InitialQuantityToStockCalculated = a.InitialQuantityToStockCalculated || b.InitialQuantityToStockCalculated;
            craftItem.LimitType = a.LimitType ?? b.LimitType;
            //Only apply this fix when not in stock mode
            if (craftItem.QuantityToStock == 0)
            {
                craftItem.QuantityNeeded = craftItem.QuantityNeededPreUpdate - craftItem.QuantityReady -
                                           craftItem.QuantityAvailable;
            }

            if (a.CraftPrices != null && b.CraftPrices != null)
            {
                craftItem.CraftPrices = a.CraftPrices.Concat(b.CraftPrices).ToList();
            }
            else if (a.CraftPrices != null)
            {
                craftItem.CraftPrices = a.CraftPrices;
            }
            else if (b.CraftPrices != null)
            {
                craftItem.CraftPrices = b.CraftPrices;
            }
            if (a.Flags != FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None)
            {
                craftItem.Flags = a.Flags;
            }
            foreach (var ingredient in b.MissingIngredients)
            {
                craftItem.MissingIngredients.TryAdd(ingredient.Key, 0);
                craftItem.MissingIngredients[ingredient.Key] += ingredient.Value;
            }
            foreach (var ingredient in a.MissingIngredients)
            {
                craftItem.MissingIngredients.TryAdd(ingredient.Key, 0);
                craftItem.MissingIngredients[ingredient.Key] += ingredient.Value;
            }
            foreach (var ingredient in b.Ingredients)
            {
                craftItem.Ingredients.TryAdd(ingredient.Key, 0);
                craftItem.Ingredients[ingredient.Key] += ingredient.Value;
            }
            foreach (var ingredient in a.Ingredients)
            {
                craftItem.Ingredients.TryAdd(ingredient.Key, 0);
                craftItem.Ingredients[ingredient.Key] += ingredient.Value;
            }
            if (a.IngredientPreference.Type != IngredientPreferenceType.None)
            {
                craftItem.IngredientPreference = a.IngredientPreference;
            }

            if (b.IngredientPreference.Type != IngredientPreferenceType.None)
            {
                craftItem.IngredientPreference = b.IngredientPreference;
            }


            if (a.Depth != null && b.Depth != null)
            {
                craftItem.Depth = Math.Max(a.Depth.Value, b.Depth.Value);
            }
            else if (a.Depth != null)
            {
                craftItem.Depth = a.Depth;
            }

            return craftItem;
        }

    }
}
