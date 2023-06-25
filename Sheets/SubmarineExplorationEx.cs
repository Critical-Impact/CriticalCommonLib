using System.Collections.Generic;
using System.Linq;
using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class SubmarineExplorationEx : SubmarineExploration
{
    private bool _unlockCalculated = false;
    private SubmarineUnlockEx? _submarineUnlock;

    public SubmarineUnlockEx? SubmarineUnlock => _submarineUnlock;
    
    public List<LazyRow<ItemEx>> Drops; 
    public LazyRow<SubmarineExplorationEx> UnlockPointEx;

    private string? _formattedName;
    
    public string FormattedName
    {
        get
        {
            if (_formattedName == null)
            {
                _formattedName = Destination.ToDalamudString().ToString();
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
                _formattedNameShort = Location.ToDalamudString().ToString();
            }

            return _formattedNameShort;
        }
    }
    
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        _submarineUnlock = Service.ExcelCache.GetSubmarineUnlock(RowId);

        Drops = new List<LazyRow<ItemEx>>();
        var drops = Service.ExcelCache.SubmarineDrops.Where(c => c.SubmarineExplorationId == RowId).ToList();
        foreach (var drop in drops)
        {
            Drops.Add(new LazyRow<ItemEx>(gameData, drop.ItemId, language));
        }

        if (_submarineUnlock != null)
        {
            UnlockPointEx = new LazyRow<SubmarineExplorationEx>(gameData, _submarineUnlock.SubmarineExplorationUnlock.Row, language);
        }
        else
        {
            UnlockPointEx = new LazyRow<SubmarineExplorationEx>(gameData, 0, language);
        }
    }
}