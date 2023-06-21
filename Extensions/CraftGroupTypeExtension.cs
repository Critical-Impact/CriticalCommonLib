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
            case CraftGroupType.Gathering:
                return "Gather/Buy";
            case CraftGroupType.Output:
                return "Output";
            case CraftGroupType.Precraft:
                return "Precraft";
            case CraftGroupType.EverythingElse:
                return "Gather/Buy";
        }

        return "Unknown";
    }
}