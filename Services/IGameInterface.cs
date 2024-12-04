using System;
using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Rows;


namespace CriticalCommonLib.Services
{
    public interface IGameInterface : IDisposable
    {
        public bool IsGatheringItemGathered(uint gatheringItemId);
        public bool? IsItemGathered(uint itemId);
        unsafe void OpenGatheringLog(uint itemId);
        unsafe void OpenFishingLog(uint itemId, bool isSpearFishing);
        unsafe bool IsInArmoire(uint itemId);
        uint? ArmoireIndexIfPresent(uint itemId);
        unsafe bool OpenCraftingLog(uint itemId);
        unsafe bool OpenCraftingLog(uint itemId, uint recipeId);
    }
}