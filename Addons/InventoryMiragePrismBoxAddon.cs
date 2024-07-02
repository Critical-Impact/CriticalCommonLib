using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 3960)]
    public struct InventoryMiragePrismBoxAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(416)] public byte ClassJobSelected; //0 = All
        [FieldOffset(3612)] public byte SelectedTab;

    }
}