using System.Text;
using CriticalCommonLib.Interfaces;
using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace CriticalCommonLib.Sheets
{
public class ShopListingItem : IShopListingItem {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ShopListingItem" /> class.
        /// </summary>
        /// <param name="shopItem">The <see cref="IShopListing" /> the entry is for.</param>
        /// <param name="itemId">The item of the entry.</param>
        /// <param name="count">The count for the entry.</param>
        /// <param name="isHq">A value indicating whether the <c>item</c> is high-quality.</param>
        /// <param name="collectabilityRating">The collectability rating of the entry.</param>
        public ShopListingItem(GameData gameData, Language language, IShopListing shopItem, uint itemId, int count, bool isHq, int collectabilityRating) {
            ItemEx = new LazyRow<ItemEx>(gameData,itemId, language);
            Count = count;
            IsHq = isHq;
            CollectabilityRating = collectabilityRating;
            ShopItem = shopItem;
        }

        #endregion

        /// <summary>
        ///     Gets the <see cref="IShopListing" /> the current entry is for.
        /// </summary>
        /// <value>The <see cref="IShopListing" /> the current entry is for.</value>
        public IShopListing ShopItem { get; private set; }

        /// <summary>
        ///     Gets the item of the current listing entry.
        /// </summary>
        /// <value>The item of the current listing entry.</value>
        public LazyRow<ItemEx> ItemEx { get; private set; }

        /// <summary>
        ///     Gets the count for the current listing entry.
        /// </summary>
        /// <value>The count for the current listing entry.</value>
        public int Count { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the item is high-quality.
        /// </summary>
        /// <value>A value indicating whether the item is high-quality.</value>
        public bool IsHq { get; private set; }

        /// <summary>
        ///     Gets the collectability rating for the item.
        /// </summary>
        /// <value>The collectability rating of the item.</value>
        public int CollectabilityRating { get; private set; }

        /// <summary>
        ///     Returns a string that represents the current <see cref="ShopListingItem" />.
        /// </summary>
        /// <returns>A string that represents the current <see cref="ShopListingItem" />.</returns>
        public override string ToString() {
            var sb = new StringBuilder();

            if (Count > 1)
                sb.AppendFormat("{0} ", Count);
            sb.Append(ItemEx);
            if (IsHq)
                sb.Append(" (HQ)");
            return sb.ToString();
        }
    }
}