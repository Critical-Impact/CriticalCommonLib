namespace CriticalCommonLib.Models
{
    public struct UpdateInventorySlot
    {
        public uint sequence;
        public uint unknown;
        public ushort containerId;
        public ushort slot;
        public uint quantity;
        public uint catalogId;
        public uint reservedFlag;
        public ulong signatureId;
        public ushort hqFlag;
        public ushort condition;
        public ushort spiritBond;
        public ushort color;
        public uint glamourCatalogId;
        public ushort materia1;
        public ushort materia2;
        public ushort materia3;
        public ushort materia4;
        public ushort materia5;
        public byte buffer1;
        public byte buffer2;
        public byte buffer3;
        public byte buffer4;
        public byte buffer5;
        public byte padding;
        public uint unknown10;
    }
}