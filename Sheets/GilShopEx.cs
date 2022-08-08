using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Services;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class GilShopEx : GilShop, IShop, IItemSource
    {
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            var gilShopItemLookup = Service.ExcelCache.GilShopItemLookup;
            var gilShopGilShopItemLookup = Service.ExcelCache.GilShopGilShopItemLookup;
            if (gilShopGilShopItemLookup.ContainsKey(RowId))
            {
                _shopItems = gilShopGilShopItemLookup[RowId]
                    .Select(c => new LazySubRow<GilShopItemEx>(gameData, RowId, c, language));
            }
            else
            {
                _shopItems = new List<LazySubRow<GilShopItemEx>>();
            }

            if (gilShopItemLookup.ContainsKey(RowId))
            {
                _items = gilShopItemLookup[RowId]
                    .Select(c => new LazyRow<ItemEx>(gameData, c, language));
                _shopItemIds = gilShopItemLookup[RowId]
                    .Select(c => c).Distinct().ToHashSet();
            }
            else
            {
                _items = new List<LazyRow<ItemEx>>();
                _shopItemIds = new HashSet<uint>();
            }

        }

        private ENpc[]? _eNpcs;
        private IEnumerable<LazySubRow<GilShopItemEx>> _shopItems = null!;
        private IEnumerable<LazyRow<ItemEx>> _items = null!;
        private HashSet<uint> _shopItemIds = null!;

        public IEnumerable<LazyRow<ItemEx>> Items { get { return _items; } }
        public IEnumerable<LazyRow<ItemEx>> CostItems { get; } = new List<LazyRow<ItemEx>>();
        public IEnumerable<ENpc> ENpcs { get { return _eNpcs ??= BuildENpcs(); } }
        IEnumerable<IShopListing> IShop.ShopListings { get { return _shopItems.Select(c => c.Value).Where(c => c != null).Select(c => c!); } }

        string IShop.Name {
            get { return Name; }
        }
        
        public override string ToString() {
            return Name;
        }
        
        private ENpc[] BuildENpcs() {
            return Service.ExcelCache.ENpcCollection.FindWithData(RowId).ToArray();
        }

        public IEnumerable<LazyRow<ItemEx>> ShopItems => _items;
        public HashSet<uint> ShopItemIds => _shopItemIds;
    }
}