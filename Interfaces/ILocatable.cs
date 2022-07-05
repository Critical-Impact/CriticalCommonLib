using System.Collections.Generic;

namespace CriticalCommonLib.Interfaces {
    /// <summary>
    /// Interface for objects that have specific locations.
    /// </summary>
    public interface ILocatable {
        /// <summary>
        /// Gets the locations of the current object.
        /// </summary>
        /// <value>The locations of the current object.</value>
        IEnumerable<ILocation> Locations { get; }
    }
}
