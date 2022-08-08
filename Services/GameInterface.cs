using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using ActionType = CriticalCommonLib.Models.ActionType;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
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
        
        public delegate byte IsInArmoireDelegate(IntPtr armoire, int index);
        public static IsInArmoireDelegate _isInArmoire { get; private set; }
        public static IntPtr _isInArmoirePtr { get; private set; }
        public static IntPtr IsInArmoireAddress { get; private set; }

        public static SigScanner? Scanner { get; private set; }
        private delegate byte HasItemActionUnlockedDelegate(long a1, long a2, long* a3);
        
        private static HasItemActionUnlockedDelegate? _hasItemActionUnlocked;
        private delegate byte HasCardDelegate(IntPtr localPlayer, ushort cardId);
        
        private static HasCardDelegate? _hasCard;
        
        private static ItemToUlongDelegate? _itemToUlong;
        
        private static IntPtr _cardStaticAddr;
        private delegate long ItemToUlongDelegate(uint a1);
        
        public delegate void AcquiredItemsUpdatedDelegate();

        public static event AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;
        
        private delegate void SearchForItemByCraftingMethodDelegate(AgentInterface* agent, ushort itemId);
        private static SearchForItemByCraftingMethodDelegate? _searchForItemByCraftingMethod;

        private delegate int MoveItemSlotDelegate(IntPtr manager, InventoryType srcContainer, uint srcSlot, InventoryType dstContainer,
            uint dstSlot, byte unk = 0);
        
        private static Hook<MoveItemSlotDelegate>? _moveItemSlotHook;
        

        private delegate void SearchForItemByGatheringMethodDelegate(AgentInterface* agent, ushort itemId);
        private static SearchForItemByGatheringMethodDelegate _searchForItemByGatheringMethod;

        public static void Initialise(SigScanner targetModuleScanner)
        {
            Scanner = targetModuleScanner;
            var getInventoryContainerPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 55 BB");
            var getContainerSlotPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 8B 5B 0C");
            InventoryManagerAddress = targetModuleScanner.GetStaticAddressFromSig("BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F8 48 85 C0");
            PlayerStaticAddress = targetModuleScanner.GetStaticAddressFromSig("8B D7 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F B7 E8");
            var hasIaUnlockedPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 ?? C6 03 ?? 48 8B 5C 24");
            var hasCardPtr = targetModuleScanner.ScanText("40 53 48 83 EC 20 48 8B D9 66 85 D2 74");
            _cardStaticAddr = targetModuleScanner.GetStaticAddressFromSig("41 0F B7 17 48 8D 0D");
            
            IsInArmoireAddress = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 16 8B CB");
            _isInArmoirePtr = targetModuleScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 74 16 8B CB E8");
            var itemToUlongPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 33 83 7F 04 00");
            var searchForByCraftingMethodPtr = targetModuleScanner.ScanText("E8 ?? ?? ?? ?? EB 7A 48 83 F8 06");
            
            IntPtr* addonReceiveEventPtr = (IntPtr*)targetModuleScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 48 89 03 33 C0 48 89 43 ?? 48 89 43 ?? 88 43");

            if (hasIaUnlockedPtr == IntPtr.Zero ) {
                throw new ApplicationException("Could not get pointers for item action unlocked");
            }
            if (itemToUlongPtr == IntPtr.Zero ) {
                throw new ApplicationException("Could not get pointers for item to ulong addr");
            }
            if (hasCardPtr == IntPtr.Zero ) {
                throw new ApplicationException("Could not get pointers for has card addr");
            }
            if (_cardStaticAddr == IntPtr.Zero) {
                throw new ApplicationException("Could not get pointers for card static addr");
            }
            if (IsInArmoireAddress == IntPtr.Zero) {
                throw new ApplicationException("Could not get pointers for is in armoire addr");
            }
            if (addonReceiveEventPtr[0] == IntPtr.Zero) {
                PluginLog.LogError("Could not get the pointer for inventory agent receive pointer.");
            }
            
            _hasItemActionUnlocked = Marshal.GetDelegateForFunctionPointer<HasItemActionUnlockedDelegate>(hasIaUnlockedPtr);
            _hasCard = Marshal.GetDelegateForFunctionPointer<HasCardDelegate>(hasCardPtr);
            _itemToUlong = Marshal.GetDelegateForFunctionPointer<ItemToUlongDelegate>(itemToUlongPtr);
            _isInArmoire = Marshal.GetDelegateForFunctionPointer<IsInArmoireDelegate>(IsInArmoireAddress);
            
            _getInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainer>(getInventoryContainerPtr);
            _getContainerSlot = Marshal.GetDelegateForFunctionPointer<GetContainerSlot>(getContainerSlotPtr);
            _searchForItemByCraftingMethod = Marshal.GetDelegateForFunctionPointer<SearchForItemByCraftingMethodDelegate>(searchForByCraftingMethodPtr);
            
            var hookPtr = (IntPtr)InventoryManager.fpMoveItemSlot;
            
            _moveItemSlotHook = Hook<MoveItemSlotDelegate>.FromAddress(hookPtr, MoveItemSlot);;
            _moveItemSlotHook.Enable();

            if(targetModuleScanner.TryScanText("E8 ?? ?? ?? ?? EB 38 48 83 F8 07", out var searchForItemByGatheringMethodPtr))
            {
                _searchForItemByGatheringMethod = Marshal.GetDelegateForFunctionPointer<SearchForItemByGatheringMethodDelegate>(searchForItemByGatheringMethodPtr);
            }
            else
            {
                PluginLog.LogError("Signature for search for item by gathering method failed.");
            }


        }

        public static void OpenGatheringLog(uint itemId)
        {
            var itemIdShort = (ushort)(itemId % 500_000);
            var agent = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.GatheringNote);
            _searchForItemByGatheringMethod(agent, itemIdShort);
        }

        public static int MoveItemSlot(IntPtr manager, InventoryType srcContainer, uint srcSlot, InventoryType dstContainer, uint dstSlot,
            byte unk = 0)
        {
            return _moveItemSlotHook!.Original(manager, srcContainer, srcSlot, dstContainer, dstSlot, unk);
        }
        public static void Dispose()
        {
            AcquiredItems = new HashSet<uint>();
            _moveItemSlotHook?.Dispose();
        }

        public static string GetUserDataPath()
        {
            return Framework.Instance()->UserPath;
        }

        public static HashSet<uint> AcquiredItems = new HashSet<uint>();
        
        public static bool HasAcquired(ItemEx item, bool debug = false)
        {
            if (AcquiredItems.Contains(item.RowId))
            {
                return true;
            }
            var action = item.ItemAction.Value;
            if (action == null) {
                return false;
            }
            var type = (ActionType) action.Type;
            if (type != ActionType.Cards)
            {
                var hasItemActionUnlocked = HasItemActionUnlocked(item, debug);
                if (hasItemActionUnlocked)
                {
                    AcquiredItems.Add(item.RowId);
                    AcquiredItemsUpdated?.Invoke();
                }
                return hasItemActionUnlocked;
            }
            var cardId = item.AdditionalData;
            var card = Service.ExcelCache.GetTripleTriadCard(cardId);
            if (card != null)
            {
                var hasAcquired = HasCard((ushort) card.RowId);
                if (hasAcquired)
                {
                    AcquiredItems.Add(item.RowId);
                    AcquiredItemsUpdated?.Invoke();
                }
                return hasAcquired;
            }

            return false;
        }

        private  static unsafe bool HasItemActionUnlocked(ItemEx item, bool debug = false)
        {
            var itemAction = item.ItemAction.Value;
            if (itemAction == null || _hasItemActionUnlocked == null || _itemToUlong == null) {
                return false;
            }
            var result = _itemToUlong(item.RowId);
            if (result == 0)
            {
                return false;
            }
            var mem = Marshal.AllocHGlobal(64);
            *(long*) (mem) = 0;
            var ret = _hasItemActionUnlocked(result, 0, (long*)mem) == 1;
            Marshal.FreeHGlobal(mem);
            return ret;
        }
        private static bool HasCard(ushort cardId)
        {
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
        
        public static bool IsInArmoire(uint itemId) {
            var row = Service.ExcelCache.GetSheet<Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null) {
                return false;
            }
            return _isInArmoire(_isInArmoirePtr, (int) row.RowId) != 0;
        }
        public static uint? ArmoireIndexIfPresent(uint itemId) {
            var row = Service.ExcelCache.GetSheet<Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null) {
                return null;
            }
            var isInArmoire = _isInArmoire(_isInArmoirePtr, (int) row.RowId) != 0;
            return isInArmoire
                ? row.RowId
                : null;
        }

        public static void OpenCraftingLog(uint itemId)
        {
            itemId = (itemId % 500_000);
            if (Service.ExcelCache.CanCraftItem(itemId))
            {
                var agent = Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.RecipeNote);
                if (_searchForItemByCraftingMethod != null)
                {
                    _searchForItemByCraftingMethod(agent, (ushort)itemId);
                }
            }
        }

        public static void OpenCraftingLog(uint itemId, uint recipeId)
        {
            itemId = (itemId % 500_000);
            if (Service.ExcelCache.CanCraftItem(itemId))
            {
                AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId);
            }
        }
        
        public static unsafe bool ArmoireLoaded => *(byte*) _isInArmoirePtr > 0;
    }
}