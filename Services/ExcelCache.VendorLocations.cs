using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache
    {
        private readonly Dictionary<uint, HashSet<(uint, uint)>> _itemVendorLocations = null!;
        private readonly Dictionary<uint, HashSet<uint>>? _npcLevelLookup = null!;

        public List<LevelEx> GetNpcLevels(uint eNpcId)
        {
            if(_npcLevelLookup?.ContainsKey(eNpcId) ?? false)
            {
                return _npcLevelLookup[eNpcId].Select(c => Service.ExcelCache.GetLevelExSheet().GetRow(c)).Where(c => c != null).Select(c => c!).ToList();
            }

            return new List<LevelEx>();
        }

        public List<(ENpcResidentEx,GilShopEx)> GetVendors(uint itemId)
        {
            if (_itemVendorLocations?.ContainsKey(itemId) ?? false)
            {
                return _itemVendorLocations[itemId].Select(c => (Service.ExcelCache.GetENpcResidentExSheet()!.GetRow(c.Item1),Service.ExcelCache.GetGilShopExSheet().GetRow(c.Item2))).Where(c => c.Item1 != null && c.Item2 != null).Select(c => (c.Item1!,c.Item2!)).ToList();
            }

            return new List<(ENpcResidentEx,GilShopEx)>();
        }
        
    }
}