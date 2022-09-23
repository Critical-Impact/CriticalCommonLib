using System.Runtime.InteropServices;

namespace CriticalCommonLib.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct ContainerInfo
    {
        [FieldOffset(0)]
        public uint containerSequence;
        [FieldOffset(8)]
        public uint numItems;
        [FieldOffset(16)]
        public uint containerId;
        [FieldOffset(24)]
        public uint startOrFinish;
    }
    
    [StructLayout(LayoutKind.Explicit, Size = 34)]
    public struct ContainerInfoMem
    {
        [FieldOffset(0)]
        public ushort packetId;
        [FieldOffset(2)]
        public uint containerSequence;
        [FieldOffset(10)]
        public uint numItems;
        [FieldOffset(18)]
        public uint containerId;
        [FieldOffset(26)]
        public uint startOrFinish;
    }
}