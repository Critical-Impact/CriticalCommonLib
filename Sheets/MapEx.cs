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

        #region Coordinates
        /// <summary>
        ///     Convert a X- or Y-coordinate into an offset, map-scaled 2D-coordinate.
        /// </summary>
        /// <param name="value">The coordinate to convert.</param>
        /// <param name="offset">The coordinate offset from this map.</param>
        /// <returns><c>value</c> converted and scaled to this map.</returns>
        public double ToMapCoordinate2d(int value, int offset)
        {
            var c = SizeFactor / 100.0;
            var offsetValue = value + offset;
            return (41.0 / c) * (offsetValue / 2048.0) + 1;
        }

        /// <summary>
        ///     Convert a X- or Z-coordinate from world-space into its 2D-coordinate.
        /// </summary>
        /// <param name="value">The coordinate in world-space to convert.</param>
        /// <param name="offset">The coordinate offset from this map in world-space.</param>
        /// <returns><c>value</c> converted into 2D-space.</returns>
        public double ToMapCoordinate3d(double value, int offset)
        {
            var c = SizeFactor / 100.0;
            var offsetValue = (value + offset) * c;
            return ((41.0 / c) * ((offsetValue + 1024.0) / 2048.0)) + 1;
        }
        #endregion

    }
}