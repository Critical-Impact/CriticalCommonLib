using System;
using CriticalCommonLib.Interfaces;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public struct NpcLocation : ILocation, IEquatable<NpcLocation>
    {
        public LazyRow<MapEx> MapEx { get; }
        public LazyRow<PlaceName> PlaceName { get; }
        private readonly double X;
        private readonly double Y;

        public NpcLocation(double mapX, double mapY, LazyRow<MapEx> mapEx, LazyRow<PlaceName> placeName)
        {
            X = mapX;
            Y = mapY;
            MapEx = mapEx;
            PlaceName = placeName;
        }
        
        /// <summary>
        ///     Gets the X-coordinate on the 2D-map.
        /// </summary>
        /// <value>The X-coordinate on the 2D-map.</value>
        public double MapX 
        {
            get
            {
                if (MapEx.Value != null)
                {
                    return MapEx.Value.ToMapCoordinate3d(X, MapEx.Value.OffsetX);
                }

                return 0;
            }
        }

        /// <summary>
        ///     Gets the Y-coordinate on the 2D-map.
        /// </summary>
        /// <value>The Y-coordinate on the 2D-map.</value>
        public double MapY
        {
            get
            {
                if (MapEx.Value != null)
                {
                    return MapEx.Value.ToMapCoordinate3d(Y, MapEx.Value.OffsetY);
                }

                return 0;
            }
        }

        
        public string FormattedName
        {
            get
            {
                var map = MapEx.Value?.PlaceName.Value?.Name.ToString() ?? "Unknown Map";
                var region =  MapEx.Value?.PlaceNameRegion.Value?.Name.ToString() ?? "Unknown Territory";
                return region + " - " + map;
            }
        }
        
        public override string ToString()
        {
            return FormattedName;
        }

        public bool Equals(NpcLocation other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && MapEx.Row.Equals(other.MapEx.Row) && PlaceName.Row.Equals(other.PlaceName.Row);
        }

        public override bool Equals(object? obj)
        {
            return obj is NpcLocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, MapEx.Row, PlaceName.Row);
        }
    }
}