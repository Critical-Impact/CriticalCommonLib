using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

public interface ITooltipService : IDisposable
{
    void AddTooltipTweak(TooltipService.TooltipTweak tooltipTweak);
    void Disable();
}