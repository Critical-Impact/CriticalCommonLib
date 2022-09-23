using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class SpecialShopEx : SpecialShop, IShop, IItemSource
    {
        private IEnumerable<IShopListing> _shopListings = null!;
        private ENpc[]? _eNpcs = null!;
        private IEnumerable<LazyRow<ItemEx>> _items = null!;
        private IEnumerable<LazyRow<ItemEx>> _costItems = null!;
        private HashSet<uint> _shopItemIds = null!;

        public override void PopulateData(RowParser parser, Lumina.GameData lumina, Language language) {
            base.PopulateData(parser, lumina, language);
            _shopListings = BuildShopItems(parser, lumina, language);
        }

        string IShop.Name => Name.ToString();
        public IEnumerable<ENpc> ENpcs { get { return _eNpcs ??= BuildENpcs(); } }

        public IEnumerable<IShopListing> ShopListings => _shopListings;

        public IEnumerable<LazyRow<ItemEx>> Items => _items;
        public IEnumerable<LazyRow<ItemEx>> CostItems => _costItems;
        public HashSet<uint> ShopItemIds => _shopItemIds;
        
        //Need to hardcode scrip
        private static Dictionary<int, int> _currencies = new Dictionary<int, int>() {
            { 1, 28 },
            { 2, 25199 },
            { 4, 25200 },
            { 6, 33913 },
            { 7, 33914 }
        };
        
        private SpecialShopListing[] BuildShopItems(RowParser parser, Lumina.GameData lumina, Language language) {
            const int Count = 60;

            var shopListings = new List<SpecialShopListing>();
            var resultItems = new List<LazyRow<ItemEx>>();
            var costItems = new List<LazyRow<ItemEx>>();
            var shopItemIds = new HashSet<uint>();
            for (var i = 0; i < Count; ++i) {
                var item = new SpecialShopListing(parser, lumina, language, this, i);
                if (item.Rewards.Any())
                {
                    shopListings.Add(item);
                    foreach (var listing in item.Rewards)
                    {
                        resultItems.Add(listing.ItemEx);
                        shopItemIds.Add(listing.ItemEx.Row);
                    }
                }

                foreach (var listing in item.Costs)
                {
                    costItems.Add(listing.ItemEx);
                }
            }

            _costItems = costItems;
            _items = resultItems;
            _shopItemIds = shopItemIds;
            return shopListings.ToArray();
        }
        private ENpc[] BuildENpcs() {
            return Service.ExcelCache.ENpcCollection.FindWithData(RowId).ToArray();
        }
    }

}