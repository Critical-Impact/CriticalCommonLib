using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class ClassJobEx : ClassJob
{
    private string? _formattedName;
    private string? _formattedNameEnglish;

    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString();
    public string FormattedNameEnglish => _formattedNameEnglish ??= NameEnglish.ToDalamudString().ToString();

    public int Icon => (int)(62000 + RowId);
}