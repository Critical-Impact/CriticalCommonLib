using System;
using System.IO;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Services
{
    public static class NetworkDecoder
    {
        public unsafe static NetworkRetainerInformation DecodeRetainerInformation(IntPtr dataPtr)
        {
            NetworkRetainerInformation retainerInformation = new NetworkRetainerInformation();
            using (UnmanagedMemoryStream unmanagedMemoryStream =
                new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 640L))
            {
                using (BinaryReader binaryReader = new BinaryReader((Stream) unmanagedMemoryStream))
                {
                    retainerInformation.unknown = binaryReader.ReadBytes(8);
                    retainerInformation.retainerId = binaryReader.ReadUInt64();
                    retainerInformation.hireOrder = binaryReader.ReadByte();
                    retainerInformation.itemCount = binaryReader.ReadByte();
                    retainerInformation.unknown5 = binaryReader.ReadBytes(2);
                    retainerInformation.gil = binaryReader.ReadUInt32();
                    retainerInformation.sellingCount = binaryReader.ReadByte();
                    retainerInformation.cityId = binaryReader.ReadByte();
                    retainerInformation.classJob = binaryReader.ReadByte();
                    retainerInformation.level = binaryReader.ReadByte();
                    retainerInformation.unknown11 = binaryReader.ReadBytes(4);
                    retainerInformation.retainerTask = binaryReader.ReadUInt32();
                    retainerInformation.retainerTaskComplete = binaryReader.ReadUInt32();
                    binaryReader.ReadByte();
                    var chars = binaryReader.ReadBytes(19);
                    retainerInformation.retainerName =  chars;
                    return retainerInformation;
                }
            }
        }
    }
}