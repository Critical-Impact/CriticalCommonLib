using CriticalCommonLib.Crafting;

namespace CriticalCommonLib.Extensions;

public static class CraftGroupTypeExtension
{
    public static string FormattedName(this CraftGroupType craftGroupType)
    {
        switch (craftGroupType)
        {
            case CraftGroupType.Crystals:
                return "Crystals";
            case CraftGroupType.Currency:
                return "Currency";
            case CraftGroupType.Output:
                return "Output";
            case CraftGroupType.Precraft:
                return "Precraft";
            case CraftGroupType.EverythingElse:
                return "Gather/Buy";
            case CraftGroupType.Retrieve:
                return "Retrieve";
        }

        return "Unknown";
    }
    public static string FormattedName(this PrecraftGroupSetting precraftGroupSetting)
    {
        switch (precraftGroupSetting)
        {
            case PrecraftGroupSetting.ByDepth:
                return "By Depth";
            case PrecraftGroupSetting.Together:
                return "Together";
            case PrecraftGroupSetting.ByClass:
                return "By Class";
        }

        return "Unknown";
    }
}