using System.Collections.Generic;
using CriticalCommonLib.Sheets;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Interfaces
{
    public interface IItemSource
    {
        /// <summary>
        /// Gets the <see cref="Item"/>s that can be obtained from the current object.
        /// </summary>
        /// <value>The <see cref="Item"/>s that can be obtained from the current object.</value>
        IEnumerable<LazyRow<ItemEx>> Items { get; }
    }
}