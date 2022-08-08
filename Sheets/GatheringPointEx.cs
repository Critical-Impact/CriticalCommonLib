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
        }

        public LazyRow<GatheringPointBaseEx> GatheringPointBaseEx = null!;
    }
}