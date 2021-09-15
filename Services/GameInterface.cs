using System;
using System.Runtime.InteropServices;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using Dalamud.Game;
using Dalamud.Plugin;

namespace CriticalCommonLib.Services
{
    public unsafe class GameInterface
    {
        private delegate IntPtr GameAlloc(ulong size, IntPtr unk, IntPtr allocator, IntPtr alignment);

        private delegate IntPtr GetGameAllocator();

        private static GameAlloc _gameAlloc;
        private static GetGameAllocator _getGameAllocator;

        private delegate MemoryInventoryContainer* GetInventoryContainer(IntPtr inventoryManager, InventoryType inventoryType);
        private delegate MemoryInventoryItem* GetContainerSlot(MemoryInventoryContainer* inventoryContainer, int slotId);

        private static GetInventoryContainer _getInventoryContainer;
        private static GetContainerSlot _getContainerSlot;

        public static IntPtr InventoryManagerAddress;
        public static IntPtr PlayerStaticAddress { get; private set; }

        public static SigScanner Scanner { get; private set; }

        public GameInterface(SigScanner targetModuleScanner)
        {
            Scanner = targetModuleScanner;
            var gameAllocPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 45 8D 67 23");
            var getGameAllocatorPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 75 08");

            InventoryManagerAddress = targetModuleScanner.GetStaticAddressFromSig("BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F8 48 85 C0");
            var getInventoryContainerPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 55 BB");
            var getContainerSlotPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 5B 0C");

            PlayerStaticAddress = targetModuleScanner.GetStaticAddressFromSig("8B D7 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B7 E8");

            _gameAlloc = Marshal.GetDelegateForFunctionPointer<GameAlloc>(gameAllocPtr);
            _getGameAllocator = Marshal.GetDelegateForFunctionPointer<GetGameAllocator>(getGameAllocatorPtr);

            _getInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainer>(getInventoryContainerPtr);
            _getContainerSlot = Marshal.GetDelegateForFunctionPointer<GetContainerSlot>(getContainerSlotPtr);
        }
        
        public static MemoryInventoryContainer* GetContainer(InventoryType inventoryType) {
            if (InventoryManagerAddress == IntPtr.Zero) return null;
            return _getInventoryContainer(InventoryManagerAddress, inventoryType);
        }

        public static MemoryInventoryItem* GetContainerItem(MemoryInventoryContainer* container, int slot) {
            if (container == null) return null;
            return _getContainerSlot(container, slot);
        }

        public static MemoryInventoryItem* GetInventoryItem(InventoryType inventoryType, int slotId) {
            if (InventoryManagerAddress == IntPtr.Zero) return null;
            var container = _getInventoryContainer(InventoryManagerAddress, inventoryType);
            return container == null ? null : _getContainerSlot(container, slotId);
        }

    }
}