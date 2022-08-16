using CriticalCommonLib.Sheets;
using Lumina.Excel;

namespace CriticalCommonLib.Interfaces
{
    public interface IShopListingItem {
        #region Properties

        /// <summary>
        ///     Gets the item of the current listing entry.
        /// </summary>
        /// <value>The item of the current listing entry.</value>
        LazyRow<ItemEx> ItemEx { get; }

        /// <summary>
        ///     Gets the count for the current listing entry.
        /// </summary>
        /// <value>The count for the current listing entry.</value>
        int Count { get; }

        /// <summary>
        ///     Gets a value indicating whether the item is high-quality.
        /// </summary>
        /// <value>A value indicating whether the item is high-quality.</value>
        bool IsHq { get; }

        /// <summary>
        ///     Gets the collectability rating for the item.
        /// </summary>
        int CollectabilityRating { get; }

        /// <summary>
        ///     Gets the <see cref="IShopListing" /> the current entry is for.
        /// </summary>
        /// <value>The <see cref="IShopListing" /> the current entry is for.</value>
        IShopListing ShopItem { get; }

        #endregion
    }
}