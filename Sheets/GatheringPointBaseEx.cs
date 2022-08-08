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
                new Lazy<List<GatheringPoint>>(CalculateGatheringPoints, LazyThreadSafetyMode.PublicationOnly);
        }

        private Lazy<List<GatheringPoint>> _gatheringPoints = null!;
        
        private List<GatheringPoint> CalculateGatheringPoints()
        {
            if (Service.ExcelCache.GatheringPointBaseToGatheringPoint.ContainsKey(RowId))
            {
                return Service.ExcelCache.GatheringPointBaseToGatheringPoint[RowId]
                    .Select(c => Service.ExcelCache.GetSheet<GatheringPoint>().GetRow(c)).Where(c => c != null)
                    .Select(c => c!).ToList();
                ;
            }

            return new List<GatheringPoint>();
        }

        public Lazy<List<GatheringPoint>> GatheringPoints => _gatheringPoints;
    }
}