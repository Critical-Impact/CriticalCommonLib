using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class MapEx : Map
    {
        public LazyRow<PlaceNameEx> PlaceNameEx { get; set; }
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            PlaceNameEx = new LazyRow<PlaceNameEx>(gameData, PlaceName.Row, language);
        }
    }
}