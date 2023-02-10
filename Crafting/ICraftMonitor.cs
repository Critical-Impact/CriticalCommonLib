using System;
using CriticalCommonLib.Agents;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Crafting
{
    public interface ICraftMonitor: IDisposable
    {
        event CraftMonitor.CraftStartedDelegate? CraftStarted;
        event CraftMonitor.CraftFailedDelegate? CraftFailed;
        event CraftMonitor.CraftCompletedDelegate? CraftCompleted;
        CraftingAgent? Agent { get; }
        SimpleCraftingAgent? SimpleAgent { get; }
        Recipe? CurrentRecipe { get; }
        RecipeLevelTable? RecipeLevelTable { get; }
    }
}