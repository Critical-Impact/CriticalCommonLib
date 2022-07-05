using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Sheets;
using Dalamud.Logging;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache
    {
        private Dictionary<uint, HashSet<(uint, uint)>> _vendorItemLocations;
        private Dictionary<uint, HashSet<(uint, uint)>> _itemVendorLocations;
        private bool _vendorLocationsCalculated = false;
        private bool _npcLevelLookupCalculated = false;
        private Dictionary<uint, HashSet<uint>>? _npcLevelLookup;

        public List<LevelEx> GetNpcLevels(uint eNpcId)
        {
            if(_npcLevelLookup?.ContainsKey(eNpcId) ?? false)
            {
                return _npcLevelLookup[eNpcId].Select(c => Service.ExcelCache.GetSheet<LevelEx>().GetRow(c)).Where(c => c != null).Select(c => c!).ToList();
            }

            return new List<LevelEx>();
        }

        public List<(ENpcResident,GilShop)> GetVendors(uint itemId)
        {
            if (_itemVendorLocations?.ContainsKey(itemId) ?? false)
            {
                return _itemVendorLocations[itemId].Select(c => (Service.ExcelCache.GetSheet<ENpcResident>().GetRow(c.Item1),Service.ExcelCache.GetSheet<GilShop>().GetRow(c.Item2))).Where(c => c.Item1 != null && c.Item2 != null).Select(c => (c.Item1!,c.Item2!)).ToList();
            }

            return new List<(ENpcResident,GilShop)>();
        }
        
    }
}