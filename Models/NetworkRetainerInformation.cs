using System;

namespace CriticalCommonLib.Models
{
     public struct NetworkRetainerInformation
    {
        public Byte[] unknown;
        public ulong retainerId;
        public Byte hireOrder;
        public Byte itemCount;
        public Byte[] unknown5;
        public uint gil;
        public Byte sellingCount;
        public Byte cityId;
        public Byte classJob;
        public Byte level;
        public Byte[] unknown11;
        public uint retainerTask;
        public uint retainerTaskComplete;
        public Byte[] retainerName;
    };
    
}