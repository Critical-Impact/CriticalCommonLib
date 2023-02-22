using System;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models
{
    public class GatheringSource
    {
        private GatheringType _gatheringType;
        private GatheringItemLevelConvertTable _level;
        private TerritoryType _territoryType;
        private PlaceName _placeName;

        public GatheringType GatheringType => _gatheringType;

        public GatheringItemLevelConvertTable Level => _level;

        public TerritoryType TerritoryType => _territoryType;

        public PlaceName PlaceName => _placeName;

        public GatheringSource(GatheringType gatheringType, GatheringItemLevelConvertTable level, TerritoryType territoryType, PlaceName placeName)
        {
            _gatheringType = gatheringType;
            _level = level;
            _territoryType = territoryType;
            _placeName = placeName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_gatheringType.RowId, _level.RowId, _territoryType.RowId, _placeName.RowId);

        }

        public ItemSource Source
        {
            get
            {
                if (_gatheringType.RowId == 0)
                {
                    return new ItemSource("Mining", 60438, null);
                }
                //Quarrying
                if (_gatheringType.RowId == 1)
                {
                    return new ItemSource("Quarrying", 60437, null);
                }
                //Logging
                if (_gatheringType.RowId == 2)
                {
                    return new ItemSource("Logging", 60433, null);
                }
                //Harvesting
                if (_gatheringType.RowId == 3)
                {
                    return new ItemSource("Harvesting", 60432, null);
                }

                return new ItemSource("Unknown", 66310, null);
            }
        }
    }
}