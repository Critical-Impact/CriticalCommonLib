using System;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Extensions
{
    public static class RecipeLevelTableExtension
    {
        public static uint ProgressRequired(this RecipeLevelTable recipeLevelTable, Recipe? recipe)
        {
            if (recipe == null)
            {
                return 0;
            }
            return (uint)Math.Floor((double)recipeLevelTable.Difficulty * ((double)recipe.DifficultyFactor / 100.0));
        }
    }
}