using CriticalCommonLib.Sheets;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Interfaces {
    /// <summary>
    /// Interface for objects defining a location in a zone (in map-coordinates).
    /// </summary>
    public interface ILocation {
        /// <summary>
        /// Gets the x-coordinate of the current object.
        /// </summary>
        /// <value>The x-coordinate of the current object.</value>
        double MapX { get; }

        /// <summary>
        /// Gets the y-coordinate of the current object.
        /// </summary>
        /// <value>The y-coordinate of the current object.</value>
        double MapY { get; }

        LazyRow<MapEx> MapEx { get; }

        /// <summary>
        /// Gets the <see cref="PlaceName"/> of the current object's location.
        /// </summary>
        /// <value>The <see cref="PlaceName"/> of the current object's location.</value>
        LazyRow<PlaceName> PlaceName { get; }
    }
}
