using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using Cabinet = Lumina.Excel.GeneratedSheets.Cabinet;

namespace CriticalCommonLib.Services
{
    public unsafe class GameInterface : IGameInterface
    {
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly ICondition _condition;

        public delegate void AcquiredItemsUpdatedDelegate();

        public event AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;

        delegate byte GetIsGatheringItemGatheredDelegate(ushort item);
        
        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 8B D9 8B F9")]
#pragma warning disable CS0649
        GetIsGatheringItemGatheredDelegate? GetIsGatheringItemGathered;
#pragma warning restore CS0649

        public readonly IReadOnlyDictionary<uint, Cabinet> ArmoireItems;

        public GameInterface(IGameInteropProvider gameInteropProvider, ICondition condition)
        {
            _gameInteropProvider = gameInteropProvider;
            _condition = condition;
            _gameInteropProvider.InitializeFromAttributes(this);

            ArmoireItems = Service.ExcelCache.GetCabinetSheet()!.Where(row => row.Item.Row != 0).ToDictionary(row => row.Item.Row, row => row);
        }
        
        public bool IsGatheringItemGathered(uint gatheringItemId) =>  GetIsGatheringItemGathered != null && GetIsGatheringItemGathered.Invoke((ushort)gatheringItemId) != 0;

        public bool? IsItemGathered(uint itemId)
        {
            if (Service.ExcelCache.ItemGatheringItem.ContainsKey(itemId))
            {
                foreach (var gatheringItem in Service.ExcelCache.ItemGatheringItem[itemId])
                {
                    if (IsGatheringItemGathered(gatheringItem))
                    {
                        return true;
                    }
                }

                return false;
            }
            return null;
        }

        public void OpenGatheringLog(uint itemId)
        {
            var itemIdShort = (ushort)(itemId % 500_000);
            AgentGatheringNote* agent = (AgentGatheringNote*)Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.GatheringNote);
            if (agent != null)
            {
                agent->OpenGatherableByItemId(itemIdShort);
            }
        }

        public void OpenFishingLog(uint itemId, bool isSpearfishing)
        {
            var itemIdShort = (ushort)(itemId % 500_000);
            var agent = (AgentFishGuide*)Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.FishGuide);
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
            if (!ArmoireItems.TryGetValue(itemId, out var row)) return false;
            if (!UIState.Instance()->Cabinet.IsCabinetLoaded()) return false;

            return UIState.Instance()->Cabinet.IsItemInCabinet((int)row.RowId);
        }

        public uint? ArmoireIndexIfPresent(uint itemId)
        {
            if (!ArmoireItems.TryGetValue(itemId, out var row)) return null;

            var isInArmoire = IsInArmoire(itemId);
            return isInArmoire
                ? row.RowId
                : null;
        }

        public bool OpenCraftingLog(uint itemId)
        {
            if (_condition[ConditionFlag.Crafting] || _condition[ConditionFlag.Crafting40])
            {
                if (!_condition[ConditionFlag.PreparingToCraft])
                {
                    return false;
                }
            }
            itemId = itemId % 500_000;
            if (Service.ExcelCache.CanCraftItem(itemId)) AgentRecipeNote.Instance()->OpenRecipeByItemId(itemId);
            
            return true;
        }

        public bool OpenCraftingLog(uint itemId, uint recipeId)
        {
            if (_condition[ConditionFlag.Crafting] || _condition[ConditionFlag.Crafting40])
            {
                if (!_condition[ConditionFlag.PreparingToCraft])
                {
                    return false;
                }
            }
            itemId = itemId % 500_000;
            if (Service.ExcelCache.CanCraftItem(itemId)) AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId);
            return true;
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
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}
