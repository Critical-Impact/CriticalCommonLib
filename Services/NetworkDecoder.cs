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
        
        public unsafe static NpcSpawn DecodeNpcSpawn(IntPtr dataPtr)
        {
            NpcSpawn npcSpawn = new NpcSpawn();
            using (UnmanagedMemoryStream unmanagedMemoryStream =
                   new UnmanagedMemoryStream((byte*) dataPtr.ToPointer(), 671L))
            {
                using (BinaryReader binaryReader = new BinaryReader((Stream) unmanagedMemoryStream))
                {
                    
                    npcSpawn.gimmickId = binaryReader.ReadUInt32();
                    npcSpawn.u2b = binaryReader.ReadByte();
                    npcSpawn.u2ab = binaryReader.ReadByte();
                    npcSpawn.gmRank = binaryReader.ReadByte();
                    npcSpawn.u3b = binaryReader.ReadByte();
                    npcSpawn.aggressionMode = binaryReader.ReadByte();
                    npcSpawn.onlineStatus = binaryReader.ReadByte();
                    npcSpawn.u3c = binaryReader.ReadByte();
                    npcSpawn.pose = binaryReader.ReadByte();
                    npcSpawn.u4 = binaryReader.ReadUInt32();
                    npcSpawn.targetId = binaryReader.ReadUInt64();
                    npcSpawn.u6 = binaryReader.ReadUInt32();
                    npcSpawn.u7 = binaryReader.ReadUInt32();
                    npcSpawn.mainWeaponModel = binaryReader.ReadUInt64();
                    npcSpawn.secWeaponModel = binaryReader.ReadUInt64();
                    npcSpawn.craftToolModel = binaryReader.ReadUInt64();
                    npcSpawn.u14 = binaryReader.ReadUInt32();
                    npcSpawn.u15 = binaryReader.ReadUInt32();
                    npcSpawn.bNpcBase = binaryReader.ReadUInt32();
                    npcSpawn.bNpcName = binaryReader.ReadUInt32();
                    npcSpawn.levelId = binaryReader.ReadUInt32();
                    npcSpawn.u19 = binaryReader.ReadUInt32();
                    npcSpawn.directorId = binaryReader.ReadUInt32();
                    npcSpawn.spawnerId = binaryReader.ReadUInt32();
                    npcSpawn.parentActorId = binaryReader.ReadUInt32();
                    npcSpawn.hPMax = binaryReader.ReadUInt32();
                    npcSpawn.hPCurr = binaryReader.ReadUInt32();
                    npcSpawn.displayFlags = binaryReader.ReadUInt32();
                    npcSpawn.fateId = binaryReader.ReadUInt16();
                    npcSpawn.mPCurr = binaryReader.ReadUInt16();
                    npcSpawn.unknown1 = binaryReader.ReadUInt16();
                    npcSpawn.unknown2 = binaryReader.ReadUInt16();
                    npcSpawn.modelChara = binaryReader.ReadUInt16();
                    npcSpawn.rotation = binaryReader.ReadUInt16();
                    npcSpawn.activeMinion = binaryReader.ReadUInt16();
                    npcSpawn.spawnIndex = binaryReader.ReadByte();
                    npcSpawn.state = binaryReader.ReadByte();
                    npcSpawn.persistantEmote = binaryReader.ReadByte();
                    npcSpawn.modelType = binaryReader.ReadByte();
                    npcSpawn.subtype = binaryReader.ReadByte();
                    npcSpawn.voice = binaryReader.ReadByte();
                    npcSpawn.u25c = binaryReader.ReadUInt16();
                    npcSpawn.enemyType = binaryReader.ReadByte();
                    npcSpawn.level = binaryReader.ReadByte();
                    npcSpawn.classJob = binaryReader.ReadByte();
                    npcSpawn.u26d = binaryReader.ReadByte();
                    npcSpawn.u27a = binaryReader.ReadUInt16();
                    npcSpawn.currentMount = binaryReader.ReadByte();
                    npcSpawn.mountHead = binaryReader.ReadByte();
                    npcSpawn.mountBody = binaryReader.ReadByte();
                    npcSpawn.mountFeet = binaryReader.ReadByte();
                    npcSpawn.mountColor = binaryReader.ReadByte();
                    npcSpawn.scale = binaryReader.ReadByte();
                    npcSpawn.elementalLevel = binaryReader.ReadUInt16();
                    npcSpawn.element = binaryReader.ReadUInt16();
                    NpcSpawnEffect[] npcSpawnEffects = new NpcSpawnEffect[30];
                    for (int i = 0; i < 30; i++)
                    {
                        var npcSpawnEffect = new NpcSpawnEffect();
                        npcSpawnEffect.id = binaryReader.ReadUInt16();
                        npcSpawnEffect.param = binaryReader.ReadUInt16();
                        npcSpawnEffect.duration = binaryReader.ReadSingle();
                        npcSpawnEffect.sourceActorId = binaryReader.ReadUInt32();
                        npcSpawnEffects[i] = npcSpawnEffect;
                    }

                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    npcSpawn.npcSpawnEffects = npcSpawnEffects;
                    var x = binaryReader.ReadSingle();
                    var y = binaryReader.ReadSingle();
                    var z = binaryReader.ReadSingle();
                    npcSpawn.pos = new Vector3(x, y, z);
                    return npcSpawn;
                }
            }
        }
    }
}