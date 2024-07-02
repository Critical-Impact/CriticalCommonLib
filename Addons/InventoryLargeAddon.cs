using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 817)]
    public struct InventoryLargeAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(816)] public byte CurrentTab;

    }
}