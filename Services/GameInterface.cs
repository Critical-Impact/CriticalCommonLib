using System;
using System.Runtime.InteropServices;
using CriticalCommonLib.Models;
using Dalamud.Game;
using Lumina.Excel.GeneratedSheets;
using ActionType = CriticalCommonLib.Models.ActionType;
using InventoryType = CriticalCommonLib.Enums.InventoryType;

namespace CriticalCommonLib.Services
{
    public static unsafe class GameInterface
    {
        private delegate MemoryInventoryContainer* GetInventoryContainer(IntPtr inventoryManager, InventoryType inventoryType);
        private delegate MemoryInventoryItem* GetContainerSlot(MemoryInventoryContainer* inventoryContainer, int slotId);

        private static GetInventoryContainer? _getInventoryContainer;
        private static GetContainerSlot? _getContainerSlot;

        public static IntPtr InventoryManagerAddress;
        public static IntPtr PlayerStaticAddress { get; private set; }

        public static SigScanner? Scanner { get; private set; }
        private delegate byte HasItemActionUnlockedDelegate(IntPtr mem);
        
        private static HasItemActionUnlockedDelegate? _hasItemActionUnlocked;
        private delegate byte HasCardDelegate(IntPtr localPlayer, ushort cardId);
        
        private static HasCardDelegate? _hasCard;
        
        private static IntPtr _cardStaticAddr;

        public static void Initialise(SigScanner targetModuleScanner)
        {
            Scanner = targetModuleScanner;
            var getInventoryContainerPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 55 BB");
            var getContainerSlotPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 5B 0C");
            InventoryManagerAddress = targetModuleScanner.GetStaticAddressFromSig("BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F8 48 85 C0");
            PlayerStaticAddress = targetModuleScanner.GetStaticAddressFromSig("8B D7 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B7 E8");
            var hasIaUnlockedPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 A9");
            var hasCardPtr = targetModuleScanner.ScanText("40 53 48 83 EC 20 48 8B D9 66 85 D2 74");
            _cardStaticAddr = targetModuleScanner.GetStaticAddressFromSig("41 0F B7 17 48 8D 0D");
            
            if (hasIaUnlockedPtr == IntPtr.Zero || hasCardPtr == IntPtr.Zero || _cardStaticAddr == IntPtr.Zero) {
                throw new ApplicationException("Could not get pointers for game functions");
            }
            
            _hasItemActionUnlocked = Marshal.GetDelegateForFunctionPointer<HasItemActionUnlockedDelegate>(hasIaUnlockedPtr);
            _hasCard = Marshal.GetDelegateForFunctionPointer<HasCardDelegate>(hasCardPtr);
            
            _getInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainer>(getInventoryContainerPtr);
            _getContainerSlot = Marshal.GetDelegateForFunctionPointer<GetContainerSlot>(getContainerSlotPtr);
        }
        
        public static bool HasAcquired(Item item) {
            var action = item.ItemAction.Value;
            if (action == null) {
                return false;
            }
            var type = (ActionType) action.Type;
            if (type != ActionType.Cards) {
                return HasItemActionUnlocked(item);
            }
            var cardId = item.AdditionalData;
            var card = ExcelCache.GetTripleTriadCard(cardId);
            return card != null && HasCard((ushort) card.RowId);
        }
        private  static unsafe bool HasItemActionUnlocked(Item item) {
            var itemAction = item.ItemAction.Value;
            if (itemAction == null || _hasItemActionUnlocked == null) {
                return false;
            }
            var type = (ActionType) itemAction.Type;
            var mem = Marshal.AllocHGlobal(256);
            *(uint*) (mem + 142) = itemAction.RowId;
            
            if (type == ActionType.OrchestrionRolls) {
                *(uint*) (mem + 112) = item.AdditionalData;
            }
            var ret = _hasItemActionUnlocked(mem) == 1;
            Marshal.FreeHGlobal(mem);
            return ret;
        }
        private static bool HasCard(ushort cardId) {
            if (_hasCard == null)
            {
                return false;
            }
            return _hasCard(_cardStaticAddr, cardId) == 1;
        }
        
        public static MemoryInventoryContainer* GetContainer(InventoryType inventoryType) {
            if (_getInventoryContainer == null || InventoryManagerAddress == IntPtr.Zero) return null;
            return _getInventoryContainer(InventoryManagerAddress, inventoryType);
        }

        public static MemoryInventoryItem* GetContainerItem(MemoryInventoryContainer* container, int slot) {
            if (_getContainerSlot == null || container == null) return null;
            return _getContainerSlot(container, slot);
        }

        public static MemoryInventoryItem* GetInventoryItem(InventoryType inventoryType, int slotId) {
            if (_getInventoryContainer == null || _getContainerSlot == null || InventoryManagerAddress == IntPtr.Zero) return null;
            var container = _getInventoryContainer(InventoryManagerAddress, inventoryType);
            return container == null ? null : _getContainerSlot(container, slotId);
        }

    }
}