using System;
using System.Collections.Generic;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Services
{
    public interface IGameInterface : IDisposable
    {
        event GameInterface.AcquiredItemsUpdatedDelegate? AcquiredItemsUpdated;
        HashSet<uint> AcquiredItems { get; set; }
        public bool IsGatheringItemGathered(uint gatheringItemId);
        public bool? IsItemGathered(uint itemId);
        unsafe void OpenGatheringLog(uint itemId);
        unsafe void OpenFishingLog(uint itemId, bool isSpearFishing);
        unsafe bool HasAcquired(ItemEx item, bool debug = false);
        unsafe bool IsInArmoire(uint itemId);
        uint? ArmoireIndexIfPresent(uint itemId);
        unsafe bool OpenCraftingLog(uint itemId);
        unsafe bool OpenCraftingLog(uint itemId, uint recipeId);
    }
}