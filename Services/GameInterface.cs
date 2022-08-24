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
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using ActionType = CriticalCommonLib.Models.ActionType;
using Cabinet = Lumina.Excel.GeneratedSheets.Cabinet;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using InventoryType = CriticalCommonLib.Enums.InventoryType;

namespace CriticalCommonLib.Services
{
    public static unsafe class GameInterface
    {
        public static SigScanner? Scanner { get; private set; }
        
        private static IntPtr _cardStaticAddr;
        public delegate void AcquiredItemsUpdatedDelegate();

        public static event AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;
        
        private delegate int MoveItemSlotDelegate(IntPtr manager, InventoryType srcContainer, uint srcSlot, InventoryType dstContainer,
            uint dstSlot, byte unk = 0);
        
        private static Hook<MoveItemSlotDelegate>? _moveItemSlotHook;
        

        private delegate void SearchForItemByGatheringMethodDelegate(AgentInterface* agent, ushort itemId);
        private static SearchForItemByGatheringMethodDelegate _searchForItemByGatheringMethod;

        public static void Initialise(SigScanner targetModuleScanner)
        {
            Scanner = targetModuleScanner;
            
            var hookPtr = (IntPtr)InventoryManager.fpMoveItemSlot;
            
            _moveItemSlotHook = Hook<MoveItemSlotDelegate>.FromAddress(hookPtr, MoveItemSlot);;
            _moveItemSlotHook.Enable();

            if(targetModuleScanner.TryScanText("E8 ?? ?? ?? ?? EB ?? 48 83 F8 07", out var searchForItemByGatheringMethodPtr))
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
                var itemExdPtr = ExdModule.GetItemRowById(item.RowId);
                if (itemExdPtr != null)
                {
                    var hasItemActionUnlocked = UIState.Instance()->IsItemActionUnlocked(itemExdPtr) == 1;
                    if (hasItemActionUnlocked)
                    {
                        AcquiredItems.Add(item.RowId);
                        AcquiredItemsUpdated?.Invoke();
                    }

                    return hasItemActionUnlocked;
                }

                return false;
            }
            var cardId = item.AdditionalData;
            var card = Service.ExcelCache.GetTripleTriadCard(cardId);
            if (card != null)
            {
                var hasAcquired = UIState.Instance()->IsTripleTriadCardUnlocked((ushort) card.RowId);
                if (hasAcquired)
                {
                    AcquiredItems.Add(item.RowId);
                    AcquiredItemsUpdated?.Invoke();
                }
                return hasAcquired;
            }

            return false;
        }
        
        
        public static bool IsInArmoire(uint itemId) {
            var row = Service.ExcelCache.GetSheet<Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null || !UIState.Instance()->Cabinet.IsCabinetLoaded()) {
                return false;
            }

            return UIState.Instance()->Cabinet.IsItemInCabinet((int)row.RowId);
        }
        public static uint? ArmoireIndexIfPresent(uint itemId) {
            var row = Service.ExcelCache.GetSheet<Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null) {
                return null;
            }
            var isInArmoire = IsInArmoire(itemId);
            return isInArmoire
                ? row.RowId
                : null;
        }

        public static void OpenCraftingLog(uint itemId)
        {
            itemId = (itemId % 500_000);
            if (Service.ExcelCache.CanCraftItem(itemId))
            {
                AgentRecipeNote.Instance()->OpenRecipeByItemId(itemId);
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
    }
}