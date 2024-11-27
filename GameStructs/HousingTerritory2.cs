using System.Runtime.InteropServices;

namespace CriticalCommonLib.GameStructs;

[StructLayout(LayoutKind.Explicit, Size = 41376)]
public unsafe partial struct HousingTerritory2 {
    [FieldOffset(38560)] public ulong HouseId;

    public uint TerritoryTypeId
    {
        get
        {
            return (uint)((HouseId >> 32) & 0xFFFF);
        }
    }
}
