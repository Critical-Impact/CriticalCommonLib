using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Interfaces;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class SpecialShopListing : IShopListing {
        #region Fields

        /// <summary>
        ///     Costs of the current listing.
        /// </summary>
        private readonly ShopListingItem[] _Costs;

        /// <summary>
        ///     Rewards of the current listing.
        /// </summary>
        private readonly ShopListingItem[] _Rewards;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the <see cref="SpecialShop" /> the current listing is from.
        /// </summary>
        /// <value>The <see cref="SpecialShop" /> the current listing is from.</value>
        public SpecialShopEx SpecialShop { get; private set; }

        /// <summary>
        ///     Gets the <see cref="Quest" /> required for the current listing.
        /// </summary>
        /// <value>The <see cref="Quest" /> required for the current listing.</value>
        public LazyRow<Quest> Quest { get; private set; }
        
        //Need to hardcode scrip
        private static Dictionary<uint, uint> _currencies = new Dictionary<uint, uint>() {
            { 1, 28 },
            { 2, 33913 },
            { 3, 45 },
            { 4, 33914 },
            { 6, 41784 },
            { 7, 41785 }
        };

        //No fucking idea why these 2 are special, make a PR if you know how square managed to make this system even stupider
        private static HashSet<uint> _currencyShops = new HashSet<uint>()
        {
            1770637,
            1770638,
            1770699
        };

        private static Dictionary<uint, uint>? _tomeStones;

        private static Dictionary<uint, uint> BuildTomestones() {
            // Tomestone currencies rotate across patches.
            // These keys correspond to currencies A, B, and C.
            var sTomestonesItems = Service.ExcelCache.GetSheet<TomestonesItem>()
                .Where(t => t.Tomestones.Row > 0)
                .OrderBy(t => t.Tomestones.Row)
                .ToArray();

            var tomeStones = new Dictionary<uint, uint>();

            for (uint i = 0; i < sTomestonesItems.Length; i++) {
                tomeStones[i + 1] = (uint)sTomestonesItems[i].Item.Row;
            }

            return tomeStones;
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpecialShopListing" /> class.
        /// </summary>
        /// <param name="shop"><see cref="SpecialShop" /> for which the listing is.</param>
        /// <param name="index">Position of the listing in the <c>shop</c>'s data.</param>
        public SpecialShopListing(RowParser parser, Lumina.GameData lumina, Language language,SpecialShopEx shop, int index) {
            SpecialShop = shop;
        
            var rewards = new List<ShopListingItem>();

            ushort shopUseCurrencyType = (ushort)(_currencyShops.Contains(SpecialShop.RowId) ? 16 : shop.UseCurrencyType);
            var itemOne = ConvertCurrencyId((uint)parser.ReadColumn<int>(1 + index), shopUseCurrencyType);
            var countOne = parser.ReadColumn<uint>(61 + index);
            var hqOne = parser.ReadColumn<bool>(181 + index);
            if (itemOne != 0 && countOne != 0)
            {
                rewards.Add(new ShopListingItem(lumina, language, this, (uint)itemOne, (int)countOne, hqOne, 0));
            }

            
            var itemTwo = ConvertCurrencyId((uint)parser.ReadColumn<int>(241 + index), shopUseCurrencyType);
            var countTwo = parser.ReadColumn<uint>(301 + index);
            var hqTwo = parser.ReadColumn<bool>(421 + index);
            if (itemTwo != 0 && countTwo != 0)
            {
                rewards.Add(new ShopListingItem(lumina, language, this, (uint)itemTwo, (int)countTwo, hqTwo, 0));
            }

            _Rewards = rewards.ToArray();
            Quest = new LazyRow<Quest>(lumina, parser.ReadColumn<uint>(1501), language);

            var costs = new List<ShopListingItem>();
            
            var itemThree = ConvertCurrencyId((uint)parser.ReadColumn<int>(481 + index), shopUseCurrencyType);
            var countThree = parser.ReadColumn<uint>(541 + index);
            var hqThree = parser.ReadColumn<bool>(601 + index);
            var collectiabilityThree = parser.ReadColumn<ushort>(661 + index);
            if (itemThree != 0 && countThree != 0)
            {
                costs.Add(new ShopListingItem(lumina, language, this, itemThree, (int)countThree, hqThree, collectiabilityThree));
            }
            
            var itemFour = ConvertCurrencyId((uint)parser.ReadColumn<int>(721 + index), shopUseCurrencyType);
            var countFour = parser.ReadColumn<uint>(781 + index);
            var hqFour = parser.ReadColumn<bool>(841 + index);
            var collectiabilityFour = parser.ReadColumn<ushort>(901 + index);
            if (itemFour != 0 && countFour != 0)
            {
                costs.Add(new ShopListingItem(lumina, language, this, itemFour, (int)countFour, hqFour, collectiabilityFour));
            }
            
            var itemFive = ConvertCurrencyId((uint)parser.ReadColumn<int>(961 + index), shopUseCurrencyType);
            var countFive = parser.ReadColumn<uint>(1021 + index);
            var hqFive = parser.ReadColumn<bool>(1081 + index);
            var collectiabilityFive = parser.ReadColumn<ushort>(1141 + index);
            if (itemFive != 0 && countFive != 0)
            {
                costs.Add(new ShopListingItem(lumina, language, this, itemFive, (int)countFive, hqFive, collectiabilityFive));
            }

            _Costs = costs.ToArray();
        }

        public uint ConvertCurrencyId(uint itemId, ushort useCurrencyType)
        {
            if (itemId < 8 && itemId != 0)
            {
                switch (useCurrencyType)
                {
                    case 16:
                        return _currencies[itemId];
                    case 8:
                        return 1;
                    case 4:
                        if (TomeStones.ContainsKey(itemId))
                        {
                            return TomeStones[itemId];
                        }

                        return itemId;
                }
            }

            return itemId;

        }

        #endregion

        /// <summary>
        ///     Gets the rewards of the current listing.
        /// </summary>
        /// <value>The rewards of the current listing.</value>
        public IEnumerable<IShopListingItem> Rewards { get { return _Rewards; } }

        /// <summary>
        ///     Gets the costs of the current listing.
        /// </summary>
        /// <value>The costs of the current listing.</value>
        public IEnumerable<IShopListingItem> Costs { get { return _Costs; } }

        #region IShopItem Members

        /// <summary>
        ///     Gets the shops offering the current listing.
        /// </summary>
        /// <value>The shops offering the current listing.</value>
        IEnumerable<IShop> IShopListing.Shops { get { yield return SpecialShop; } }

        public static Dictionary<uint, uint> TomeStones
        {
            get
            {
                _tomeStones = BuildTomestones();
                return _tomeStones;
            }
        }

        #endregion
    }
}