using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services
{
    public unsafe class GameInterface : IGameInterface
    {
        public delegate void AcquiredItemsUpdatedDelegate();

        public event AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;

        private delegate void SearchForItemByGatheringMethodDelegate(AgentInterface* agent, ushort itemId);
        private readonly SearchForItemByGatheringMethodDelegate _searchForItemByGatheringMethod;

        public GameInterface()
        {
            if(Service.Scanner.TryScanText("E8 ?? ?? ?? ?? EB 63 48 83 F8 ??", out var searchForItemByGatheringMethodPtr))
            {
                _searchForItemByGatheringMethod = Marshal.GetDelegateForFunctionPointer<SearchForItemByGatheringMethodDelegate>(searchForItemByGatheringMethodPtr);
            }
            else
            {
                PluginLog.LogError("Signature for search for item by gathering method failed.");
            }
        }

        public void OpenGatheringLog(uint itemId)
        {
            var itemIdShort = (ushort)(itemId % 500_000);
            var agent =
                Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.GatheringNote);
            _searchForItemByGatheringMethod.Invoke(agent, itemIdShort);
        }

        public void OpenFishingLog(uint itemId, bool isSpearfishing)
        {
            var itemIdShort = (ushort)(itemId % 500_000);
            var agent = (AgentFishGuide*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.FishGuide);
            if (agent != null)
            {
                agent->OpenForItemId(itemIdShort, isSpearfishing);
            }
        }

        public HashSet<uint> AcquiredItems { get; set; } = new();

        public bool HasAcquired(ItemEx item, bool debug = false)
        {
            if (AcquiredItems.Contains(item.RowId)) return true;

            var action = item.ItemAction.Value;
            if (action == null) return false;

            var type = (ActionType)action.Type;
            if (type != ActionType.Cards)
            {
                var itemExdPtr = ExdModule.GetItemRowById(item.RowId);
                if (itemExdPtr != null)
                {
                    var hasItemActionUnlocked = UIState.Instance()->IsItemActionUnlocked(itemExdPtr) == 1;
                    if (hasItemActionUnlocked)
                    {
                        AcquiredItems.Add(item.RowId);
                        Service.Framework.RunOnTick(() => { AcquiredItemsUpdated?.Invoke(); });
                    }

                    return hasItemActionUnlocked;
                }

                return false;
            }

            var cardId = item.AdditionalData;
            var card = Service.ExcelCache.GetTripleTriadCardSheet().GetRow(cardId);
            if (card != null)
            {
                var hasAcquired = UIState.Instance()->IsTripleTriadCardUnlocked((ushort)card.RowId);
                if (hasAcquired)
                {
                    AcquiredItems.Add(item.RowId);
                    Service.Framework.RunOnTick(() => { AcquiredItemsUpdated?.Invoke(); });
                }

                return hasAcquired;
            }

            return false;
        }


        public bool IsInArmoire(uint itemId)
        {
            var row = Service.ExcelCache.GetCabinetSheet()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null || !UIState.Instance()->Cabinet.IsCabinetLoaded()) return false;

            return UIState.Instance()->Cabinet.IsItemInCabinet((int)row.RowId);
        }

        public uint? ArmoireIndexIfPresent(uint itemId)
        {
            var row = Service.ExcelCache.GetCabinetSheet()!.FirstOrDefault(row => row.Item.Row == itemId);
            if (row == null) return null;

            var isInArmoire = IsInArmoire(itemId);
            return isInArmoire
                ? row.RowId
                : null;
        }

        public void OpenCraftingLog(uint itemId)
        {
            itemId = itemId % 500_000;
            if (Service.ExcelCache.CanCraftItem(itemId)) AgentRecipeNote.Instance()->OpenRecipeByItemId(itemId);
        }

        public void OpenCraftingLog(uint itemId, uint recipeId)
        {
            itemId = itemId % 500_000;
            if (Service.ExcelCache.CanCraftItem(itemId)) AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId);
        }
        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {

            }
            _disposed = true;         
        }
        
        ~GameInterface()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}