using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 2742)]
    public struct InventoryMiragePrismBoxAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(400)] public byte ClassJobSelected; //0 = All
        [FieldOffset(401)] public byte OnlyDisplayRaceGenderItems;
        [FieldOffset(2740)] public byte SelectedTab;

    }
}