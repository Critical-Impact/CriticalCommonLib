using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 769)]
    public struct InventoryRetainerLargeAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(768)] public byte CurrentTab;

    }
}