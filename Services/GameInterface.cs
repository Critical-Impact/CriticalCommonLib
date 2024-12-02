using System;
using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Models;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using Lumina.Excel.Sheets;
using Cabinet = Lumina.Excel.Sheets.Cabinet;

namespace CriticalCommonLib.Services
{
    public unsafe class GameInterface : IGameInterface
    {
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly ICondition _condition;
        private readonly ExcelCache _excelCache;

        public delegate void AcquiredItemsUpdatedDelegate();

        public event AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;

        delegate byte GetIsGatheringItemGatheredDelegate(ushort item);

        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 8B D9 8B F9")]
#pragma warning disable CS0649
        GetIsGatheringItemGatheredDelegate? GetIsGatheringItemGathered;
#pragma warning restore CS0649

        public readonly IReadOnlyDictionary<uint, CabinetRow> ArmoireItems;

        public GameInterface(IGameInteropProvider gameInteropProvider, ICondition condition, ExcelCache excelCache, IFramework framework)
        {
            _gameInteropProvider = gameInteropProvider;
            _condition = condition;
            _excelCache = excelCache;
            framework.RunOnFrameworkThread(() => { _gameInteropProvider.InitializeFromAttributes(this); });
            ArmoireItems = excelCache.GetCabinetSheet().Where(row => row.Base.Item.RowId != 0).ToDictionary(row => row.Base.Item.RowId, row => row);
        }

        public bool IsGatheringItemGathered(uint gatheringItemId) =>  GetIsGatheringItemGathered != null && GetIsGatheringItemGathered.Invoke((ushort)gatheringItemId) != 0;

        public bool? IsItemGathered(uint itemId)
        {
            var gatheringLookup = _excelCache.GetGatheringItemSheet().GatheringItemsByItemId;
            if (gatheringLookup.ContainsKey(itemId))
            {
                foreach (var gatheringItem in gatheringLookup[itemId])
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

        public bool HasAcquired(ItemRow item, bool debug = false)
        {
            if (AcquiredItems.Contains(item.RowId)) return true;

            var action = item.Base.ItemAction.ValueNullable;
            if (action == null) return false;

            var type = (ActionType)action.Value.Type;
            if (type != ActionType.Cards)
            {
                var itemRowdPtr = ExdModule.GetItemRowById(item.RowId);
                if (itemRowdPtr != null)
                {
                    var hasItemActionUnlocked = UIState.Instance()->IsItemActionUnlocked(itemRowdPtr) == 1;
                    if (hasItemActionUnlocked)
                    {
                        AcquiredItems.Add(item.RowId);
                        Service.Framework.RunOnTick(() => { AcquiredItemsUpdated?.Invoke(); });
                    }

                    return hasItemActionUnlocked;
                }

                return false;
            }

            var card = item.Base.AdditionalData;
            if (card.Is<TripleTriadCard>())
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
            if (_excelCache.GetRecipeSheet().HasRecipesByItemId(itemId)) AgentRecipeNote.Instance()->OpenRecipeByItemId(itemId);

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
            if (_excelCache.GetRecipeSheet().HasRecipesByItemId(itemId)) AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId);
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
