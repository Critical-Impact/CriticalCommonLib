using System.Collections.Generic;
using System.Linq;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class MobDropEx : MobDrop
{
    public LazyRow<BNpcNameEx> BNpcNameEx = null!;

    private List<MobSpawnPositionEx>? _mobSpawnPositions;
    public List<MobSpawnPositionEx> MobSpawnPositions
    {
        get
        {
            if (_mobSpawnPositions == null)
            {
                _mobSpawnPositions = Service.ExcelCache.GetMobSpawns(BNpcNameId);
                if (_mobSpawnPositions == null)
                {
                    _mobSpawnPositions = new List<MobSpawnPositionEx>();
                }
            }

            return _mobSpawnPositions;
        }
    }
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        BNpcNameEx = new LazyRow<BNpcNameEx>(gameData, BNpcNameId, language);
    }
    
    private Dictionary<TerritoryType, List<MobSpawnPositionEx>>? _groupedMobSpawns;

    public Dictionary<TerritoryType, List<MobSpawnPositionEx>> GroupedMobSpawns
    {
        get
        {
            if (_groupedMobSpawns == null)
            {
                _groupedMobSpawns = MobSpawnPositions.Where(c => c.TerritoryType != null).GroupBy(c => c.TerritoryType.Value!).ToDictionary(c => c.Key, c => c.ToList()).Where(c => c.Value.Count != 0).ToDictionary(c => c.Key, c => c.Value);
            }
            return _groupedMobSpawns;
        }
    }
    
}