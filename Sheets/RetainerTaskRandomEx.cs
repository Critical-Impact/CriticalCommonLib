using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class RetainerTaskRandomEx :  RetainerTaskRandom
{
    public LazyRow<RetainerTaskEx> RetainerTaskEx { get; set; } = null!;

    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        if (Service.ExcelCache.RetainerTaskToRetainerNormalLookup.ContainsKey(RowId))
        {
            RetainerTaskEx = new LazyRow<RetainerTaskEx>(gameData, Service.ExcelCache.RetainerTaskToRetainerNormalLookup[RowId],
                language);
        }
        else
        {
            RetainerTaskEx = new LazyRow<RetainerTaskEx>(gameData, 0, language);
        }
    }

    private string? _formattedName;
    
    public string FormattedName
    {
        get
        {
            if (_formattedName == null)
            {
                _formattedName = Name.ToDalamudString() + " - Lv " + (RetainerTaskEx.Value?.RetainerLevel.ToString() ?? "Unknown");
            }

            return _formattedName;
        }
    }

    private string? _nameString;
    
    public string NameString
    {
        get
        {
            if (_nameString == null)
            {
                _nameString = Name.ToDalamudString().ToString();
            }

            return _nameString;
        }
    }
}