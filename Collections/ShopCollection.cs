using System;
using System.Collections;
using System.Collections.Generic;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Sheets;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Collections
{
public class ShopCollection : IEnumerable<IShop> {

        #region Constructors

        #region Constructor

        public ShopCollection() {
            _itemLookup = new Dictionary<uint, List<IShop>>();
            CompileLookups();
        }

        #endregion

        #endregion

        public List<IShop> GetShops(uint itemId)
        {
            return _itemLookup.ContainsKey(itemId) ? _itemLookup[itemId] : new List<IShop>();
        }

        private readonly Dictionary<uint, List<IShop>> _itemLookup;
        private bool _lookupsCompiled;
        public void CompileLookups()
        {
            if (_lookupsCompiled)
            {
                return;
            }

            _lookupsCompiled = true;
            foreach (var shop in this)
            {
                foreach (var itemId in shop.ShopItemIds)
                {
                    if (!_itemLookup.ContainsKey(itemId))
                    {
                        _itemLookup.Add(itemId, new List<IShop>());
                    }
                    _itemLookup[itemId].Add(shop);
                }
            }
        }

        #region IEnumerable<IShop> Members

        public IEnumerator<IShop> GetEnumerator() {
            return new Enumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        #region Enumerator

        private class Enumerator : IEnumerator<IShop> {
            #region Fields

            // ReSharper disable once InconsistentNaming
            private readonly IEnumerator<GCShopEx> _GCShopEnumerator;
            private readonly IEnumerator<GilShopEx> _gilShopEnumerator;
            private readonly IEnumerator<SpecialShopEx> _specialShopEnumerator;
            private readonly IEnumerator<FccShop> _fccShopEnumerator;
            private int _state;

            #endregion

            #region Constructors

            #region Constructor

            public Enumerator() {
                _gilShopEnumerator = Service.ExcelCache.GetGilShopExSheet().GetEnumerator();
                _GCShopEnumerator = Service.ExcelCache.GetGCShopExSheet().GetEnumerator();
                _specialShopEnumerator = Service.ExcelCache.GetSpecialShopExSheet().GetEnumerator();
                //_FccShopEnumerator = Service.ExcelCache.GetSheet<FccShop>().GetEnumerator();
            }

            #endregion

            #endregion

            #region IEnumerator<Item> Members

            public IShop Current { get; private set; }

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
                    _gilShopEnumerator.Dispose();
                    _GCShopEnumerator.Dispose();
                    _specialShopEnumerator.Dispose();
                }
                _disposed = true;         
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current { get { return Current; } }

            public bool MoveNext() {
                var result = false;

                Current = null;
                if (_state == 0) {
                    result = _gilShopEnumerator.MoveNext();
                    if (result)
                        Current = _gilShopEnumerator.Current;
                    else
                        ++_state;
                }
                if (_state == 1) {
                    result = _GCShopEnumerator.MoveNext();
                    if (result)
                        Current = _GCShopEnumerator.Current;
                    else
                        ++_state;
                }
                if (_state == 2) {
                    result = _specialShopEnumerator.MoveNext();
                    if (result)
                        Current = _specialShopEnumerator.Current;
                    else
                        ++_state;
                }
/*
                if(_State == 3) {
                    result = _FccShopEnumerator.MoveNext();
                    if (result)
                        Current = _FccShopEnumerator.Current;
                    else
                        ++_State;
                }*/

                return result;
            }

            public void Reset() {
                _state = 0;
                _gilShopEnumerator.Reset();
                _GCShopEnumerator.Dispose();
                _specialShopEnumerator.Dispose();
            }

            #endregion
        }

        #endregion
    }
}