using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class ENpcResidentEx : ENpcResident
{
    
    private string? _formattedSingular;
    
    public string FormattedSingular
    {
        get
        {
            if (_formattedSingular == null)
            {
                _formattedSingular = Singular.ToDalamudString().ToString();
            }

            return _formattedSingular;
        }
    }
}