using System;
using CriticalCommonLib.Crafting;

namespace CriticalCommonLib.Extensions
{
    public static class CraftRetainerRetrievalExtension
    {
        public static string FormattedName(this CraftRetainerRetrieval craftRetainerRetrieval)
        {
            switch (craftRetainerRetrieval)
            {
                case CraftRetainerRetrieval.Yes:
                    return "Yes";
                case CraftRetainerRetrieval.No:
                    return "No";
                case CraftRetainerRetrieval.HqOnly:
                    return "HQ Only";
                case CraftRetainerRetrieval.NqOnly:
                    return "NQ Only";
                case CraftRetainerRetrieval.CollectableOnly:
                    return "Collectable Only";
            }
            return craftRetainerRetrieval.ToString();
        }
    }
}