using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace CriticalCommonLib.Crafting
{
    public class CraftList
    {
        private List<CraftItem>? _craftItems = new();

        [JsonIgnore]
        public bool BeenUpdated = false;

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

        public void AddCraftItem(uint itemId, uint quantity = 1, ItemFlags flags = ItemFlags.None, uint? phase = null)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase))
            {
                var craftItem = CraftItems.First(c => c.ItemId == itemId && c.Flags == flags && c.Phase == phase);
                craftItem.AddQuantity(quantity);
            }
            else
            {
                CraftItems.Add(new CraftItem(itemId, flags, quantity, true, null, phase));

            }
        }

        public void AddCompanyCraftItem(uint itemId, uint quantity, uint phase, bool includeItem = false, CompanyCraftStatus status = CompanyCraftStatus.Normal)
        {
            if (includeItem)
            {
                AddCraftItem(itemId, quantity, ItemFlags.None, phase);
                return;
            }
            //var tempCraftItem = new CraftItem(itemId, )
                
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
                CraftItems.RemoveAll(c => c.ItemId == itemId && c.Flags == itemFlags);
            }
        }

        public void GenerateCraftChildren()
        {
            foreach (var craftItem in CraftItems)
            {
                craftItem.GenerateRequiredMaterials();
            }
        }
        
        public void MarkCrafted(uint itemId, ItemFlags itemFlags, uint quantity, bool removeEmpty = true)
        {
            if (GetFlattenedMaterials().Any(c =>
                !c.IsOutputItem && c.ItemId == itemId && c.Flags == itemFlags && c.QuantityNeeded != 0))
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
            foreach (var craftItem in CraftItems)
            {
                PluginLog.Log("Calculating items for " + (craftItem.Item?.Name ?? "Unknown"));
                craftItem.Update(characterSources, externalSources);
            }

            BeenUpdated = true;
        }

        public List<CraftItem> GetFlattenedMaterials()
        {
            var list = new List<CraftItem>();
            foreach (var craftItem in CraftItems)
            {
                list.Add(craftItem);
                foreach (var material in craftItem.GetFlattenedMaterials())
                {
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
            foreach (var item in flattenedMaterials)
            {
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
            foreach (var item in flattenedMaterials)
            {
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
            foreach (var item in flattenedMaterials)
            {
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
            foreach (var item in flattenedMaterials)
            {
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
            foreach (var item in flattenedMaterials)
            {
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
            foreach (var item in flattenedMaterials)
            {
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
            foreach (var item in flattenedMaterials)
            {
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