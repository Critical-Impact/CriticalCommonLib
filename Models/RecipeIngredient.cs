using CriticalCommonLib.Sheets;
using Lumina.Excel;

namespace CriticalCommonLib.Models
{
    public class RecipeIngredient {
        #region Properties

        /// <summary>
        ///     Gets the <see cref="Item" /> of the current ingredient.
        /// </summary>
        /// <value>The <see cref="Item" /> of the current ingredient.</value>
        public LazyRow<ItemEx> Item { get; private set; }

        /// <summary>
        ///     Gets the item count for the current ingredient.
        /// </summary>
        /// <value>The item count for the current ingredient.</value>
        public int Count { get; private set; }

        /// <summary>
        ///     Gets the quality gained per high-quality item used for the current ingredient.
        /// </summary>
        /// <value>The quality gained per high-quality item used for the current ingredient.</value>
        public float QualityPerItem { get; internal set; }

        #endregion

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecipeIngredient" /> class.
        /// </summary>
        /// <param name="item"><see cref="Item" /> for the ingredient.</param>
        /// <param name="count">Item count for the ingredient.</param>
        public RecipeIngredient(LazyRow<ItemEx> item, int count) {
            Item = item;
            Count = count;
        }

        #endregion
    }
}