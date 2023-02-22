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
    private LazyRow<PlaceName> _placeName;

    public GenericMapLocation(double mapX, double mapY, LazyRow<MapEx> mapEx, LazyRow<PlaceName> placeName)
    {
        _mapX = mapX;
        _mapY = mapY;
        _mapEx = mapEx;
        _placeName = placeName;
    }

    public double MapX => _mapX;

    public double MapY => _mapY;

    public LazyRow<MapEx> MapEx => _mapEx;

    public LazyRow<PlaceName> PlaceName => _placeName;
}