using System;
using System.IO;
using CriticalCommonLib.Models;

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
                using (BinaryReader binaryReader = new BinaryReader((Stream) unmanagedMemoryStream))
                {
                    containerInfo.containerSequence = binaryReader.ReadUInt32();
                    containerInfo.numItems = binaryReader.ReadUInt32();
                    containerInfo.containerId = binaryReader.ReadUInt32();
                    containerInfo.startOrFinish = binaryReader.ReadUInt32();
                    return containerInfo;
                }
            }
        }
        public unsafe static ItemInfo DecodeItemInfo(IntPtr dataPtr)
        {
            ItemInfo itemInfo = new ItemInfo();
            using (UnmanagedMemoryStream unmanagedMemoryStream =
                new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 64L))
            {
                using (BinaryReader binaryReader = new BinaryReader((Stream) unmanagedMemoryStream))
                {
                    itemInfo.containerSequence = binaryReader.ReadUInt32();
                    itemInfo.unknown = binaryReader.ReadUInt32();
                    itemInfo.containerId = binaryReader.ReadUInt16();
                    itemInfo.slot = binaryReader.ReadUInt16();
                    itemInfo.quantity = binaryReader.ReadUInt32();
                    itemInfo.catalogId = binaryReader.ReadUInt32();
                    itemInfo.reservedFlag = binaryReader.ReadUInt32();
                    itemInfo.signatureId = binaryReader.ReadUInt64();
                    itemInfo.hqFlag = binaryReader.ReadByte();
                    itemInfo.unknown2 = binaryReader.ReadByte();
                    itemInfo.condition = binaryReader.ReadUInt16();
                    itemInfo.spiritBond = binaryReader.ReadUInt16();
                    itemInfo.stain = binaryReader.ReadUInt16();
                    itemInfo.glamourCatalogId = binaryReader.ReadUInt32();
                    itemInfo.materia1 = binaryReader.ReadUInt16();
                    itemInfo.materia2 = binaryReader.ReadUInt16();
                    itemInfo.materia3 = binaryReader.ReadUInt16();
                    itemInfo.materia4 = binaryReader.ReadUInt16();
                    itemInfo.materia5 = binaryReader.ReadUInt16();
                    itemInfo.buffer1 = binaryReader.ReadByte();
                    itemInfo.buffer2 = binaryReader.ReadByte();
                    itemInfo.buffer3 = binaryReader.ReadByte();
                    itemInfo.buffer4 = binaryReader.ReadByte();
                    itemInfo.buffer5 = binaryReader.ReadByte();
                    itemInfo.padding = binaryReader.ReadByte();
                    itemInfo.unknown10 = binaryReader.ReadUInt32();
                    return itemInfo;
                }
            }
        }
        public unsafe static UpdateInventorySlot DecodeUpdateInventorySlot(IntPtr dataPtr)
        {
            UpdateInventorySlot inventorySlot = new UpdateInventorySlot();
            using (UnmanagedMemoryStream unmanagedMemoryStream =
                new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 64L))
            {
                using (BinaryReader binaryReader = new BinaryReader((Stream) unmanagedMemoryStream))
                {
                    inventorySlot.sequence = binaryReader.ReadUInt32();
                    inventorySlot.unknown = binaryReader.ReadUInt32();
                    inventorySlot.containerId = binaryReader.ReadUInt16();
                    inventorySlot.slot = binaryReader.ReadUInt16();
                    inventorySlot.quantity = binaryReader.ReadUInt32();
                    inventorySlot.catalogId = binaryReader.ReadUInt32();
                    inventorySlot.reservedFlag = binaryReader.ReadUInt32();
                    inventorySlot.signatureId = binaryReader.ReadUInt64();
                    inventorySlot.hqFlag = binaryReader.ReadUInt16();
                    inventorySlot.condition = binaryReader.ReadUInt16();
                    inventorySlot.spiritBond = binaryReader.ReadUInt16();
                    inventorySlot.color = binaryReader.ReadUInt16();
                    inventorySlot.glamourCatalogId = binaryReader.ReadUInt32();
                    inventorySlot.materia1 = binaryReader.ReadUInt16();
                    inventorySlot.materia2 = binaryReader.ReadUInt16();
                    inventorySlot.materia3 = binaryReader.ReadUInt16();
                    inventorySlot.materia4 = binaryReader.ReadUInt16();
                    inventorySlot.materia5 = binaryReader.ReadUInt16();
                    inventorySlot.buffer1 = binaryReader.ReadByte();
                    inventorySlot.buffer2 = binaryReader.ReadByte();
                    inventorySlot.buffer3 = binaryReader.ReadByte();
                    inventorySlot.buffer4 = binaryReader.ReadByte();
                    inventorySlot.buffer5 = binaryReader.ReadByte();
                    inventorySlot.padding = binaryReader.ReadByte();
                    inventorySlot.unknown10 = binaryReader.ReadUInt32();

                    return inventorySlot;
                }
            }
        }
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
        public unsafe static ItemMarketBoardInfo DecodeItemMarketBoardInfo(IntPtr dataPtr)
        {
            ItemMarketBoardInfo itemMarketBoardInfo = new ItemMarketBoardInfo();
            using (UnmanagedMemoryStream unmanagedMemoryStream =
                new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 640L))
            {
                using (BinaryReader binaryReader = new BinaryReader((Stream) unmanagedMemoryStream))
                {
                    itemMarketBoardInfo.sequence = binaryReader.ReadUInt32();
                    itemMarketBoardInfo.containerId = binaryReader.ReadUInt32();
                    itemMarketBoardInfo.slot = binaryReader.ReadUInt32();
                    itemMarketBoardInfo.unknown = binaryReader.ReadUInt32();
                    itemMarketBoardInfo.unitPrice = binaryReader.ReadUInt32();
                    return itemMarketBoardInfo;
                }
            }
        }
    }
}