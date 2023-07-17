using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class FccShopEx : FccShop, IShop, IItemSource
    {
        private ENpc[]? _eNpcs;
        private IEnumerable<LazyRow<ItemEx>> _shopItems = null!;
        private IEnumerable<LazyRow<ItemEx>> _costItems = null!;
        private HashSet<uint> _shopItemIds = null!;
        private IEnumerable<IShopListing> _shopListings = null!;

        string IShop.Name  => ToString();
        public IEnumerable<ENpc> ENpcs { get { return _eNpcs ??= BuildENpcs(); } }
        public IEnumerable<IShopListing> ShopListings => _shopListings;
        public IEnumerable<LazyRow<ItemEx>> Items => _shopItems;
        public IEnumerable<LazyRow<ItemEx>> CostItems => _costItems;
        public HashSet<uint> ShopItemIds => _shopItemIds;
        
        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);

            _shopItems = Item.Where(c => c != 0)
                .Select(c => new LazyRow<ItemEx>(gameData, c,  language));

            _shopItemIds = Item.Where(c => c != 0).Distinct().ToHashSet();
            _costItems = Cost.Where(c => c != 0)
                .Select(c => new LazyRow<ItemEx>(gameData, c,  language));
            _shopListings = BuildShopListings(gameData, language);
        }
        
        public string? _name = null;

        public override string ToString() {
            if (_name == null)
            {
                var shopName = Service.ExcelCache.GetShopName(RowId);
                _name = shopName != null ? shopName.Name : Name.ToString();
            }

            if (_name == "")
            {
                _name = "Unknown Vendor";
            }
            return _name;
        }
        
        
        private IShopListing[] BuildShopListings(GameData gameData, Language language) {
            const uint costItem = ItemEx.FreeCompanyCreditItemId; 

            var listings = new List<IShopListing>();
            for (var index = 0; index < Item.Length; index++)
            {
                var item = Item[index];
                if (item == 0)
                    continue;
                var cost = Cost[index];
                var requiredRank = FCRankRequired[index];

                listings.Add(new Listing(gameData,language, this, item, costItem, (int)cost, requiredRank));
            }
            return listings.ToArray();
        }
        
        private ENpc[] BuildENpcs() {
            return Service.ExcelCache.ENpcCollection.FindWithData(RowId).ToArray();
        }
        
        public class Listing : IShopListing {
            #region Fields
            IShopListingItem _Cost;
            IShopListingItem _Reward;
            IShop _Shop;
            #endregion

            public Listing(GameData gamedata, Language language, FccShopEx shop, uint rewardItem, uint costItem, int costCount, int requiredFcRank) {
                _Shop = shop;
                _Cost = new ShopListingItem(gamedata, language, this, costItem, costCount, false, 0);
                _Reward = new ShopListingItem(gamedata, language, this, rewardItem, 1, false, 0);
            }

            public IEnumerable<IShopListingItem> Costs {
                get {
                    yield return _Cost;
                }
            }

            public IEnumerable<IShopListingItem> Rewards {
                get {
                    yield return _Reward;
                }
            }

            public IEnumerable<IShop> Shops {
                get {
                    yield return _Shop;
                }
            }
        }
    }
}