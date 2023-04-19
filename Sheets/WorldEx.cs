using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class WorldEx : World
{
    private string? _formattedName;
    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString();
}