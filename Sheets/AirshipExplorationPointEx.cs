using System.Collections.Generic;
using System.Linq;
using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class AirshipExplorationPointEx : AirshipExplorationPoint
{
    private AirshipUnlockEx? _airshipUnlockEx;

    public AirshipUnlockEx? AirshipUnlockEx => _airshipUnlockEx;
    
    public List<LazyRow<ItemEx>> Drops = null!; 
    public LazyRow<AirshipExplorationPointEx> UnlockPointEx = null!;

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

    private string? _formattedNameShort;
    
    public string FormattedNameShort
    {
        get
        {
            if (_formattedNameShort == null)
            {
                _formattedNameShort = NameShort.ToDalamudString().ToString();
            }

            return _formattedNameShort;
        }
    }
    
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        _airshipUnlockEx = Service.ExcelCache.GetAirshipUnlock(RowId);

        Drops = new List<LazyRow<ItemEx>>();

        if (Service.ExcelCache.AirshipDrops != null)
        {
            var drops = Service.ExcelCache.AirshipDrops.Where(c => c.AirshipExplorationPointId == RowId).ToList();
            foreach (var drop in drops)
            {
                Drops.Add(new LazyRow<ItemEx>(gameData, drop.ItemId, language));
            }
        }

        if (_airshipUnlockEx != null)
        {
            UnlockPointEx = new LazyRow<AirshipExplorationPointEx>(gameData, _airshipUnlockEx.AirshipExplorationPointUnlock.Row, language);
        }
        else
        {
            UnlockPointEx = new LazyRow<AirshipExplorationPointEx>(gameData, 0, language);
        }
    }
    
    
}