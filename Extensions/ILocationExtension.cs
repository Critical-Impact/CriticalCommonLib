using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib.Interfaces;
using Dalamud.Logging;

namespace CriticalCommonLib.Extensions;

public static class LocationExtension
{
    public static void TeleportToNearestAetheryte(this ILocation location)
    {
        if (location.MapEx.Value == null)
        {
            return;
        }
        var map = location.MapEx.Value;
        var nearestAetheryteId = Service.ExcelCache.GetMapMarkerSheet()
            .Where(x => x.DataType == 3 && x.RowId == map.MapMarkerRange)
            .Select(
                marker => new
                {
                    distance = Vector2.DistanceSquared(
                        new Vector2((float)location.MapX, (float)location.MapY),
                        ConvertLocationToRaw(marker.X, marker.Y, map.SizeFactor)),
                    rowId = marker.DataKey
                })
            .MinBy(x => x.distance);
        if (nearestAetheryteId != null)
        {
            // Support the unique case of aetheryte not being in the same map
            var nearestAetheryte = location.TerritoryTypeEx.Row == 399
                ? map.TerritoryType?.Value?.Aetheryte.Value
                : Service.ExcelCache.GetAetheryteSheet().FirstOrDefault(x =>
                    x.IsAetheryte && x.Territory.Row == location.TerritoryTypeEx.Row && x.RowId == nearestAetheryteId.rowId);

            if (nearestAetheryte == null)
                return;
            PluginLog.Verbose("Nearest aetheryte is " + nearestAetheryte.PlaceName.Value?.Name.ToString() ?? "Unknown");
            //TeleportConsumer.UseTeleport(nearestAetheryte.RowId);
        }
        else if (ZonesWithoutAetherytes.ContainsKey(location.TerritoryTypeEx.Row))
        {
            var alternateAetheryte = Service.ExcelCache.GetAetheryteSheet().GetRow(ZonesWithoutAetherytes[location.TerritoryTypeEx.Row]);
            if (alternateAetheryte != null)
            {
                PluginLog.Verbose("Nearest aetheryte is " + alternateAetheryte.PlaceName.Value?.Name.ToString() ??
                                  "Unknown");
            }
        }
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