using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AllaganLib.GameSheets.Model;
using CriticalCommonLib.Interfaces;
using Lumina.Excel.Sheets;


namespace CriticalCommonLib.Extensions;

public static class LocationExtension
{
    public static Aetheryte? GetNearestAetheryte(this ILocation location)
    {
        if (location.Map.ValueNullable == null)
        {
            return null;
        }
        var map = location.Map.Value;
        var nearestAetheryteId = Service.Data.GetSubrowExcelSheet<MapMarker>()!
            .SelectMany(c => c)
            .Where(x => x.DataType == 3 && x.RowId == map.MapMarkerRange)
            .Select(
                marker => new
                {
                    distance = Vector2.DistanceSquared(
                        new Vector2((float)location.MapX, (float)location.MapY),
                        ConvertLocationToRaw(marker.X, marker.Y, map.SizeFactor)),
                    rowId = marker.DataKey.RowId
                })
            .MinBy(x => x.distance);

        if (nearestAetheryteId != null)
        {
            // Support the unique case of aetheryte not being in the same map
            var nearestAetheryte = location.TerritoryType.RowId == 399
                ? map.TerritoryType.ValueNullable?.Aetheryte.ValueNullable
                : Service.Data.GetExcelSheet<Aetheryte>().FirstOrDefault(x =>
                    x.IsAetheryte && x.Territory.RowId == location.TerritoryType.RowId && x.RowId == nearestAetheryteId.rowId);
            return nearestAetheryte;
        }
        else if (ZonesWithoutAetherytes.ContainsKey(location.TerritoryType.RowId))
        {
            var alternateAetheryte = Service.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(ZonesWithoutAetherytes[location.TerritoryType.RowId]);
            if (alternateAetheryte != null)
            {
                return alternateAetheryte;
            }
        }

        return null;
    }

    public static readonly Dictionary<uint,uint> ZonesWithoutAetherytes = new()
    {
        {128u, 8u},   // Limsa Upper Decks -> Limsa
        {900u, 8u},   // The Endeavor -> Limsa
        {901u, 70u},  // The Diadem -> Foundation
        {929u, 70u},  // The Diadem -> Foundation
        {939u, 70u},  // The Diadem -> Foundation
        {399u, 75u},  // The Dravanian Hinterlands -> Idyllshire
        {133u, 2u},   // Old Gridania -> New Gridania,
        {339u, 8u},   // Mist -> Limsa
        {340u, 2u},   // Lavender Beds -> New Gridania
        {341u, 9u},   // Goblet -> Ul'dah
        {641u, 111u}, // Shirogane -> Kugane
    };

    private static Vector2 ConvertLocationToRaw(int x, int y, float scale)
    {
        var num = scale / 100f;
        return new Vector2(ConvertRawToMap((int)((x - 1024) * num * 1000f), scale), ConvertRawToMap((int)((y - 1024) * num * 1000f), scale));
    }

    private static float ConvertRawToMap(int pos, float scale)
    {
        var num1 = scale / 100f;
        var num2 = (float)(pos * (double)num1 / 1000.0f);
        return (40.96f / num1 * ((num2 + 1024.0f) / 2048.0f)) + 1.0f;
    }
}