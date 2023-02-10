using System;
using System.Collections.Generic;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Services
{
    public interface IGameInterface : IDisposable
    {
        event GameInterface.AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;
        HashSet<uint> AcquiredItems { get; set; }
        unsafe void OpenGatheringLog(uint itemId);
        unsafe bool HasAcquired(ItemEx item, bool debug = false);
        unsafe bool IsInArmoire(uint itemId);
        uint? ArmoireIndexIfPresent(uint itemId);
        unsafe void OpenCraftingLog(uint itemId);
        unsafe void OpenCraftingLog(uint itemId, uint recipeId);
    }
}