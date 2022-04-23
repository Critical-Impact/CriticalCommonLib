using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    [StructLayout(LayoutKind.Explicit, Size = 1218)]

    public struct InventoryFreeCompanyChestAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(1216)] public FreeCompanyTab CurrentTab;
    }

    public enum FreeCompanyTab : short
    {
        Unknown = -1,
        One = 0,
        Two = 256,
        Three = 512,
        Four = 768,
        Five = 1024,
        Crystals = 1,
        Unselected = 2,
    }
}