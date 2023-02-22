using CriticalCommonLib.Extensions;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class BNpcNameEx : BNpcName
{
    private string? _formattedName;
    public string FormattedName => _formattedName ??= Singular.ToDalamudString().ToString().ToTitleCase();
}