using System;
using System.Collections.Generic;
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

        public LazyRow<MapEx> MapEx { get; set; } = null!;
        public LazyRow< PlaceNameEx > PlaceNameEx { get; set; } = null!;

        private Dictionary<uint, LazyRow<MapEx>>? _layerIndexCache = null;
        
        public LazyRow<MapEx> GetMapAtLayerIndex(uint layerIndex)
        {
            _layerIndexCache ??= new Dictionary<uint, LazyRow<MapEx>>();

            if (_layerIndexCache.TryGetValue(layerIndex, out var value))
            {
                return value;
            }
            var mapId = Service.ExcelCache.GetMapIdByTerritoryTypeAndMapIndex(RowId, (sbyte)layerIndex);
            _layerIndexCache[layerIndex] = new LazyRow<MapEx>(Service.ExcelCache.GameData, mapId, MapEx.Language);
            return _layerIndexCache[layerIndex];
        }
        private string? _formattedName;

        public string FormattedName => _formattedName ??= (PlaceNameEx.Value?.FormattedName ?? "Unknown");
        private string? _formattedExpandedName;
        public string FormattedExpandedName
        {
            get
            {
                if (_formattedExpandedName == null)
                {
                    var map = MapEx.Value?.PlaceName.Value?.Name.ToString() ?? "Unknown Map";
                    var region = MapEx.Value?.PlaceNameRegion.Value?.Name.ToString() ?? "Unknown Territory";
                    var subArea = MapEx.Value?.PlaceNameSub.Value?.Name.ToString() ?? null;
                    if (!String.IsNullOrEmpty(subArea))
                    {
                        subArea = " - " + subArea;
                    }

                    _formattedExpandedName = region + " - " + map + (subArea ?? "");
                }

                return _formattedExpandedName;
            }
        }

    }
}