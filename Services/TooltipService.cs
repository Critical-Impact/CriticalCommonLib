using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using Dalamud.Game.Text.SeStringHandling;
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
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly ILogger<TooltipService> _logger;
        private readonly IFramework _framework;
        private List<TooltipTweak> _tooltipTweaks = new();

        public void AddTooltipTweak(TooltipTweak tooltipTweak)
        {
            _tooltipTweaks.Add(tooltipTweak);
        }

        public abstract class TooltipTweak
        {
            public abstract bool IsEnabled { get; }
            protected static unsafe ItemTooltipFieldVisibility GetTooltipVisibility(int** numberArrayData)
            {
                return (ItemTooltipFieldVisibility)RaptureAtkModule.Instance()->AtkArrayDataHolder.GetNumberArrayData(29)->IntArray[19];
            }

            public virtual unsafe void OnGenerateItemTooltip(NumberArrayData* numberArrayData,
                StringArrayData* stringArrayData)
            {
            }

            protected static unsafe SeString?
                GetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field) =>
                GetTooltipString(stringArrayData, (int)field);

        protected static unsafe SeString? GetTooltipString(StringArrayData* stringArrayData, int field) {
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

            protected static unsafe void SetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field, SeString seString) {
                try {
                    seString ??= new SeString();

                    var bytes = seString.Encode().ToList();
                    bytes.Add(0);
                    stringArrayData->SetValue((int)field, bytes.ToArray(), false, true, false);
                } catch (Exception ex) {
                    Service.Log.Error(ex, "Failed to set tooltip string");
                }
            }
            protected static InventoryItem Item => HoveredItem;
        }

        private unsafe delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);
        [Signature("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 89 9C 24 ?? ?? ?? ?? 4C 89 A4 24 ?? ?? ?? ??", DetourName = nameof(ItemHoveredDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<ItemHoveredDelegate>? _itemHoveredHook = null;

        private unsafe delegate void* GenerateItemTooltip(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
        [Signature("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 48 8B 42 28", DetourName = nameof(GenerateItemTooltipDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<GenerateItemTooltip>? _generateItemTooltipHook = null;

        public TooltipService(IGameInteropProvider gameInteropProvider, ILogger<TooltipService> logger, IFramework framework)
        {
            _gameInteropProvider = gameInteropProvider;
            _logger = logger;
            _framework = framework;
            _framework.RunOnFrameworkThread(() =>
            {
                _gameInteropProvider.InitializeFromAttributes(this);
                _generateItemTooltipHook?.Enable();
            });

            Service.GameGui.HoveredItemChanged += GuiOnHoveredItemChanged;
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
            Service.GameGui.HoveredItemChanged -= GuiOnHoveredItemChanged;
            _itemHoveredHook?.Dispose();
            _generateItemTooltipHook?.Dispose();
        }

        //Track the last item they hovered, if they have nothing hovered and goto hover something, it'll fire twice, otherwise it'll fire once
        private ulong lastItem;
        private bool blockItemTooltip;
        private void GuiOnHoveredItemChanged(object? sender, ulong e)
        {
            if (lastItem == 0 && e != 0)
            {
                blockItemTooltip = true;
                lastItem = e;
            }
            else if (lastItem != 0 && e == 0)
            {
                blockItemTooltip = true;
                lastItem = e;
            }
            else
            {
                blockItemTooltip = false;
                lastItem = e;
            }
        }
        public unsafe void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
        {
            if(!blockItemTooltip)
            {
                try
                {
                    foreach (var t in _tooltipTweaks)
                    {
                        try
                        {
                            t.OnGenerateItemTooltip(numberArrayData, stringArrayData);
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
            }
            else
            {
                blockItemTooltip = false;
            }
            return _generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
        }

        public enum ItemTooltipField : byte {
            ItemName,
            GlamourName,
            ItemUiCategory,
            ItemDescription = 13,
            Effects = 16,
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