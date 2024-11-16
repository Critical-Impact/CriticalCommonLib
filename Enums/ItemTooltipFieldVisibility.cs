using System;

namespace CriticalCommonLib.Enums
{
    /// <summary>
    /// An enum containing the fields that can be displayed in item tooltips.
    /// </summary>
    [Flags]
    public enum ItemTooltipFieldVisibility {
#pragma warning disable 1591
        Crafter = 1,
        Description = 2,
        VendorSellPrice = 4,

        // makes the tooltip much smaller when hovered over gear and unset
        // something to do with EquipLevel maybe?
        Unknown3 = 8,
        Bonuses = 16,
        Materia = 32,
        CraftingAndRepairs = 64,
        Effects = 256,
        DyeableIndicator = 1024,
        Stat1 = 2048,
        Stat2 = 4096,
        Stat3 = 8192,

        /// <summary>
        /// <para>
        /// Shows item level and equip level.
        /// </para>
        /// <para>
        /// Item level is always visible, but if equip level is set to an empty string, it will be hidden.
        /// </para>
        /// </summary>
        Levels = 32768,
        GlamourIndicator = 65536,
        Unknown19 = 524288,
#pragma warning restore 1591
    }
}