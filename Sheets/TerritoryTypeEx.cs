using System.Collections.Generic;
using System.Linq;
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

        private Dictionary<uint, LazyRow<MapEx>>? _layerIndexCache = null;
        
        public LazyRow<MapEx> GetMapAtLayerIndex(uint layerIndex)
        {
            _layerIndexCache ??= new Dictionary<uint, LazyRow<MapEx>>();

            if (_layerIndexCache.TryGetValue(layerIndex, out var value))
            {
                return value;
            }
            var layerMap = Service.ExcelCache.GetMapSheet()
                .FirstOrDefault(c => c.TerritoryType.Row == RowId && c.MapIndex == layerIndex, null);
            //HACK
            _layerIndexCache[layerIndex] = new LazyRow<MapEx>(Service.ExcelCache.GameData, layerMap?.RowId ?? 0, MapEx.Language);
            return _layerIndexCache[layerIndex];
        }
    }
}