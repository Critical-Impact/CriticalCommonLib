using System.Linq;
using Dalamud.Utility;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class GatheringTypeEx : GatheringType
{
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        GatheringPointBases = new LazyRelated<GatheringPointBaseEx, GatheringTypeEx>(this, gameData, language,
            types =>
            {
                return types.GroupBy(c => c.GatheringType.Row)
                    .ToDictionary(c => c.Key, c => c.Select(c => c.RowId).ToList());
            }, "GPB");
    }

    public LazyRelated<GatheringPointBaseEx, GatheringTypeEx> GatheringPointBases { get; set; } = null!;

    private string? _formattedName;

    public string FormattedName => _formattedName ??= Name.ToDalamudString().ToString();
}