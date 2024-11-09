using AllaganLib.GameSheets.Model;


namespace CriticalCommonLib.Extensions
{
    public static class RetainerTaskTypExtensions
    {
        public static string FormattedName(this RetainerTaskType retainerTaskType)
        {
            switch (retainerTaskType)
            {
                case RetainerTaskType.Botanist:
                    return "Botany";
                case RetainerTaskType.FieldExploration:
                    return "Field Exploration";
                case RetainerTaskType.HighlandExploration:
                    return "Highland Exploration";
                case RetainerTaskType.WatersideExploration:
                    return "Waterside Exploration";
                case RetainerTaskType.WoodlandExploration:
                    return "Woodland Exploration";
                case RetainerTaskType.Fishing:
                    return "Fishing";
                case RetainerTaskType.Hunting:
                    return "Hunting";
                case RetainerTaskType.Mining:
                    return "Mining";
                case RetainerTaskType.QuickExploration:
                    return "Quick Exploration";
                case RetainerTaskType.Unknown:
                    return "Unknown";
            }
            return retainerTaskType.ToString();
        }
    }
}