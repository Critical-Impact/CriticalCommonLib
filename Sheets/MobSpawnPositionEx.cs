using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class MobSpawnPositionEx : MobSpawnPosition
{
    public LazyRow<TerritoryTypeEx> TerritoryTypeEx;
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        TerritoryTypeEx = new LazyRow<TerritoryTypeEx>(gameData, TerritoryTypeId, language);
    }

    private string? _formattedPosition;
    public string FormattedPosition => _formattedPosition ??= Position.X + " , " + Position.Y;

    private string? _formattedId;
    public string FormattedId => _formattedId ??= Position.X + "," + Position.Y + "," + Position.Z;
}