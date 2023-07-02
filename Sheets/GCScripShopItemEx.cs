using System.Collections.Generic;
using CriticalCommonLib.Interfaces;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class GCScripShopItemEx : GCScripShopItem, IShopListing, IShopListingItem
    {
        public LazyRow<GCShopEx> GCShopEx { get; private set; } = null!;
        public GCScripShopCategory GCScripShopCategory { get; private set; } = null!;
        public ShopListingItem Cost { get; private set; } = null!;
        
        private const int SealItemOffset = 19;//Like they are ever changing this

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            if (RowId != 0)
            {
                var lookup = Service.ExcelCache.GcScripShopCategoryGrandCompany;
                var grandCompanyId = lookup[RowId];
                var shopLookup = Service.ExcelCache.GcShopGrandCompany;
                GCShopEx = new LazyRow<GCShopEx>(gameData, shopLookup[grandCompanyId], language);
                ItemEx = new LazyRow<ItemEx>(gameData, Item.Row, language);
                Cost = new ShopListingItem(gameData, language, this, SealItemOffset, (int)CostGCSeals, false, 0);
            }
        }

        public IEnumerable<IShopListingItem> Rewards  { get { yield return this; } }
        public IEnumerable<IShopListingItem> Costs { get { yield return Cost; } }
        public IEnumerable<IShop> Shops { get { yield return GCShopEx.Value!; } }
        public LazyRow<ItemEx> ItemEx { get; private set; } = null!;
        public int Count => 1;
        public bool IsHq => false;
        public int CollectabilityRating => 0;
        public IShopListing ShopItem => this;
    }
}