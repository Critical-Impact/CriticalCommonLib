using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services
{
    public static partial class ExcelCache
    {
        private static bool _gatheringItemLinksCalculated;
        private static bool _gatheringItemPointLinksCalculated;

        private static Dictionary<uint, GatheringItem> GatheringItems { get; set; } = new();

        private static Dictionary<uint, uint> GatheringItemsLinks { get; set; } = new();

        private static Dictionary<uint, GatheringItemPoint> GatheringItemPoints { get; set; } = new();

        private static Dictionary<uint, uint> GatheringItemPointLinks { get; set; } = new();

        private static Dictionary<uint, GatheringPoint> GatheringPoints { get; set; } = new();

        private static Dictionary<uint, GatheringPointTransient> GatheringPointsTransients { get; set; } = new();

        public static GatheringItem? GetGatheringItem(uint itemId)
        {
            if (!GatheringItems.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringItem>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringItems[itemId] = item;
            }

            return GatheringItems[itemId];
        }

        public static GatheringItemPoint? GetGatheringItemPoint(uint itemId)
        {
            if (!GatheringItemPoints.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringItemPoint>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringItemPoints[itemId] = item;
            }

            return GatheringItemPoints[itemId];
        }

        public static GatheringPointTransient? GetGatheringPointTransient(uint itemId)
        {
            if (!GatheringPointsTransients.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringPointTransient>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringPointsTransients[itemId] = item;
            }

            return GatheringPointsTransients[itemId];
        }

        public static GatheringPoint? GetGatheringPoint(uint itemId)
        {
            if (!GatheringPoints.ContainsKey(itemId))
            {
                var item = ExcelCache.GetSheet<GatheringPoint>().GetRow(itemId);
                if (item == null)
                {
                    return null;
                }

                GatheringPoints[itemId] = item;
            }

            return GatheringPoints[itemId];
        }

        public static GatheringItem? GetGatheringItemByItemId(uint itemId)
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

        public static bool CanBeGathered(uint itemId)
        {
            if (!_gatheringItemLinksCalculated)
            {
                CalculateGatheringItemLinks();
            }

            return GatheringItemsLinks.ContainsKey(itemId);
        }

        public static void CalculateGatheringItemPointLinks()
        {
            if (!_gatheringItemPointLinksCalculated && Initialised)
            {
                _gatheringItemPointLinksCalculated = true;
                foreach (var gatheringItemPoint in ExcelCache.GetSheet<GatheringItemPoint>())
                {
                    if (!GatheringItemPointLinks.ContainsKey(gatheringItemPoint.RowId))
                    {
                        GatheringItemPointLinks.Add(gatheringItemPoint.RowId, gatheringItemPoint.GatheringPoint.Row);
                    }
                }
            }
        }

        public static void CalculateGatheringItemLinks()
        {
            if (!_gatheringItemLinksCalculated && Initialised)
            {
                _gatheringItemLinksCalculated = true;
                foreach (var gatheringItem in ExcelCache.GetSheet<GatheringItem>())
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