namespace CriticalCommonLib.Models;

public enum InventoryChangeReason
{
    /// <summary>
    /// The slot was empty and an item was added
    /// </summary>
    Added,
    /// <summary>
    /// The slot had an item and is now empty
    /// </summary>
    Removed,
    /// <summary>
    /// The item moved to a different location
    /// </summary>
    Moved,
    /// <summary>
    /// The ID of the item changed, most likely due to a change in Flag
    /// </summary>
    ItemIdChanged,
    /// <summary>
    /// The quantity of the item changed
    /// </summary>
    QuantityChanged,
    SpiritbondChanged,
    ConditionChanged,
    FlagsChanged,
    MateriaChanged,
    StainChanged,
    GlamourChanged,
    Transferred,
    MarketPriceChanged,
    GearsetsChanged,
    
}