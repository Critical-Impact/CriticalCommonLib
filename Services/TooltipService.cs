using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Microsoft.Extensions.Logging;

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
            public abstract bool IsEnabled { get; }

            public DalamudLinkPayload? IdentifierPayload { get; set; }

            protected unsafe bool GetTooltipVisibility(ItemTooltipFieldVisibility tooltipField)
            {
                var flags = (ItemTooltipFieldVisibility)RaptureAtkModule.Instance()->AtkArrayDataHolder.GetNumberArrayData(29)->IntArray[5];
                return flags.HasFlag(tooltipField);
            }

            protected unsafe bool GetTooltipVisibility(ItemTooltipField tooltipField)
            {
                return RaptureAtkModule.Instance()->AtkArrayDataHolder.GetNumberArrayData(29)->IntArray[(int)tooltipField] == 0;
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
                    Service.Log.Error(ex.Message);
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
                    Service.Log.Error(ex, "Failed to set tooltip string");
                }
            }
            protected InventoryItem Item => HoveredItem;
        }

        private unsafe delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);
        [Signature("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 89 9C 24 ?? ?? ?? ?? 4C 89 A4 24 ?? ?? ?? ??", DetourName = nameof(ItemHoveredDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<ItemHoveredDelegate>? _itemHoveredHook = null;

        private unsafe delegate void* GenerateItemTooltip(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
        [Signature("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 48 8B 42 28", DetourName = nameof(GenerateItemTooltipDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<GenerateItemTooltip>? _generateItemTooltipHook = null;

        public TooltipService(IGameInteropProvider gameInteropProvider, ILogger<TooltipService> logger, IFramework framework)
        {
            _logger = logger;
            framework.RunOnFrameworkThread(() =>
            {
                gameInteropProvider.InitializeFromAttributes(this);
                _generateItemTooltipHook?.Enable();
            });

            _logger.LogDebug("Creating {type} ({this})", GetType().Name, this);

        }


        public static InventoryItem HoveredItem { get; private set; }

        public unsafe byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerid, ushort* slotid, IntPtr a5, uint slotidint, IntPtr a7) {
            var returnValue = _itemHoveredHook!.Original(a1, a2, containerid, slotid, a5, slotidint, a7);
            HoveredItem = *(InventoryItem*) (a7);
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

        public unsafe void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
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
                        Service.Log.Error(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Service.Log.Error(ex.Message);
            }
            return _generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
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