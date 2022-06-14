using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace CriticalCommonLib.Crafting
{
    public class CraftList
    {
        private List<CraftItem> _craftItems = new();
        private Dictionary<uint, uint> _requiredItems = new ();
        private List<CraftItem> _flattenedMaterials = new();
        private bool _needsRefresh = true;
        private bool _useInventoryItems = true;

        [JsonIgnore]
        public Dictionary<uint, uint> RequiredItems
        {
            get
            {
                return _requiredItems;
            }
        }

        public List<CraftItem> CraftItems => _craftItems;

        [JsonIgnore]
        public List<CraftItem> FlattenedMaterials => _flattenedMaterials;

        public void AddCraftItem(CraftItem craftItem)
        {
            if (!CraftItems.Contains(craftItem))
            {
                CraftItems.Add(craftItem);
            }
        }

        public void AddCraftItem(uint itemId, uint quantity = 1, ItemFlags flags = ItemFlags.None, bool isOutputItem = true)
        {
            AddCraftItem(new CraftItem(itemId, flags, quantity, quantity, isOutputItem));
        }

        public void RemoveCraftItem(uint itemId, ItemFlags flags)
        {
            if (CraftItems.Any(c => c.ItemId == itemId && c.Flags == flags))
            {
                CraftItems.RemoveAll(c => c.ItemId == itemId && c.Flags == flags);
            }
        }

        public Dictionary<uint, List<CraftItemSource>> RefreshRequiredItems(Dictionary<uint, List<CraftItemSource>> availableMaterials)
        {
            var requiredItems = new Dictionary<uint, uint>();
            var allFlattenedMaterials = new List<CraftItem>();
            foreach (var craftItem in CraftItems)
            {
                craftItem.RecalculateRequiredQuantities(availableMaterials );
                allFlattenedMaterials.Add(craftItem);
                var flattenedMaterials = craftItem.GetFlattenedMaterials();
                foreach (var flattenedMaterial in flattenedMaterials)
                {
                    allFlattenedMaterials.Add(flattenedMaterial);
                }
                var materialTotals = CraftItem.GetMaterialTotals(flattenedMaterials);
                
                foreach (var materials in materialTotals)
                {
                    if (!requiredItems.ContainsKey(materials.Key))
                    {
                        requiredItems.Add(materials.Key, 0);
                    }

                    requiredItems[materials.Key] += materials.Value;
                }
            }

            _requiredItems = requiredItems;
            _flattenedMaterials = allFlattenedMaterials;
            _needsRefresh = false;
            return availableMaterials;
        }
        
    }
}