using System;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class MapEx : Map
    {
        public LazyRow<PlaceNameEx> PlaceNameEx { get; set; } = null!;
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            PlaceNameEx = new LazyRow<PlaceNameEx>(gameData, PlaceName.Row, language);
        }

        private string? _formattedName;
        public string FormattedName
        {
            get
            {
                if (_formattedName == null)
                {
                    var map = PlaceName.Value?.Name.ToString() ?? "Unknown Map";
                    var region = PlaceNameRegion.Value?.Name.ToString() ?? "Unknown Territory";
                    var subArea = PlaceNameSub.Value?.Name.ToString() ?? null;
                    if (!String.IsNullOrEmpty(subArea))
                    {
                        subArea = " - " + subArea;
                    }

                    _formattedName = region + " - " + map + (subArea ?? "");
                }

                return _formattedName;
            }
        }
    }
}