using System.Runtime.InteropServices;
using CriticalCommonLib.Enums;
using InventoryTools.Structs;

namespace CriticalCommonLib.Models {
    
    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public unsafe struct MemoryInventoryContainer {
        [FieldOffset(0x00)] public MemoryInventoryItem* Items;
        [FieldOffset(0x08)] public InventoryType Type;
        [FieldOffset(0x0C)] public int SlotCount;
        [FieldOffset(0x10)] public byte Loaded;
    }
}
