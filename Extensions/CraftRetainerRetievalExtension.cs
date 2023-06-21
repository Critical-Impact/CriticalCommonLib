using CriticalCommonLib.Crafting;
using CriticalCommonLib.Sheets;

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
                case CraftRetainerRetrieval.HQOnly:
                    return "HQ Only";
            }
            return craftRetainerRetrieval.ToString();
        }
    }
}