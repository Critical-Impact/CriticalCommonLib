using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Services;
using CriticalCommonLib.Sheets;
using Lumina.Data.Files;
using Lumina.Data.Parsing;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Collections
{
public class ENpcCollection : IEnumerable<ENpc> {
        #region Fields

        private readonly Dictionary<uint, ENpc> _inner = new Dictionary<uint, ENpc>();
        private Dictionary<uint, List<ENpc>>? _eNpcDataMap;
        private Dictionary<uint, HashSet<NpcLocation>>? _eNpcLevelMap;

        #endregion

        #region Properties

        public ExcelSheet<ENpcBase> BaseSheet { get; private set; }
        public ExcelSheet<ENpcResident> ResidentSheet { get; private set; }

        #endregion

        #region Constructors

        public ENpcCollection() {
            BaseSheet = Service.ExcelCache.GetSheet<ENpcBase>();
            ResidentSheet = Service.ExcelCache.GetSheet<ENpcResident>();
            _eNpcLevelMap = BuildLevelMap();
            _eNpcDataMap = BuildDataMap();
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
            if (_inner.ContainsKey(key))
                return _inner[key];

            var enpc = new ENpc(this, key);
            _inner.Add(key, enpc);
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
            if (_eNpcDataMap == null)
                _eNpcDataMap = BuildDataMap();
            if (_eNpcDataMap.ContainsKey(value))
                return _eNpcDataMap[value];
            return Array.Empty<ENpc>();
        }

        public HashSet<NpcLocation> FindLevels(uint npcId) {
            if (_eNpcLevelMap == null)
            {
                _eNpcLevelMap = BuildLevelMap();
            }

            if (_eNpcLevelMap.ContainsKey(npcId))
            {
                return _eNpcLevelMap[npcId];
            }
            return new HashSet<NpcLocation>();
        }

        private Dictionary<uint,HashSet<NpcLocation>> BuildLevelMap()
        {
            var sTerritoryTypes = Service.ExcelCache.GetSheet<TerritoryTypeEx>();
            Dictionary<uint, HashSet<NpcLocation>> npcLevelLookup = new Dictionary<uint, HashSet<NpcLocation>>();
            
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
                            if (npcRowId != 0)
                            {
                                if (!npcLevelLookup.ContainsKey(npcRowId))
                                {
                                    npcLevelLookup.Add(npcRowId, new ());
                                }
                                var npcLocation = new NpcLocation(instanceObject.Transform.Translation.X, instanceObject.Transform.Translation.Z, sTerritoryType.MapEx, sTerritoryType.PlaceName);
                                if(!npcLevelLookup[npcRowId].Contains(npcLocation))
                                {
                                    npcLevelLookup[npcRowId].Add(npcLocation);
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
                        BuildDataMapLoop(variable, dataMap, npc);
                    }
                }
            }

            return dataMap;
        }

        private static void BuildDataMapLoop(uint actualVariable, Dictionary<uint, List<ENpc>> dataMap, ENpc npc)
        {
            if (actualVariable != 0)
            {
                if (actualVariable == 3539062)
                {
                    var a = "";
                }
                if (actualVariable >= 3538944 && 3539068 >= actualVariable)
                {
                    var prehandler = Service.ExcelCache.GetSheet<PreHandler>().GetRow(actualVariable);
                    if (prehandler != null)
                    {
                        if (prehandler.Target != 0)
                        {
                            BuildDataMapLoop(prehandler.Target, dataMap, npc);
                            return;
                        }
                    }
                }

                if (actualVariable >= 3276800 && actualVariable <= 3276899)
                {
                    var topicSelect = Service.ExcelCache.GetSheet<TopicSelect>().GetRow(actualVariable);
                    if (topicSelect != null)
                    {
                        foreach (var topicSelectItem in topicSelect.UnkData4)
                        {
                            if (topicSelectItem.Shop != 0)
                            {
                                BuildDataMapLoop(topicSelectItem.Shop, dataMap, npc);
                                
                            }
                        }
                        return;
                    }
                }
                if (actualVariable >= 720896 && actualVariable <= 721681)
                {
                    var customTalk = Service.ExcelCache.GetSheet<CustomTalk>().GetRow(actualVariable);
                    if (customTalk != null)
                    {
                        foreach (var arg in customTalk.ScriptArg)
                        {
                            if (arg >= 1769472 && arg <= 1770600)
                            {
                                if (!dataMap.TryGetValue(arg, out var l))
                                    dataMap.Add(arg, l = new List<ENpc>());
                                l.Add(npc);
                            }
                        }
                    }
                }

                //Lookup the item in topic select lookup and add all of those(nested shops, why square, pick a lane honestly)
                if (Service.ExcelCache.ShopToShopCollectionLookup.ContainsKey(actualVariable))
                {
                    var lookup = Service.ExcelCache.ShopToShopCollectionLookup[actualVariable];
                    foreach (var actualShop in lookup)
                    {
                        if (!dataMap.TryGetValue(actualShop, out var l2))
                            dataMap.Add(actualShop, l2 = new List<ENpc>());
                        l2.Add(npc);
                    }
                }

                if (Service.ExcelCache.InclusionShopToCategoriesLookup.ContainsKey(actualVariable))
                {
                    var categories = Service.ExcelCache.InclusionShopToCategoriesLookup[actualVariable];
                    foreach (var category in categories)
                    {
                        if (Service.ExcelCache.InclusionShopCategoryToShopLookup.ContainsKey(category))
                        {
                            var shops = Service.ExcelCache.InclusionShopCategoryToShopLookup[category];
                            foreach (var actualShop in shops)
                            {
                                if (!dataMap.TryGetValue(actualShop, out var l3))
                                    dataMap.Add(actualShop, l3 = new List<ENpc>());
                                l3.Add(npc);
                            }
                        }

                        if (Service.ExcelCache.InclusionShopCategoryToShopSeriesLookup.ContainsKey(category))
                        {
                            var shops = Service.ExcelCache.InclusionShopCategoryToShopSeriesLookup[category];
                            foreach (var actualShop in shops)
                            {
                                if (!dataMap.TryGetValue(actualShop, out var l3))
                                    dataMap.Add(actualShop, l3 = new List<ENpc>());
                                l3.Add(npc);
                            }
                        }
                    }
                }

                if (!dataMap.TryGetValue(actualVariable, out var l4))
                    dataMap.Add(actualVariable, l4 = new List<ENpc>());
                l4.Add(npc);
            }
        }

        #endregion
    }
}