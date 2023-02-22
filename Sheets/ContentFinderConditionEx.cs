using System;
using System.Collections.Generic;
using CriticalCommonLib.Extensions;
using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class ContentFinderConditionEx : ContentFinderCondition
{
    private string? _formattedName;
    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString().ToTitleCase();

    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        AcceptClassJobCategoryEx = new LazyRow<ClassJobCategoryEx>(gameData, AcceptClassJobCategory.Row, language);
    }
    
    public LazyRow<ClassJobCategoryEx> AcceptClassJobCategoryEx { get; set; } = null!;
    
    private string? _roulettes;
    public string Roulettes
    {
        get
        {
            if (_roulettes != null)
            {
                return _roulettes;
            }
            List<string> roulettes = new List<string>();
            if (LevelingRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(1)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (HighLevelRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(2)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (MSQRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(3)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (GuildHestRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(4)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (ExpertRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(5)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (TrialRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(6)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (DailyFrontlineChallenge)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(7)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (LevelCapRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(8)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (MentorRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(9)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (AllianceRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(15)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }
            if (NormalRaidRoulette)
            {
                roulettes.Add(Service.ExcelCache.GetContentRouletteSheet().GetRow(17)?.Category.ToDalamudString().ToString() ?? "Unknown");
            }

            _roulettes = String.Join(", ", roulettes);
            return _roulettes;
        }
    }
}