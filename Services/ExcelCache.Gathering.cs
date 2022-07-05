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

        public GatheringItem? GetGatheringItem(uint itemId)
        {
            if (!GatheringItems.ContainsKey(itemId))
            {
                var item = Service.ExcelCache.GetSheet<GatheringItem>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringItems[itemId] = item;
            }

            return GatheringItems[itemId];
        }

        public GatheringItemPoint? GetGatheringItemPoint(uint itemId)
        {
            if (!GatheringItemPoints.ContainsKey(itemId))
            {
                var item = Service.ExcelCache.GetSheet<GatheringItemPoint>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringItemPoints[itemId] = item;
            }

            return GatheringItemPoints[itemId];
        }

        public GatheringPointTransient? GetGatheringPointTransient(uint itemId)
        {
            if (!GatheringPointsTransients.ContainsKey(itemId))
            {
                var item = Service.ExcelCache.GetSheet<GatheringPointTransient>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringPointsTransients[itemId] = item;
            }

            return GatheringPointsTransients[itemId];
        }

        public GatheringPoint? GetGatheringPoint(uint itemId)
        {
            if (!GatheringPoints.ContainsKey(itemId))
            {
                var item = Service.ExcelCache.GetSheet<GatheringPoint>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringPoints[itemId] = item;
            }

            return GatheringPoints[itemId];
        }

        public GatheringItem? GetGatheringItemByItemId(uint itemId)
        {
            if (!_gatheringItemLinksCalculated)
            {
                CalculateGatheringItemLinks();
            }

            if (GatheringItemsLinks.ContainsKey(itemId))
            {
                return GetGatheringItem(GatheringItemsLinks[itemId]);
            }

            return null;
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
                foreach (var gatheringItemPoint in Service.ExcelCache.GetSheet<GatheringItemPoint>())
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
                foreach (var gatheringItem in Service.ExcelCache.GetSheet<GatheringItem>())
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