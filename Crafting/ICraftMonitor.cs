using System;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Agents;

namespace CriticalCommonLib.Crafting
{
    public interface ICraftMonitor: IDisposable
    {
        event CraftMonitor.CraftStartedDelegate? CraftStarted;
        event CraftMonitor.CraftFailedDelegate? CraftFailed;
        event CraftMonitor.CraftCompletedDelegate? CraftCompleted;
        CraftingAgent? Agent { get; }
        SimpleCraftingAgent? SimpleAgent { get; }
        RecipeRow? CurrentRecipe { get; }
        RecipeLevelTableRow? RecipeLevelTable { get; }
        uint CraftType { get; }
    }
}