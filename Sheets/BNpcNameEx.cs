using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class BNpcNameEx : BNpcName
{
    private string? _formattedName;
    public string FormattedName => _formattedName ??= Singular.ToDalamudString().ToString().ToTitleCase();

    private List<MobSpawnPositionEx>? _mobSpawns;
    public List<MobSpawnPositionEx> MobSpawns
    {
        get
        {
            if (_mobSpawns == null)
            {
                _mobSpawns = Service.ExcelCache.GetMobSpawns(RowId);
                if (_mobSpawns == null)
                {
                    _mobSpawns = new List<MobSpawnPositionEx>();
                }
            }

            return _mobSpawns;
        }
    }
}