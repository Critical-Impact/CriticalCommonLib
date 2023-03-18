using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class PlaceNameEx : PlaceName
{
    private string? _formattedName;
    
    public string FormattedName
    {
        get
        {
            if (_formattedName == null)
            {
                _formattedName = Name.ToDalamudString().ToString();
            }

            return _formattedName;
        }
    }
}