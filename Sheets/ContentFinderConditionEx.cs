using CriticalCommonLib.Extensions;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class ContentFinderConditionEx : ContentFinderCondition
{
    public string FormattedName => Name.ToDalamudString().ToString().ToTitleCase();
}