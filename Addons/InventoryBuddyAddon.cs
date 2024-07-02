using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 1177)]
    public struct InventoryBuddyAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(1176)] public byte CurrentTab;

    }
}