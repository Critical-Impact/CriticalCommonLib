using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
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
        
        /// <summary>
        /// The total amount that is required for the item
        /// </summary>
        public uint QuantityRequired { get; set; } 

        /// <summary>
        /// The total amount that is needed once the amount ready and in external sources is factored in
        /// </summary>
        [JsonIgnore]
        public uint QuantityNeeded;

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

        //The total amount that can be crafted, calculated from the child craft items 
        [JsonIgnore]
        public uint QuantityCanCraft;

        //The total amount that will be retrieved
        [JsonIgnore]
        public uint QuantityWillRetrieve;

        //The total amount that will be retrieved
        [JsonIgnore]
        public IngredientPreference IngredientPreference
        {
            get
            {
                if (_ingredientPreference == null)
                {
                    _ingredientPreference = new IngredientPreference();
                }
                return _ingredientPreference;
            }
            set => _ingredientPreference = value;
        }

        private IngredientPreference? _ingredientPreference;

        /// <summary>
        /// The total amount missing from the users inventory
        /// </summary>
        [JsonIgnore]
        public uint QuantityMissing => (uint)Math.Max(0,(int)QuantityNeeded - QuantityReady);

        //The total amount to retrieve from retainers
        [JsonIgnore]
        public uint QuantityToRetrieve => IsOutputItem ? 0 : Math.Min(QuantityRequired, QuantityAvailable);

        //The total amount missing from the users inventory including if we got items from retainers
        [JsonIgnore]
        public uint QuantityUnavailable => (uint)Math.Max(0,(int)QuantityNeeded - QuantityAvailable);

        //The total amount missing from the users inventory
        [JsonIgnore]
        public uint RequiredQuantityUnavailable => (uint)Math.Max(0,(int)QuantityRequired - QuantityReady - QuantityAvailable);

        [JsonIgnore] public bool IsCompleted => QuantityUnavailable == 0 && QuantityWillRetrieve == 0;

        public uint RecipeId;

        public bool IsOutputItem;
        
        //Only for company crafts
        public uint? Phase;

        public uint? Depth;
        
        [JsonIgnore]
        public RecipeEx? Recipe
        {
            get
            {
                if (Item.CanBeCrafted && RecipeId == 0)
                {
                    var recipes = Service.ExcelCache.GetItemRecipes(ItemId);
                    if (recipes.Count != 0)
                    {
                        RecipeId = recipes.First().RowId;
                    }
                }
                return RecipeId != 0 ? Service.ExcelCache.GetRecipe(RecipeId) : null;
            }
        }

        [JsonIgnore]
        public uint Yield => Recipe?.AmountResult ?? 1u;

        public void ClearChildCrafts()
        {
            ChildCrafts = new List<CraftItem>();
        }
        
        
        [JsonIgnore]
        public List<CraftItem> ChildCrafts;


        public CraftItem()
        {
            ChildCrafts = new List<CraftItem>();
        }
        
        public CraftItem(uint itemId, InventoryItem.ItemFlags flags, uint quantityRequired, uint? quantityNeeded = null, bool isOutputItem = false, uint? recipeId = null, uint? phase = null, bool flat = false)
        {
            ItemId = itemId;
            Flags = flags;
            QuantityRequired = quantityRequired;
            QuantityNeeded = quantityNeeded ?? quantityRequired;
            IsOutputItem = isOutputItem;
            Phase = phase;
            if (recipeId != null)
            {
                RecipeId = recipeId.Value;
            }

            ChildCrafts = new List<CraftItem>();
        }

        public int SourceIcon
        {
            get
            {
                return IngredientPreference.Type switch
                {
                    IngredientPreferenceType.Crafting => Recipe?.CraftTypeEx.Value?.Icon ?? Icons.CraftIcon,
                    IngredientPreferenceType.None => Item.Icon,
                    _ => IngredientPreference.SourceIcon!.Value
                };
            }
        }

        public string SourceName
        {
            get
            {
                return IngredientPreference.Type switch
                {
                    IngredientPreferenceType.Crafting => Recipe?.CraftTypeEx.Value?.FormattedName ?? "Unknown Craft",
                    IngredientPreferenceType.None => "N/A",
                    _ => IngredientPreference.FormattedName
                };
            }
        }


        public void SwitchRecipe(uint newRecipeId)
        {
            RecipeId = newRecipeId;
            ChildCrafts = new List<CraftItem>();
        }

        public void SwitchPhase(uint newPhase)
        {
            Phase = newPhase;
            ChildCrafts = new List<CraftItem>();
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

        
        
        public List<CraftItem> GetFlattenedMaterials(uint depth = 0)
        {
            var list = new List<CraftItem>();

            for (var index = 0; index < ChildCrafts.Count; index++)
            {
                var craftItem = ChildCrafts[index];
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
            var craftItem = new CraftItem(a.ItemId, a.Flags, a.QuantityRequired + b.QuantityRequired, a.QuantityNeeded + b.QuantityNeeded, a.IsOutputItem, a.RecipeId, a.Phase, true);
            craftItem.QuantityNeeded = a.QuantityNeeded + b.QuantityNeeded;
            craftItem.QuantityReady = a.QuantityReady + b.QuantityReady;
            craftItem.QuantityAvailable = a.QuantityAvailable + b.QuantityAvailable;
            craftItem.QuantityCanCraft = a.QuantityCanCraft + b.QuantityCanCraft;
            craftItem.QuantityWillRetrieve = a.QuantityWillRetrieve + b.QuantityWillRetrieve;
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
