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
        private readonly ICondition _condition;
        private readonly GatheringItemSheet _gatheringItemSheet;
        private readonly RecipeSheet _recipeSheet;
        private readonly IFramework _framework;
        private readonly IPluginLog _pluginLog;

        delegate byte GetIsGatheringItemGatheredDelegate(ushort item);

        [Signature("48 89 5C 24 ?? 57 48 83 EC 20 8B D9 8B F9")]
#pragma warning disable CS0649
        GetIsGatheringItemGatheredDelegate? GetIsGatheringItemGathered;
#pragma warning restore CS0649

        public readonly IReadOnlyDictionary<uint, CabinetRow> ArmoireItems;

        public GameInterface(IGameInteropProvider gameInteropProvider, ICondition condition, GatheringItemSheet gatheringItemSheet, CabinetSheet cabinetSheet, RecipeSheet recipeSheet, IFramework framework, IPluginLog pluginLog)
        {
            _condition = condition;
            _gatheringItemSheet = gatheringItemSheet;
            _recipeSheet = recipeSheet;
            _framework = framework;
            _pluginLog = pluginLog;
            framework.RunOnFrameworkThread(() => { gameInteropProvider.InitializeFromAttributes(this); });
            ArmoireItems = cabinetSheet.Where(row => row.Base.Item.RowId != 0).ToDictionary(row => row.Base.Item.RowId, row => row);
        }

        public bool IsGatheringItemGathered(uint gatheringItemId) => GetIsGatheringItemGathered != null && GetIsGatheringItemGathered.Invoke((ushort)gatheringItemId) != 0;

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
            if (_condition[ConditionFlag.Crafting] || _condition[ConditionFlag.Crafting40])
            {
                if (!_condition[ConditionFlag.PreparingToCraft])
                {
                    return false;
                }
            }
            itemId = itemId % 500_000;
            if (_recipeSheet.HasRecipesByItemId(itemId))
            {
                _framework.RunOnFrameworkThread(() => { AgentRecipeNote.Instance()->OpenRecipeByItemId(itemId); });
            }

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
            if (_recipeSheet.HasRecipesByItemId(itemId))
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
