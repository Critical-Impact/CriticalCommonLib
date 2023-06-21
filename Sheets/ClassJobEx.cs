using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class ClassJobEx : ClassJob
{
    private string? _formattedName;

    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString();
}