using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class CraftTypeEx : CraftType
{
    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString();

    private string? _formattedName;
    public ushort Icon;
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        Icon = (ushort)(RowId + 62502);
    }
}