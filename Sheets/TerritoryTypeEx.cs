using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class TerritoryTypeEx : TerritoryType
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            MapEx = new LazyRow<MapEx>(gameData, Map.Row, language);
            PlaceNameEx = new LazyRow<PlaceNameEx>(gameData, PlaceName.Row, language);
        }
        
        public LazyRow< MapEx > MapEx { get; set; }
        public LazyRow< PlaceNameEx > PlaceNameEx { get; set; }
    }
}