using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Sheets;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Collections
{
public class ENpcCollection : IEnumerable<ENpc> {
        #region Fields

        private readonly Dictionary<uint, ENpc> _inner = new Dictionary<uint, ENpc>();
        private Dictionary<uint, HashSet<ENpc>>? _eNpcDataMap;
        private Dictionary<ENpc, List<uint>>? _eNpcShopMap;
        private Dictionary<uint, HashSet<NpcLocation>>? _eNpcLevelMap;

        #endregion

        #region Properties

        public ExcelSheet<ENpcBaseEx> BaseSheet { get; private set; }
        public ExcelSheet<ENpcResidentEx> ResidentSheet { get; private set; }

        #endregion

        #region Constructors

        public ENpcCollection() {
            BaseSheet = Service.ExcelCache.GetENpcBaseExSheet();
            ResidentSheet = Service.ExcelCache.GetENpcResidentExSheet();
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

            public ENpc Current { get; private set; } = null!;

            #endregion

            #region IDisposable Members

            private bool _disposed;
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        
            private void Dispose(bool disposing)
            {
                if(!_disposed && disposing)
                {
                    _BaseEnumerator.Dispose();
                }
                _disposed = true;         
            }
            
            ~Enumerator()
            {
                    #if DEBUG
                // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
                // disposed by the programmer.

                if( _disposed == false )
                {
                    Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
                }
                    #endif
                Dispose (true);
            }

            #endregion

            #region IEnumerator Members

            object? IEnumerator.Current { get { return Current; } }

            public bool MoveNext() {
                var result = _BaseEnumerator.MoveNext();
                Current = result ? _Collection.Get(_BaseEnumerator.Current.RowId) : null!;
                return result;
            }

            public void Reset() {
                Current = null!;
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

        public List<uint>? FindShops(ENpc npc)
        {
            if (_eNpcShopMap == null)
            {
                _eNpcShopMap = BuildShopMap();
            }

            if (_eNpcShopMap.ContainsKey(npc))
            {
                return _eNpcShopMap[npc];
            }

            return null;
        }

        private Dictionary<ENpc, List<uint>> BuildShopMap()
        {
            if (_eNpcDataMap == null)
            {
                _eNpcDataMap = BuildDataMap();
            }
            var shopIds = Service.ExcelCache.ShopCollection?.Select(c => c.RowId).Distinct().ToHashSet();

            var shopMap = new Dictionary<ENpc, List<uint>>();
            if (shopIds != null)
            {
                foreach (var dataMap in _eNpcDataMap)
                {
                    if (shopIds.Contains(dataMap.Key))
                    {
                        foreach (var npc in dataMap.Value)
                        {
                            shopMap.TryAdd(npc, new List<uint>());
                            shopMap[npc].Add(dataMap.Key);
                        }
                    }
                }
            }

            return shopMap;
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
            var sTerritoryTypes = Service.ExcelCache.GetTerritoryTypeExSheet();
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

                for (var index = 0u; index < sLgbFile.Layers.Length; index++)
                {
                    var sLgbGroup = sLgbFile.Layers[index];
                    var map = sTerritoryType.GetMapAtLayerIndex(index + 1);
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
                                    npcLevelLookup.Add(npcRowId, new());
                                }
                                if (map.Row == 0) continue;
                                var npcLocation = new NpcLocation(instanceObject.Transform.Translation.X,
                                    instanceObject.Transform.Translation.Z, map.Row != 0 && map.Value != null ? map : sTerritoryType.MapEx,
                                    sTerritoryType.PlaceNameEx, new LazyRow<TerritoryTypeEx>(Service.ExcelCache.GameData, sTerritoryType.RowId, Service.ExcelCache.Language));
                                npcLevelLookup[npcRowId].Add(npcLocation);
                            }
                        }
                    }
                }
            }

            foreach (var level in Service.ExcelCache.GetLevelExSheet()
                         .Where(c => c.Object > 1000000 && c.Object < 11000000))
            {
                var npcLocation = new NpcLocation(level.X,level.Z, level.MapEx,
                    level.PlaceNameEx, new LazyRow<TerritoryTypeEx>(Service.ExcelCache.GameData, level.TerritoryTypeEx.Row, Service.ExcelCache.Language));
                
                npcLevelLookup.TryAdd(level.Object, new HashSet<NpcLocation>());
                if (!npcLevelLookup[level.Object].Any(c => c.EqualRounded(npcLocation)))
                {
                    npcLevelLookup[level.Object].Add(npcLocation);
                }
            }

            if (Service.ExcelCache.ENpcPlaces != null)
            {
                foreach (var npc in Service.ExcelCache.ENpcPlaces)
                {
                    if (!npcLevelLookup.ContainsKey(npc.ENpcResidentId))
                    {
                        npcLevelLookup.Add(npc.ENpcResidentId, new());
                    }

                    if (npc.TerritoryTypeEx.Value != null)
                    {
                        var npcLocation = new NpcLocation(npc.Position.X, npc.Position.Y,
                            npc.TerritoryTypeEx.Value.MapEx,
                            npc.TerritoryTypeEx.Value.PlaceNameEx, npc.TerritoryTypeEx, true);
                        if (!npcLevelLookup[npc.ENpcResidentId].Any(c => c.EqualRounded(npcLocation)))
                        {
                            npcLevelLookup[npc.ENpcResidentId].Add(npcLocation);
                        }
                    }
                }
            }

            return npcLevelLookup;
        }

        private Dictionary<uint, HashSet<ENpc>> BuildDataMap() {
            var dataMap = new Dictionary<uint, HashSet<ENpc>>();

            foreach (var npc in this) {
                if (npc.Base != null)
                {
                    if (Service.ExcelCache.GetFateShopSheet().HasRow(npc.Key))
                    {
                        var fateShop = Service.ExcelCache.GetFateShopSheet().GetRow(npc.Key);
                        if (fateShop != null)
                        {
                            var specialShops = fateShop.SpecialShop.Where(c => c.Row != 0).ToList();
                            foreach (var specialShop in specialShops)
                            {
                                if (!dataMap.TryGetValue(specialShop.Row, out var l3))
                                    dataMap.Add(specialShop.Row, l3 = new HashSet<ENpc>());
                                l3.Add(npc);
                            }
                        }
                    }
                    foreach (var variable in npc.Base.ENpcData)
                    {
                        BuildDataMapLoop(variable, dataMap, npc);
                    }

                    AddFixedData(dataMap, npc);
                }
            }

            return dataMap;
        }

        private static readonly Dictionary<uint, uint> _shbFateShopNpc = new()
        {
            { 1027998, 1769957 },
            { 1027538, 1769958 },
            { 1027385, 1769959 },
            { 1027497, 1769960 },
            { 1027892, 1769961 },
            { 1027665, 1769962 },
            { 1027709, 1769963 },
            { 1027766, 1769964 },
        };
        
        private static void AddFixedData(Dictionary<uint, HashSet<ENpc>> dataMap, ENpc npc)
        {
            var npcId = npc.Key;
            if (npcId == 1018655)
            {
                AddNpc(dataMap, 1769743, npc);
                AddNpc(dataMap, 1769744, npc);
                AddNpc(dataMap, 1770537, npc);
            }
            else if (npcId == 1016289)
            {
                AddNpc(dataMap, 1769635, npc);
            }
            else if (npcId == 1025047)
            {
                for (uint i = 1769820; i <= 1769834; i++)
                {
                    AddNpc(dataMap, i, npc);
                }
            }
            else if (npcId == 1025763)
            {
                AddNpc(dataMap, 262919, npc);
            }
            else if (npcId == 1027123)
            {
                AddNpc(dataMap, 1769934, npc);
                AddNpc(dataMap, 1769935, npc);
            }
            else if (npcId == 1033921)
            {
                AddNpc(dataMap, 1770282, npc);
            }
            else if (npcId == 1036895 || npcId == 1034007)
            {
                AddNpc(dataMap, 1770087, npc);
            }

            if (npcId >= 1006004u && npcId <= 1006006)
            {
                for (uint j = 1769898u; j <= 1769906; j++)
                {
                    AddNpc(dataMap, j, npc);
                }
            }
            if (_shbFateShopNpc.TryGetValue(npcId, out uint value))
            {
                AddNpc(dataMap, value, npc);
            }
        }

        private static void AddNpc(Dictionary<uint, HashSet<ENpc>> dataMap, uint shopId, ENpc npc)
        {
            if (!dataMap.TryGetValue(shopId, out var l))
                dataMap.Add(shopId, l = new HashSet<ENpc>());
            l.Add(npc);
        }

        private static void BuildDataMapLoop(uint actualVariable, Dictionary<uint, HashSet<ENpc>> dataMap, ENpc npc)
        {
            if (actualVariable != 0)
            {
                if (actualVariable >= 3538944 && 3539068 >= actualVariable)
                {
                    var prehandler = Service.ExcelCache.GetPreHandlerSheet().GetRow(actualVariable);
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
                    var topicSelect = Service.ExcelCache.GetTopicSelectSheet().GetRow(actualVariable);
                    if (topicSelect != null)
                    {
                        foreach (var topicSelectItem in topicSelect.Shop)
                        {
                            if (topicSelectItem != 0)
                            {
                                BuildDataMapLoop(topicSelectItem, dataMap, npc);
                                
                            }
                        }
                        return;
                    }
                }
                if (actualVariable >= 720896 && actualVariable <= 721681)
                {
                    var customTalk = Service.ExcelCache.GetCustomTalkSheet().GetRow(actualVariable);
                    if (customTalk != null)
                    {
                        foreach (var arg in customTalk.ScriptArg)
                        {
                            if (arg >= 1769472 && arg <= 1770600)
                            {
                                if (!dataMap.TryGetValue(arg, out var l))
                                    dataMap.Add(arg, l = new HashSet<ENpc>());
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
                            dataMap.Add(actualShop, l2 = new HashSet<ENpc>());
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
                                    dataMap.Add(actualShop, l3 = new HashSet<ENpc>());
                                l3.Add(npc);
                            }
                        }

                        if (Service.ExcelCache.InclusionShopCategoryToShopSeriesLookup.ContainsKey(category))
                        {
                            var shops = Service.ExcelCache.InclusionShopCategoryToShopSeriesLookup[category];
                            foreach (var actualShop in shops)
                            {
                                if (!dataMap.TryGetValue(actualShop, out var l3))
                                    dataMap.Add(actualShop, l3 = new HashSet<ENpc>());
                                l3.Add(npc);
                            }
                        }
                    }
                }

                if (!dataMap.TryGetValue(actualVariable, out var l4))
                    dataMap.Add(actualVariable, l4 = new HashSet<ENpc>());
                l4.Add(npc);

                var eNpcShops = Service.ExcelCache.GetENpcShops(npc.Key);
                if (eNpcShops != null)
                {
                    foreach (var eNpcShop in eNpcShops)
                    {
                        if (!dataMap.TryGetValue(eNpcShop.ShopId, out var l5))
                        {
                            dataMap.Add(eNpcShop.ShopId, l5 = new HashSet<ENpc>());
                        }
                        l5.Add(npc);
                    }
                }
            }
        }

        #endregion
    }
}