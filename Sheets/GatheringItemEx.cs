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
    public class GatheringItemEx : GatheringItem
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            _gatheringItemPoints =
                new Lazy<List<GatheringPointEx>>(CalculateGatheringItems, LazyThreadSafetyMode.PublicationOnly);
        }

        private Lazy<List<GatheringPointEx>> _gatheringItemPoints = null!;
        
        private List<GatheringPointEx> CalculateGatheringItems()
        {
            if (Service.ExcelCache.GatheringItemToGatheringItemPoint.ContainsKey(RowId))
            {
                return Service.ExcelCache.GatheringItemToGatheringItemPoint[RowId]
                    .Select(c => Service.ExcelCache.GetSheet<GatheringPointEx>().GetRow(c)).Where(c => c != null)
                    .Select(c => c!).ToList();
                ;
            }

            return new List<GatheringPointEx>();
        }

        public Lazy<List<GatheringPointEx>> GatheringItemPoints => _gatheringItemPoints;
    }
}