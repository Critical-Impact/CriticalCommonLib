using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Collections;
using CriticalCommonLib.Interfaces;

namespace CriticalCommonLib.Sheets
{
    public class ENpc {
        #region Fields

        private ENpcBaseEx? _base;
        private ENpcResidentEx? _resident;
        private ILocation[]? _locations;

        #endregion

        #region Properties

        public uint Key { get; private set; }
        public ENpcCollection Collection { get; private set; }
        public ENpcResidentEx? Resident => _resident ??= Service.ExcelCache.GetENpcResidentExSheet()!.GetRow(Key);
        public ENpcBaseEx? Base => _base ??= Service.ExcelCache.GetENpcBaseExSheet().GetRow(Key);

        public IEnumerable<ILocation> Locations { get { return _locations ??= BuildLocations(); } }

        #endregion

        #region Constructors

        public ENpc(ENpcCollection collection, uint key) {
            Key = key;
            Collection = collection;
        }

        #endregion

        #region Build

        private ILocation[] BuildLocations() {
            return Collection.FindLevels(Key).Cast<ILocation>().ToArray();
        }
        #endregion
        
        public bool IsVendor => Service.ExcelCache.ENpcCollection?.FindShops(this) != null;
        public bool IsHouseVendor => Service.ExcelCache.GetHouseVendor(this.Key) != null;
        public bool IsCalamitySalvager => Key is 1006004;
        public List<IShop>? Shops => Service.ExcelCache.ENpcCollection?.FindShops(this)?.Select(c => Service.ExcelCache.ShopCollection?.Get(c)).Where(c => c != null).Select(c => c!).ToList();


        public override string ToString() {
            return Resident?.Singular ?? "Unknown";
        }
    }
}