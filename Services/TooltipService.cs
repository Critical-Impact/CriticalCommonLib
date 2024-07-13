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
using FFXIVClientStructs.FFXIV.Component.GUI;
using Microsoft.Extensions.Logging;

namespace CriticalCommonLib.Services
{
    public class TooltipService : ITooltipService
    {
        private readonly IGameInteropProvider _gameInteropProvider;
        private readonly ILogger<TooltipService> _logger;
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
                return (ItemTooltipFieldVisibility)(*(*(numberArrayData + 4) + 5));
            }

            public virtual unsafe void OnActionTooltip(AtkUnitBase* addonActionDetail, HoveredActionDetail action)
            {
            }

            public virtual unsafe void OnGenerateItemTooltip(NumberArrayData* numberArrayData,
                StringArrayData* stringArrayData)
            {
            }

            public virtual unsafe void OnGenerateActionTooltip(NumberArrayData* numberArrayData,
                StringArrayData* stringArrayData)
            {
            }

            protected static unsafe SeString?
                GetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field) =>
                GetTooltipString(stringArrayData, (int)field);

            protected static unsafe SeString? GetTooltipString(StringArrayData* stringArrayData,
                ActionTooltipField field) => GetTooltipString(stringArrayData, (int)field);

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


            protected static unsafe void SetTooltipString(StringArrayData* stringArrayData, ActionTooltipField field, SeString seString) {
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
            protected static HoveredActionDetail Action => HoveredAction;
        }

        private unsafe delegate IntPtr ActionTooltipDelegate(AtkUnitBase* a1, void* a2, ulong a3);
        [Signature("48 89 5C 24 ?? 55 56 57 41 56 41 57 48 83 EC ?? 48 8B 9A", DetourName = nameof(ActionTooltipDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<ActionTooltipDelegate>? _actionTooltipHook = null;

        private unsafe delegate byte ItemHoveredDelegate(IntPtr a1, IntPtr* a2, int* containerId, ushort* slotId, IntPtr a5, uint slotIdInt, IntPtr a7);
        [Signature("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 89 9C 24 ?? ?? ?? ?? 48 89 B4 24", DetourName = nameof(ItemHoveredDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<ItemHoveredDelegate>? _itemHoveredHook = null;
            
        private delegate void ActionHoveredDelegate(ulong a1, int a2, uint a3, int a4, byte a5);
        [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 83 F8 ?? 75 ?? BA ?? ?? ?? ?? E8 ?? ?? ?? ?? 49 8D 4F ?? 48 8B F8", DetourName = nameof(ActionHoveredDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<ActionHoveredDelegate>? _actionHoveredHook = null;

        private unsafe delegate void* GenerateItemTooltip(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
        [Signature("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 48 8B 42 20", DetourName = nameof(GenerateItemTooltipDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<GenerateItemTooltip>? _generateItemTooltipHook = null;
        
        private unsafe delegate void* GenerateActionTooltip(AtkUnitBase* addonActionDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
        
        [Signature("E8 ?? ?? ?? ?? 48 8B 43 ?? 48 8B 9F", DetourName = nameof(GenerateActionTooltipDetour), UseFlags = SignatureUseFlags.Hook)]
        private Hook<GenerateActionTooltip>? _generateActionTooltipHook = null;

        public TooltipService(IGameInteropProvider gameInteropProvider, ILogger<TooltipService> logger)
        {
            _gameInteropProvider = gameInteropProvider;
            _logger = logger;
            _gameInteropProvider.InitializeFromAttributes(this);
            Service.GameGui.HoveredItemChanged += GuiOnHoveredItemChanged;
            _generateItemTooltipHook?.Enable();
            _logger.LogDebug("Creating {type} ({this})", GetType().Name, this);

        }

        public class HoveredActionDetail {
            public int Category;
            public uint Id;
            public int Unknown3;
            public byte Unknown4;
        }

        public static readonly HoveredActionDetail HoveredAction = new HoveredActionDetail();

        public void ActionHoveredDetour(ulong a1, int a2, uint a3, int a4, byte a5) {
            HoveredAction.Category = a2;
            HoveredAction.Id = a3;
            HoveredAction.Unknown3 = a4;
            HoveredAction.Unknown4 = a5;
            _actionHoveredHook?.Original(a1, a2, a3, a4, a5);
        }

        public unsafe IntPtr ActionTooltipDetour(AtkUnitBase* addon, void* a2, ulong a3) {
            var retVal = _actionTooltipHook!.Original(addon, a2, a3);
            try {
                foreach (var t in _tooltipTweaks) {
                    try {
                        t.OnActionTooltip(addon, HoveredAction);
                    } catch (Exception ex) {
                        Service.Log.Error(ex.Message);
                    }
                }
            } catch (Exception ex) {
                Service.Log.Error(ex.Message);
            }
            return retVal;
        }
            
        public static InventoryItem HoveredItem { get; private set; }

        public unsafe byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerid, ushort* slotid, IntPtr a5, uint slotidint, IntPtr a7) {
            var returnValue = _itemHoveredHook!.Original(a1, a2, containerid, slotid, a5, slotidint, a7);
            HoveredItem = *(InventoryItem*) (a7);
            return returnValue;
        }

        public void Disable() {
            _itemHoveredHook?.Disable();
            _actionTooltipHook?.Disable();
            _actionHoveredHook?.Disable();
            _generateItemTooltipHook?.Disable();
            _generateActionTooltipHook?.Disable();
        }

        public void Dispose() {
            _logger.LogDebug("Disposing {type} ({this})", GetType().Name, this);
            Service.GameGui.HoveredItemChanged -= GuiOnHoveredItemChanged;
            _itemHoveredHook?.Dispose();
            _actionTooltipHook?.Dispose();
            _actionHoveredHook?.Dispose();
            _generateItemTooltipHook?.Dispose();
            _generateActionTooltipHook?.Dispose();
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

        public unsafe void* GenerateActionTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData) {
            try {
                foreach (var t in _tooltipTweaks) {
                    try {
                        t.OnGenerateActionTooltip(numberArrayData, stringArrayData);
                    } catch (Exception ex) {
                        Service.Log.Error(ex.Message);
                    }
                }
            } catch (Exception ex) {
                Service.Log.Error(ex.Message);
            }
            return _generateActionTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
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

        public enum ActionTooltipField {
            ActionName,
            ActionKind,
            Unknown02,
            RangeText,
            RangeValue,
            RadiusText,
            RadiusValue,
            MPCostText,
            MPCostValue,
            RecastText,
            RecastValue,
            CastText,
            CastValue,
            Description,
            Level,
            ClassJobAbbr,
        }
    }
}