using CriticalCommonLib.Interfaces;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class MobSpawnPositionEx : MobSpawnPosition, ILocation
{
    private LazyRow<TerritoryTypeEx> _territoryTypeEx = null!;
    private LazyRow<MapEx> _mapEx = null!;
    private LazyRow<PlaceNameEx> _placeNameEx = null!;
    private LazyRow<BNpcNameEx> _bNpcNameEx = null!;
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        _territoryTypeEx = new LazyRow<TerritoryTypeEx>(gameData, TerritoryTypeId, language);
        _mapEx = _territoryTypeEx.Value?.MapEx ?? new LazyRow<MapEx>(gameData, 0, language);
        _placeNameEx = _territoryTypeEx.Value?.PlaceNameEx ?? new LazyRow<PlaceNameEx>(gameData, 0, language);
        _bNpcNameEx = new LazyRow<BNpcNameEx>(gameData, BNpcName.Row, language);
    }

    private string? _formattedPosition;
    public string FormattedPosition => _formattedPosition ??= Position.X + " , " + Position.Y;

    private string? _formattedId;
    public string FormattedId => _formattedId ??= Position.X + "," + Position.Y + "," + Position.Z;

    private string? _formattedName;

    public string FormattedName =>
        _formattedName ??= (PlaceNameEx.Value?.FormattedName ?? "Unknown") + " - " + FormattedPosition;

    public double MapX => Position.X;
    public double MapY => Position.Y;
    public LazyRow<MapEx> MapEx => _mapEx;
    public LazyRow<PlaceNameEx> PlaceNameEx => _placeNameEx;
    public LazyRow<TerritoryTypeEx> TerritoryTypeEx => _territoryTypeEx;
    public LazyRow<BNpcNameEx> BNpcNameEx => _bNpcNameEx;
}