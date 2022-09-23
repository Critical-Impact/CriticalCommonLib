using System.Collections.Generic;
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
            { 2, 25199 },
            { 4, 25200 },
            { 6, 33913 },
            { 7, 33914 }
        };

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
            
            var itemOne = parser.ReadColumn<int>(2 + index);
            var countOne = parser.ReadColumn<uint>(62 + index);
            var hqOne = parser.ReadColumn<bool>(182 + index);
            if (itemOne != 0 && countOne != 0)
            {
                rewards.Add(new ShopListingItem(lumina, language, this, (uint)itemOne, (int)countOne, hqOne, 0));
            }

            
            var itemTwo = parser.ReadColumn<int>(242 + index);
            var countTwo = parser.ReadColumn<uint>(302 + index);
            var hqTwo = parser.ReadColumn<bool>(422 + index);
            if (itemTwo != 0 && countTwo != 0)
            {
                rewards.Add(new ShopListingItem(lumina, language, this, (uint)itemTwo, (int)countTwo, hqTwo, 0));
            }

            _Rewards = rewards.ToArray();
            Quest = new LazyRow<Quest>(lumina, parser.ReadColumn<uint>(1502), language);

            var costs = new List<ShopListingItem>();
            
            var itemThree = ConvertCurrencyId((uint)parser.ReadColumn<int>(482 + index), shop.UseCurrencyType);
            var countThree = parser.ReadColumn<uint>(542 + index);
            var hqThree = parser.ReadColumn<bool>(602 + index);
            var collectiabilityThree = parser.ReadColumn<ushort>(662 + index);
            if (itemThree != 0 && countThree != 0)
            {
                costs.Add(new ShopListingItem(lumina, language, this, itemThree, (int)countThree, hqThree, collectiabilityThree));
            }
            
            var itemFour = ConvertCurrencyId((uint)parser.ReadColumn<int>(722 + index), shop.UseCurrencyType);
            var countFour = parser.ReadColumn<uint>(782 + index);
            var hqFour = parser.ReadColumn<bool>(842 + index);
            var collectiabilityFour = parser.ReadColumn<ushort>(902 + index);
            if (itemFour != 0 && countFour != 0)
            {
                costs.Add(new ShopListingItem(lumina, language, this, itemFour, (int)countFour, hqFour, collectiabilityFour));
            }
            
            var itemFive = ConvertCurrencyId((uint)parser.ReadColumn<int>(962 + index), shop.UseCurrencyType);
            var countFive = parser.ReadColumn<uint>(1022 + index);
            var hqFive = parser.ReadColumn<bool>(1082 + index);
            var collectiabilityFive = parser.ReadColumn<ushort>(1142 + index);
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
                        if (Service.ExcelCache.TomestoneLookup.ContainsKey(itemId))
                        {
                            return Service.ExcelCache.TomestoneLookup[itemId];
                        }

                        break;
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

        #endregion
    }
}