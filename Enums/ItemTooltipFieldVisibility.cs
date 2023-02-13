using System;

namespace CriticalCommonLib.Enums
{
    /// <summary>
    /// An enum containing the fields that can be displayed in item tooltips.
    /// </summary>
    [Flags]
    public enum ItemTooltipFieldVisibility {
#pragma warning disable 1591
        Crafter = 1 << 0,
        Description = 1 << 1,
        VendorSellPrice = 1 << 2,

        // makes the tooltip much smaller when hovered over gear and unset
        // something to do with EquipLevel maybe?
        Unknown3 = 1 << 3,
        Bonuses = 1 << 4,
        Materia = 1 << 5,
        CraftingAndRepairs = 1 << 6,
        Effects = 1 << 8,
        DyeableIndicator = 1 << 10,
        Stat1 = 1 << 11,
        Stat2 = 1 << 12,
        Stat3 = 1 << 13,

        /// <summary>
        /// <para>
        /// Shows item level and equip level.
        /// </para>
        /// <para>
        /// Item level is always visible, but if equip level is set to an empty string, it will be hidden.
        /// </para>
        /// </summary>
        Levels = 1 << 15,
        GlamourIndicator = 1 << 16,
        Unknown19 = 1 << 19,
#pragma warning restore 1591
    }
}