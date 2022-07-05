using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using CriticalCommonLib.Collections;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Services;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class ENpc {
        #region Fields

        private ENpcBase? _Base;
        private ENpcResident? _Resident;
        private ILocation[]? _Locations;

        #endregion

        #region Properties

        public uint Key { get; private set; }
        public ENpcCollection Collection { get; private set; }
        public ENpcResident? Resident { get { return _Resident ?? Service.ExcelCache.GetSheet<ENpcResident>().GetRow(Key); } }
        public ENpcBase? Base { get { return _Base ?? Service.ExcelCache.GetSheet<ENpcBase>().GetRow(Key); } }

        public IEnumerable<ILocation> Locations { get { return _Locations ??= BuildLocations(); } }

        #endregion

        #region Constructors

        public ENpc(ENpcCollection collection, uint key) {
            Key = key;
            Collection = collection;
        }

        #endregion

        #region Build

        private LevelEx[] BuildGameLevels(HashSet<uint> levelIds)
        {
            return Service.ExcelCache.GetSheet<LevelEx>().Where(c => levelIds.Contains(c.RowId)).ToArray();
        }

        private ILocation[] BuildLocations() {
            return BuildGameLevels(Collection.FindLevels(Key)).Cast<ILocation>().ToArray();
        }
        #endregion

        public override string ToString() {
            return Resident?.Singular ?? "Unknown";
        }
    }
}