using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache
    {
        private bool _gatheringItemLinksCalculated;
        private bool _gatheringItemPointLinksCalculated;

        private Dictionary<uint, GatheringItem> GatheringItems { get; set; } = new();

        private Dictionary<uint, uint> GatheringItemsLinks { get; set; } = new();

        private Dictionary<uint, GatheringItemPoint> GatheringItemPoints { get; set; } = new();

        private Dictionary<uint, uint> GatheringItemPointLinks { get; set; } = new();

        private Dictionary<uint, GatheringPoint> GatheringPoints { get; set; } = new();

        private Dictionary<uint, GatheringPointTransient> GatheringPointsTransients { get; set; } = new();

        public GatheringPointTransient? GetGatheringPointTransient(uint itemId)
        {
            if (!GatheringPointsTransients.ContainsKey(itemId))
            {
                var item = Service.ExcelCache.GetGatheringPointTransientSheet().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringPointsTransients[itemId] = item;
            }

            return GatheringPointsTransients[itemId];
        }

        public bool CanBeGathered(uint itemId)
        {
            if (!_gatheringItemLinksCalculated)
            {
                CalculateGatheringItemLinks();
            }

            return GatheringItemsLinks.ContainsKey(itemId);
        }

        public void CalculateGatheringItemPointLinks()
        {
            if (!_gatheringItemPointLinksCalculated)
            {
                _gatheringItemPointLinksCalculated = true;
                foreach (var gatheringItemPoint in Service.ExcelCache.GetGatheringItemPointSheet())
                {
                    if (!GatheringItemPointLinks.ContainsKey(gatheringItemPoint.RowId))
                    {
                        GatheringItemPointLinks.Add(gatheringItemPoint.RowId, gatheringItemPoint.GatheringPoint.Row);
                    }
                }
            }
        }

        public void CalculateGatheringItemLinks()
        {
            if (!_gatheringItemLinksCalculated)
            {
                _gatheringItemLinksCalculated = true;
                foreach (var gatheringItem in Service.ExcelCache.GetGatheringItemSheet())
                {
                    if (!GatheringItemsLinks.ContainsKey((uint)gatheringItem.Item))
                    {
                        GatheringItemsLinks.Add((uint)gatheringItem.Item, gatheringItem.RowId);
                    }
                }
            }
        }
    }
}