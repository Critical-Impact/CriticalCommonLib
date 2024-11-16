using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

public interface ITooltipService : IDisposable
{
    void AddTooltipTweak(TooltipService.TooltipTweak tooltipTweak);
    unsafe byte ItemHoveredDetour(IntPtr a1, IntPtr* a2, int* containerid, ushort* slotid, IntPtr a5, uint slotidint, IntPtr a7);
    void Disable();
    unsafe void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
}