using System.Runtime.InteropServices;

namespace CriticalCommonLib.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 136)]
    public readonly struct GlamourItem {
        [FieldOffset(72)]
        public readonly uint Index;
        [FieldOffset(76)]
        public readonly uint ItemId;
        [FieldOffset(94)]
        public readonly byte StainId;

        public uint CorrectedItemId
        {
            get
            {
                if (ItemId >= 1_000_000)
                {
                    return ItemId - 1_000_000;
                }

                return ItemId;
            }
        }
    }
    
    public enum MirageSource {
        GlamourDresser = 1,
        Armoire = 2,
    }
}