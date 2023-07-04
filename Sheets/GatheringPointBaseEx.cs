using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class GatheringPointBaseEx : GatheringPointBase
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            _gatheringPoints =
                new Lazy<List<GatheringPointEx>>(CalculateGatheringPoints, LazyThreadSafetyMode.PublicationOnly);
            LazyGatheringPoints = new LazyRelated<GatheringPointEx, GatheringPointBaseEx>(this, gameData, language,
                exes =>
                {
                    return exes.GroupBy(c => c.GatheringPointBaseEx.Row).ToDictionary(c => c.Key, c => c.Select(c => c.RowId).ToList());
                }, "GatheringPoints");

            var items = Item.Where(c => c != 0).ToList();
            Items = new(items.Count);
            foreach (var item in items)
            {
                Items.Add(new (gameData, item, language));
            }
        }

        public List<LazyRow<GatheringItemEx>> Items = null!;

        public LazyRelated<GatheringPointEx, GatheringPointBaseEx> LazyGatheringPoints = null!;

        private Lazy<List<GatheringPointEx>> _gatheringPoints = null!;
        
        private List<GatheringPointEx> CalculateGatheringPoints()
        {
            if (Service.ExcelCache.GatheringPointBaseToGatheringPoint.ContainsKey(RowId))
            {
                return Service.ExcelCache.GatheringPointBaseToGatheringPoint[RowId]
                    .Select(c => Service.ExcelCache.GetGatheringPointExSheet().GetRow(c)).Where(c => c != null)
                    .Select(c => c!).ToList();
                ;
            }

            return new List<GatheringPointEx>();
        }

        public Lazy<List<GatheringPointEx>> GatheringPoints => _gatheringPoints;
    }
}