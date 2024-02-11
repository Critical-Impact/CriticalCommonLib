using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using Dalamud.Utility;
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
    private List<LazyRow<BNpcBaseEx>>? _relatedBases;
    public List<LazyRow<BNpcBaseEx>> RelatedBases
    {
        get
        {
            if (_relatedBases == null)
            {
                _relatedBases = MobSpawns.Select(c => c.BNpcBaseEx).DistinctBy(c => c.Row).ToList();
            }

            return _relatedBases;
        }
    }

    private string? _mobTypes;
    public string MobTypes
    {
        get
        {
            if (_mobTypes == null)
            {
                _mobTypes = String.Join(",", RelatedBases.Select(lazyRow => lazyRow.Value!.NpcType.ToString()).Distinct());
            }

            return _mobTypes;
        }
    }

    public string? GarlandToolsId
    {
        get
        {
            if (_relatedBases == null || _relatedBases.Count == 0)
            {
                return null;
            }

            return _relatedBases.First().Row + "0000000" + RowId;
        }
    }
    private List<MobDropEx>? _mobDrops;
    public List<MobDropEx> MobDrops
    {
        get
        {
            if (_mobDrops == null)
            {
                _mobDrops = Service.ExcelCache.GetMobDropsByBNpcNameId(RowId) ?? new();
            }
            return _mobDrops;
        }
    }

    public NotoriousMonster? NotoriousMonster => Service.ExcelCache.GetNotoriousMonster(RowId);
}