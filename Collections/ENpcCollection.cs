using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Services;
using CriticalCommonLib.Sheets;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Collections
{
public class ENpcCollection : IEnumerable<ENpc> {
        #region Fields

        private readonly Dictionary<uint, ENpc> _Inner = new Dictionary<uint, ENpc>();
        private Dictionary<uint, List<ENpc>>? _ENpcDataMap;
        private Dictionary<uint, HashSet<uint>>? _ENpcLevelMap;

        #endregion

        #region Properties

        public ExcelSheet<ENpcBase> BaseSheet { get; private set; }
        public ExcelSheet<ENpcResident> ResidentSheet { get; private set; }

        #endregion

        #region Constructors

        public ENpcCollection() {
            BaseSheet = Service.ExcelCache.GetSheet<ENpcBase>();
            ResidentSheet = Service.ExcelCache.GetSheet<ENpcResident>();
        }

        #endregion

        #region IEnumerable<ENpc> Members

        public IEnumerator<ENpc> GetEnumerator() {
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        #region Get

        public ENpc this[uint key] {
            get { return Get(key); }
        }
        public ENpc Get(uint key) {
            if (_Inner.ContainsKey(key))
                return _Inner[key];

            var enpc = new ENpc(this, key);
            _Inner.Add(key, enpc);
            return enpc;
        }

        #endregion

        #region Enumerator

        private class Enumerator : IEnumerator<ENpc> {
            #region Fields

            private readonly IEnumerator<ENpcBase> _BaseEnumerator;
            private readonly ENpcCollection _Collection;

            #endregion

            #region Constructors

            #region Constructor

            public Enumerator(ENpcCollection collection) {
                _Collection = collection;
                _BaseEnumerator = collection.BaseSheet.GetEnumerator();
            }

            #endregion

            #endregion

            #region IEnumerator<ENpc> Members

            public ENpc Current { get; private set; }

            #endregion

            #region IDisposable Members

            public void Dispose() {
                _BaseEnumerator.Dispose();
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current { get { return Current; } }

            public bool MoveNext() {
                var result = _BaseEnumerator.MoveNext();
                Current = result ? _Collection.Get(_BaseEnumerator.Current.RowId) : null!;
                return result;
            }

            public void Reset() {
                Current = null;
                _BaseEnumerator.Reset();
            }

            #endregion
        }

        #endregion

        #region Find

        public IEnumerable<ENpc> FindWithData(uint value) {
            if (_ENpcDataMap == null)
                _ENpcDataMap = BuildDataMap();
            if (_ENpcDataMap.ContainsKey(value))
                return _ENpcDataMap[value];
            return Array.Empty<ENpc>();
        }

        public HashSet<uint> FindLevels(uint npcId) {
            if (_ENpcLevelMap == null)
            {
                _ENpcLevelMap = BuildLevelMap();
            }

            if (_ENpcLevelMap.ContainsKey(npcId))
            {
                return _ENpcLevelMap[npcId];
            }
            return new HashSet<uint>();
        }

        private Dictionary<uint,HashSet<uint>> BuildLevelMap()
        {
            var sTerritoryTypes = Service.ExcelCache.GetSheet<TerritoryType>();
            var levelSheet = Service.ExcelCache.GetSheet<LevelEx>();
            Dictionary<uint, HashSet<uint>> npcLevelLookup = new Dictionary<uint, HashSet<uint>>();
            
            foreach (var sTerritoryType in sTerritoryTypes)
            {
                var bg = sTerritoryType.Bg.ToString();
                if (string.IsNullOrEmpty(bg))
                    continue;

                var lgbFileName = "bg/" + bg.Substring(0, bg.IndexOf("/level/") + 1) + "level/planevent.lgb";
                var sLgbFile = Service.ExcelCache.GetFile<LgbFile>(lgbFileName);
                if (sLgbFile == null)
                {
                    continue;
                }

                foreach (var sLgbGroup in sLgbFile.Layers)
                {
                    foreach (var instanceObject in sLgbGroup.InstanceObjects)
                    {
                        if (instanceObject.AssetType == LayerEntryType.EventNPC)
                        {
                            var eventNpc = (LayerCommon.ENPCInstanceObject)instanceObject.Object;
                            var npcRowId = eventNpc.ParentData.ParentData.BaseId;
                            var levelId = instanceObject.InstanceId;
                            if (levelId != 0 && npcRowId != 0)
                            {
                                if (!npcLevelLookup.ContainsKey(npcRowId))
                                {
                                    npcLevelLookup.Add(npcRowId, new HashSet<uint>());
                                }

                                if (!npcLevelLookup[npcRowId].Contains(levelId))
                                {
                                    npcLevelLookup[npcRowId].Add(levelId);
                                }
                            }
                        }
                    }
                }
            }

            return npcLevelLookup;
        }

        private Dictionary<uint, List<ENpc>> BuildDataMap() {
            var dataMap = new Dictionary<uint, List<ENpc>>();

            foreach (var npc in this) {
                if (npc.Base != null)
                {
                    foreach (var variable in npc.Base.ENpcData)
                    {
                        if (variable != 0)
                        {
                            if (!dataMap.TryGetValue(variable, out var l))
                                dataMap.Add(variable, l = new List<ENpc>());
                            l.Add(npc);
                        }
                    }
                }
            }

            return dataMap;
        }

        #endregion
    }
}