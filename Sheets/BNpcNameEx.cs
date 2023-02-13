using CriticalCommonLib.Extensions;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class BNpcNameEx : BNpcName
{
    public string FormattedName => Singular.ToDalamudString().ToString().ToTitleCase();
}