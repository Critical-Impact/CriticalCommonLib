using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Sheets;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models;

public class GenericMapLocation : ILocation
{
    private double _mapX;
    private double _mapY;
    private LazyRow<MapEx> _mapEx;
    private LazyRow<PlaceNameEx> _placeNameEx;
    private LazyRow<TerritoryTypeEx> _territoryTypeEx;

    public GenericMapLocation(double mapX, double mapY, LazyRow<MapEx> mapEx, LazyRow<PlaceNameEx> placeName, LazyRow<TerritoryTypeEx> territoryTypeEx)
    {
        _mapX = mapX;
        _mapY = mapY;
        _mapEx = mapEx;
        _placeNameEx = placeName;
        _territoryTypeEx = territoryTypeEx;
    }

    public double MapX => _mapX;

    public double MapY => _mapY;

    public LazyRow<MapEx> MapEx => _mapEx;

    public LazyRow<PlaceNameEx> PlaceNameEx => _placeNameEx;
    public LazyRow<TerritoryTypeEx> TerritoryTypeEx => _territoryTypeEx;
}