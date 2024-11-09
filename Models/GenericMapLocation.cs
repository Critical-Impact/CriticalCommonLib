using AllaganLib.GameSheets.Model;
using CriticalCommonLib.Interfaces;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace CriticalCommonLib.Models;

public class GenericMapLocation : ILocation
{
    private double _mapX;
    private double _mapY;
    private RowRef<Map> _mapEx;
    private RowRef<PlaceName> _placeNameEx;
    private RowRef<TerritoryType> _territoryTypeEx;

    public GenericMapLocation(double mapX, double mapY, RowRef<Map> mapEx, RowRef<PlaceName> placeName, RowRef<TerritoryType> territoryTypeEx)
    {
        _mapX = mapX;
        _mapY = mapY;
        _mapEx = mapEx;
        _placeNameEx = placeName;
        _territoryTypeEx = territoryTypeEx;
    }

    public double MapX => _mapX;

    public double MapY => _mapY;

    public RowRef<Map> Map => _mapEx;

    public RowRef<PlaceName> PlaceName => _placeNameEx;
    public RowRef<TerritoryType> TerritoryType => _territoryTypeEx;
}