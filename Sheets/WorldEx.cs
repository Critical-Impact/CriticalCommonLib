using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class WorldEx : World
{
    private string? _formattedName;
    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString();
}