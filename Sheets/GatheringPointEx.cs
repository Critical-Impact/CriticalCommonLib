using System.Linq;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class GatheringPointEx : GatheringPoint
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            GatheringPointBaseEx = new LazyRow<GatheringPointBaseEx>(gameData, GatheringPointBase.Row, language);
            GatheringPointTransient = new LazyOneToOneRow<GatheringPointTransient, GatheringPointEx>(this, gameData,
                language,
                sheet =>
                {
                    return sheet.ToDictionary(c => c.RowId, c => c.RowId);
                }, "GatheringPointTransient");
        }

        public LazyOneToOneRow<GatheringPointTransient, GatheringPointEx> GatheringPointTransient = null!;

        public LazyRow<GatheringPointBaseEx> GatheringPointBaseEx = null!;
    }
}