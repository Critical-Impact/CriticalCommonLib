using System;
using System.IO;
using System.Numerics;
using CriticalCommonLib.GameStructs;

namespace CriticalCommonLib.Services
{
    public static class NetworkDecoder
    {
        public unsafe static ContainerInfo DecodeContainerInfo(IntPtr dataPtr)
        {
            ContainerInfo containerInfo = new ContainerInfo();
            using (UnmanagedMemoryStream unmanagedMemoryStream =
                new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 16L))
            {
                using (BinaryReader binaryReader = new BinaryReader(unmanagedMemoryStream))
                {
                    containerInfo.containerSequence = binaryReader.ReadUInt32();
                    containerInfo.numItems = binaryReader.ReadUInt32();
                    containerInfo.containerId = binaryReader.ReadUInt32();
                    containerInfo.startOrFinish = binaryReader.ReadUInt32();
                    return containerInfo;
                }
            }
        }
    }
}