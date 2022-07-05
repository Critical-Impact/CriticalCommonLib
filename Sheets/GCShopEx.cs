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
    public class GCShopEx : GCShop, IShop, ILocatable, IItemSource
    {
        private ENpc[]? _eNpcs;
        private IEnumerable<LazySubRow<GCScripShopItemEx>> _items = null!;
        private HashSet<uint> _shopItemsIds = null!;
        private IEnumerable<LazyRow<ItemEx>> _shopItems = null!;

        public string Name => ToString();
        public IEnumerable<ENpc> ENpcs { get { return _eNpcs ??= BuildENpcs(); } }
        public IEnumerable<IShopListing> ShopListings => ShopItems.Where(c => c.Value != null).Select(c => c.Value!);
        public IEnumerable<LazySubRow<GCScripShopItemEx>> ShopItems => _items;
        public HashSet<uint> ShopItemIds => _shopItemsIds;
        public IEnumerable<LazyRow<ItemEx>> Items => _shopItems;
        public IEnumerable<ILocation> Locations { get { return ENpcs.SelectMany(_ => _.Locations); } }


        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);

            var itemIds = Service.ExcelCache.GcScripShopItemToGcScripCategories;
            //Get the grand company categories that relate to our shop
            var grandCompanyIds = Service.ExcelCache.GcScripShopCategoryGrandCompany.Where(c => c.Value == GrandCompany.Row).Select(c => c.Key).Distinct().ToHashSet();
            var allItems = itemIds.Where(c => grandCompanyIds.Contains(c.Key))
                .SelectMany(c => c.Value.Select(subRowId => (c.Key, subRowId))).ToList();
            var gcScripItems = Service.ExcelCache.GcScripShopToItem;
            
            _items = allItems
                .Select(c => new LazySubRow<GCScripShopItemEx>(gameData, c.Key, c.subRowId, language));
            _shopItems = allItems.Where(c => gcScripItems.ContainsKey((c.Key, c.subRowId)))
                .Select(c => new LazyRow<ItemEx>(gameData, gcScripItems[(c.Key, c.subRowId)],  language));
            _shopItemsIds = allItems.Where(c => gcScripItems.ContainsKey((c.Key, c.subRowId)))
                .Select(c => gcScripItems[(c.Key, c.subRowId)]).Distinct().ToHashSet();
        }

        private ENpc[] BuildENpcs() {
            return Service.ExcelCache.ENpcCollection.FindWithData(RowId).ToArray();
        }
        
        public override string ToString() {
            return GrandCompany.Value?.Name.ToString() ?? "Unknown";
        }

    }
}