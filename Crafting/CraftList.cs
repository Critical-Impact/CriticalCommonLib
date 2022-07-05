using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.MarketBoard;
using CriticalCommonLib.Models;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace CriticalCommonLib.Crafting
{
    public class CraftList
    {
        private List<CraftItem>? _craftItems = new();

        [JsonIgnore]
        public bool BeenUpdated = false;

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
            set => _craftItems = value;
        }

        public void CalculateCosts()
        {
            var minimumNQCost = 0u;
            var minimumHQCost = 0u;
            var list = GetFlattenedMergedMaterials();
            for (var index = 0; index < list.Count; index++)
            {
                var craftItem = list[index];
                if (!craftItem.IsOutputItem)
                {
                    var priceData = Cache.GetPricing(craftItem.ItemId, false);
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

        public void AddCraftItem(uint itemId, uint quantity = 1, ItemFlags flags = ItemFlags.None, uint? phase = null)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                craftItem.AddQuantity(quantity);
            }
            else
            {
                var newCraftItems = CraftItems.ToList();
                newCraftItems.Add(new CraftItem(itemId, flags, quantity, true, null, phase));
                CraftItems = newCraftItems;
            }
        }

        public void AddCompanyCraftItem(uint itemId, uint quantity, uint phase, bool includeItem = false, CompanyCraftStatus status = CompanyCraftStatus.Normal)
        {
            if (includeItem)
            {
                AddCraftItem(itemId, quantity, ItemFlags.None, phase);
                return;
            }
        }

        public void SetCraftRecipe(uint itemId, uint newRecipeId)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.IsOutputItem))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.IsOutputItem);
                craftItem.SwitchRecipe(newRecipeId);
            }
        }

        public void SetCraftRequiredQuantity(uint itemId, uint quantity, ItemFlags flags = ItemFlags.None, uint? phase = null)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                craftItem.SetQuantity(quantity);
            }
        }
        
        public void RemoveCraftItem(uint itemId, ItemFlags itemFlags)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags))
            {
                var withRemoved = CraftItems.ToList();
                withRemoved.RemoveAll(c => c.ItemId == itemId && c.Flags == itemFlags);
                CraftItems = withRemoved;
            }
        }

        public void GenerateCraftChildren()
        {
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                craftItem.GenerateRequiredMaterials();
            }
        }
        
        public void MarkCrafted(uint itemId, ItemFlags itemFlags, uint quantity, bool removeEmpty = true)
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
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == itemFlags && c.QuantityRequired == 0) && removeEmpty)
            {
                RemoveCraftItem(itemId, itemFlags);
            }
        }

        public void Update(Dictionary<uint, List<CraftItemSource>> characterSources,
            Dictionary<uint, List<CraftItemSource>> externalSources)
        {
            for (var index = 0; index < CraftItems.Count; index++)
            {
                var craftItem = CraftItems[index];
                //PluginLog.Log("Calculating items for " + craftItem.Item.Name);
                craftItem.Update(characterSources, externalSources);
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

                dictionary[item.ItemId] += item.QuantityUnavailable;
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

                dictionary[item.ItemId] += item.QuantityUnavailable;
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