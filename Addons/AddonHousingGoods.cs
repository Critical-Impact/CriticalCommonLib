using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 810)]
    public struct AddonHousingGoods
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(809)] public byte CurrentTab;

    }
}