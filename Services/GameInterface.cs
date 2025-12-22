using System;
using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Models;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
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
        private readonly ICondition _condition;
        private readonly GatheringItemSheet _gatheringItemSheet;
        private readonly RecipeSheet _recipeSheet;
        private readonly ItemSheet _itemSheet;
        private readonly IFramework _framework;
        private readonly IPluginLog _pluginLog;

        public readonly IReadOnlyDictionary<uint, CabinetRow> ArmoireItems;

        public GameInterface(IGameInteropProvider gameInteropProvider, ICondition condition, GatheringItemSheet gatheringItemSheet, CabinetSheet cabinetSheet, RecipeSheet recipeSheet, ItemSheet itemSheet, IFramework framework, IPluginLog pluginLog)
        {
            _condition = condition;
            _gatheringItemSheet = gatheringItemSheet;
            _recipeSheet = recipeSheet;
            _itemSheet = itemSheet;
            _framework = framework;
            _pluginLog = pluginLog;
            framework.RunOnFrameworkThread(() => { gameInteropProvider.InitializeFromAttributes(this); });
            ArmoireItems = cabinetSheet.Where(row => row.Base.Item.RowId != 0).ToDictionary(row => row.Base.Item.RowId, row => row);
        }


        public bool IsGatheringItemGathered(uint gatheringItemId) => QuestManager.IsGatheringItemGathered((ushort)gatheringItemId);

        public bool? IsItemGathered(uint itemId)
        {
            var gatheringLookup = _gatheringItemSheet.GatheringItemsByItemId;
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
            _framework.RunOnFrameworkThread(() =>
            {
                var itemIdShort = (ushort)(itemId % 500_000);
                AgentGatheringNote* agent =
                    (AgentGatheringNote*)Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(
                        AgentId.GatheringNote);
                if (agent != null)
                {
                    agent->OpenGatherableByItemId(itemIdShort);
                }
            });
        }

        public void OpenFishingLog(uint itemId, bool isSpearfishing)
        {
            _framework.RunOnFrameworkThread(() =>
            {
                var itemIdShort = (ushort)(itemId % 500_000);
                var agent =
                    (AgentFishGuide*)Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(
                        AgentId.FishGuide);
                if (agent != null)
                {
                    agent->OpenForItemId(itemIdShort, isSpearfishing);
                }
            });
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
            if (_condition[ConditionFlag.Crafting] && !_condition[ConditionFlag.PreparingToCraft])
            {
                return false;
            }
            if (_condition[ConditionFlag.ExecutingCraftingAction] && !_condition[ConditionFlag.PreparingToCraft])
            {
                return false;
            }

            if (itemId == 0)
            {
                return false;
            }
            itemId = itemId % 500_000;
            if (_recipeSheet.HasRecipesByItemId(itemId) && _itemSheet.BaseSheet.HasRow(itemId))
            {
                _framework.RunOnFrameworkThread(() => { AgentRecipeNote.Instance()->SearchRecipeByItemId(itemId); });
            }

            return true;
        }

        public bool OpenCraftingLog(uint itemId, uint recipeId)
        {
            if (_condition[ConditionFlag.Crafting] && !_condition[ConditionFlag.PreparingToCraft])
            {
                return false;
            }
            if (_condition[ConditionFlag.ExecutingCraftingAction] && !_condition[ConditionFlag.PreparingToCraft])
            {
                return false;
            }

            if (itemId == 0 || recipeId == 0)
            {
                return false;
            }
            itemId %= 500_000;
            if (_recipeSheet.HasRecipesByItemId(itemId) && _recipeSheet.BaseSheet.HasRow(recipeId))
            {
                _framework.RunOnFrameworkThread(() => { AgentRecipeNote.Instance()->OpenRecipeByRecipeId(recipeId); });
            }
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
                _pluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}
