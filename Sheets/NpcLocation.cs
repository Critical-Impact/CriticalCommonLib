using System;
using CriticalCommonLib.Interfaces;
using Dalamud.Utility;
using Lumina.Excel;

namespace CriticalCommonLib.Sheets
{
    public class NpcLocation : ILocation, IEquatable<NpcLocation>
    {
        public LazyRow<MapEx> MapEx { get; }
        public LazyRow<PlaceNameEx> PlaceNameEx { get; }
        public LazyRow<TerritoryTypeEx> TerritoryTypeEx { get; }
        private readonly double X;
        private readonly double Y;
        private readonly bool AlreadyConverted;

        public NpcLocation(double mapX, double mapY, LazyRow<MapEx> mapEx, LazyRow<PlaceNameEx> placeNameEx, LazyRow<TerritoryTypeEx> territoryTypeEx, bool alreadyConverted = false)
        {
            X = mapX;
            Y = mapY;
            MapEx = mapEx;
            PlaceNameEx = placeNameEx;
            TerritoryTypeEx = territoryTypeEx;
            AlreadyConverted = alreadyConverted;
        }
        
        /// <summary>
        ///     Gets the X-coordinate on the 2D-map.
        /// </summary>
        /// <value>The X-coordinate on the 2D-map.</value>
        public double MapX 
        {
            get
            {
                if (AlreadyConverted)
                {
                    return X;
                }
                if (MapEx.Value != null)
                {
                    return MapUtil.ConvertWorldCoordXZToMapCoord((float)X, MapEx.Value.SizeFactor, MapEx.Value.OffsetX);
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
                if (AlreadyConverted)
                {
                    return Y;
                }
                if (MapEx.Value != null)
                {
                    return MapUtil.ConvertWorldCoordXZToMapCoord((float)Y, MapEx.Value.SizeFactor, MapEx.Value.OffsetY);
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
                var subArea =  MapEx.Value?.PlaceNameSub.Value?.Name.ToString() ?? null;
                if (!String.IsNullOrEmpty(subArea))
                {
                    subArea = " - " + subArea;
                }
                return region + " - " + map + (subArea ?? "");
            }
        }
        
        public override string ToString()
        {
            return FormattedName;
        }

        public bool Equals(NpcLocation? other)
        {
            if (other == null)
            {
                return false;
            }
            return X.Equals(other.X) && Y.Equals(other.Y) && MapEx.Row.Equals(other.MapEx.Row) && PlaceNameEx.Row.Equals(other.PlaceNameEx.Row);
        }

        public override bool Equals(object? obj)
        {
            return obj is NpcLocation other && Equals(other);
        }


        public bool EqualRounded(NpcLocation other)
        {
            if (MapEx.Row.Equals(other.MapEx.Row) && PlaceNameEx.Row.Equals(other.PlaceNameEx.Row))
            {
                var x = (int) MapX;
                var y = (int) MapY;
                var otherX = (int) other.MapX;
                var otherY = (int) other.MapY;
                if (x == otherX && y == otherY)
                {
                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, MapEx.Row, PlaceNameEx.Row);
        }
    }
}