using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class GilShopItemEx: GilShopItem, IShopListing, IShopListingItem 
    {
        private LazyRow<ItemEx> _itemEx = null!;
        private ShopListingItem _Cost = null!;
        private GilShopEx[]? _Shops;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            _itemEx = new LazyRow<ItemEx>(gameData, Item.Row, language);
            _Cost = new ShopListingItem(gameData, language, this, GilItemKey,
                (int)(ItemEx.Value?.PriceLow ?? 0), false, 0);
        }

        /// <summary>
        ///     Key of the <see cref="Item" /> used as currency (Gil).
        /// </summary>
        private const int GilItemKey = 1;
        
        
        IEnumerable<IShopListingItem> IShopListing.Rewards { get { yield return this; } }
        IEnumerable<IShopListingItem> IShopListing.Costs { get { yield return _Cost; } }
        public IEnumerable<IShop> Shops { get { return _Shops ??= BuildShops(); } }
        
        private GilShopEx[] BuildShops() {
            var sSheet = Service.ExcelCache.GetGilShopExSheet();
            return sSheet.Where(shop => shop.Items.Any(c => c.Row == _itemEx.Row)).ToArray();
        }
        
        public LazyRow<ItemEx> ItemEx
        {
            get
            {
                return _itemEx;
            }
        }
        LazyRow<ItemEx> IShopListingItem.ItemEx { get { return _itemEx; } }

        public int Count { get; }
        public bool IsHq { get; }
        public int CollectabilityRating { get; }
        public IShopListing ShopItem { get; } = null!;
    }
}