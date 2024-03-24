using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

public interface ITooltipService : IDisposable
{
    void AddTooltipTweak(TooltipService.TooltipTweak tooltipTweak);
    void ActionHoveredDetour(ulong a1, int a2, uint a3, int a4, byte a5);
    unsafe IntPtr ActionTooltipDetour(AtkUnitBase* addon, void* a2, ulong a3);
    unsafe byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerid, ushort* slotid, IntPtr a5, uint slotidint, IntPtr a7);
    void Disable();
    unsafe void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
    unsafe void* GenerateActionTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
}