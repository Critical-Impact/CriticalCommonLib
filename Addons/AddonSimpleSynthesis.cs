using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 568)]
    public struct AddonSimpleSynthesis
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
    }
}