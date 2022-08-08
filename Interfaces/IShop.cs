using System.Collections.Generic;
using CriticalCommonLib.Sheets;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Interfaces
{
    public interface IShop {
        #region Properties

        /// <summary>
        ///     Gets the key of the current shop.
        /// </summary>
        /// <value>The key of the current shop.</value>
        uint RowId { get; }

        /// <summary>
        ///     Gets the name of the current shop.
        /// </summary>
        /// <value>The name of the current shop.</value>
        string Name { get; }

        /// <summary>
        ///     Gets the <see cref="ENpcs" /> offering the current shop.
        /// </summary>
        /// <value>The <see cref="ENpcs" /> offering the current shop.</value>
        IEnumerable<ENpc> ENpcs { get; }

        /// <summary>
        ///     Gets the listings of the current shop.
        /// </summary>
        /// <value>The listings of the current shop.</value>
        IEnumerable<IShopListing> ShopListings { get; }
        
        IEnumerable<LazyRow<ItemEx>> Items { get; }
        
        IEnumerable<LazyRow<ItemEx>> CostItems { get; }
        
        HashSet<uint> ShopItemIds { get; }

        #endregion
    }
}