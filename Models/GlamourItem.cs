using System.Runtime.InteropServices;

namespace CriticalCommonLib.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public readonly struct GlamourItem {
        [FieldOffset(4)]
        internal readonly uint Index;
        [FieldOffset(8)]
        internal readonly uint ItemId;
        [FieldOffset(26)]
        internal readonly byte StainId;
    }
    
    public enum MirageSource {
        GlamourDresser = 1,
        Armoire = 2,
    }
}