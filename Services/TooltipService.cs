using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Microsoft.Extensions.Logging;
using InventoryType = FFXIVClientStructs.FFXIV.Client.Game.InventoryType;

namespace CriticalCommonLib.Services
{
    public class TooltipService : ITooltipService
    {
        private readonly ILogger<TooltipService> _logger;
        private List<TooltipTweak> _tooltipTweaks = new();

        public void AddTooltipTweak(TooltipTweak tooltipTweak)
        {
            _tooltipTweaks.Add(tooltipTweak);
        }

        public abstract class TooltipTweak
        {
            private readonly ILogger _logger;

            public TooltipTweak(ILogger logger)
            {
                _logger = logger;
            }
            public abstract bool IsEnabled { get; }

            public DalamudLinkPayload? IdentifierPayload { get; set; }

            protected unsafe bool GetTooltipVisibility(ItemTooltipFieldVisibility tooltipField)
            {
                var flags = (ItemTooltipFieldVisibility)RaptureAtkModule.Instance()->AtkArrayDataHolder.GetNumberArrayData(30)->IntArray[5];
                return flags.HasFlag(tooltipField);
            }

            protected unsafe bool GetTooltipVisibility(ItemTooltipField tooltipField)
            {
                return RaptureAtkModule.Instance()->AtkArrayDataHolder.GetNumberArrayData(30)->IntArray[(int)tooltipField] == 0;
            }

            public virtual unsafe void OnGenerateItemTooltip(NumberArrayData* numberArrayData,
                StringArrayData* stringArrayData)
            {
            }

            protected unsafe SeString?
                GetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field) =>
                GetTooltipString(stringArrayData, (int)field);

            protected unsafe SeString? GetTooltipString(StringArrayData* stringArrayData, int field) {
                try {
                    if (stringArrayData->AtkArrayData.Size <= field)
                        throw new IndexOutOfRangeException($"Attempted to get Index#{field} ({field}) but size is only {stringArrayData->AtkArrayData.Size}");

                    var stringAddress = new IntPtr(stringArrayData->StringArray[field]);
                    return stringAddress == IntPtr.Zero ? null : MemoryHelper.ReadSeStringNullTerminated(stringAddress);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to get tooltip string");
                    return new SeString();
                }
            }

            protected unsafe void SetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field, SeString seString) {
                try {
                    seString ??= new SeString();

                    var bytes = seString.Encode().ToList();
                    bytes.Add(0);
                    stringArrayData->SetValue((int)field, bytes.ToArray(), false, true, false);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to set tooltip string");
                }
            }
            protected InventoryItem Item => HoveredItem;
        }

        private unsafe delegate byte ItemHoveredDelegate(AgentItemDetail* agentItemDetail, InventoryItem** outItem, InventoryType* container, short* slot, uint itemId, uint rowIdOrIndex, InventoryItem* item);
        private Hook<ItemHoveredDelegate>? _itemHoveredHook = null;

        private unsafe delegate void GenerateItemTooltip(AddonItemDetail* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
        private Hook<GenerateItemTooltip>? _generateItemTooltipHook = null;

        public unsafe TooltipService(IGameInteropProvider gameInteropProvider, ILogger<TooltipService> logger, IFramework framework)
        {
            _logger = logger;
            framework.RunOnFrameworkThread(() =>
            {
                _itemHoveredHook = gameInteropProvider.HookFromAddress<ItemHoveredDelegate>(
                    (nint)AgentItemDetail.MemberFunctionPointers.OnItemHovered, ItemHoveredDetour);
                _generateItemTooltipHook = gameInteropProvider.HookFromAddress<GenerateItemTooltip>(
                    (nint)AddonItemDetail.MemberFunctionPointers.GenerateItemTooltip, GenerateItemTooltipDetour);
                _generateItemTooltipHook.Enable();
                _itemHoveredHook.Enable();
            });

            _logger.LogDebug("Creating {type} ({this})", GetType().Name, this);

        }


        public static InventoryItem HoveredItem { get; private set; }

        private unsafe byte ItemHoveredDetour(AgentItemDetail* agentItemDetail, InventoryItem** outItem, InventoryType* container, short* slot, uint itemId, uint rowIdOrIndex, InventoryItem* item) {
            var returnValue = _itemHoveredHook!.Original(agentItemDetail, outItem, container, slot, itemId, rowIdOrIndex, item);
            HoveredItem = *item;
            return returnValue;
        }

        public void Disable() {
            _itemHoveredHook?.Disable();
            _generateItemTooltipHook?.Disable();
        }

        public void Dispose() {
            _logger.LogDebug("Disposing {type} ({this})", GetType().Name, this);
            _itemHoveredHook?.Dispose();
            _generateItemTooltipHook?.Dispose();
        }

        private unsafe void GenerateItemTooltipDetour(AddonItemDetail* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
        {
            try
            {
                foreach (var t in _tooltipTweaks)
                {
                    try
                    {
                        if (t.IsEnabled)
                        {
                            t.OnGenerateItemTooltip(numberArrayData, stringArrayData);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while modifying item tooltip");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while modifying item tooltip");
            }

            _generateItemTooltipHook?.Original(addonItemDetail, numberArrayData, stringArrayData);
        }

        public enum ItemTooltipField : byte {
            ItemName,
            GlamourName,
            ItemUiCategory,
            ItemDescription = 13,
            Effects = 16,
            ClassJobCategory = 22,
            Levels = 23,
            DurabilityPercent = 28,
            SpiritbondPercent = 30,
            ExtractableProjectableDesynthesizable = 35,
            Param0 = 37,
            Param1 = 38,
            Param2 = 39,
            Param3 = 40,
            Param4 = 41,
            Param5 = 42,
            ShopSellingPrice = 63,
            ControlsDisplay = 64,
        }
    }
}