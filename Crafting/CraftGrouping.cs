using System.Collections.Generic;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Crafting;

public class CraftGrouping
{
    private CraftGroupType _craftGroupType;

    public CraftGroupType CraftGroupType => _craftGroupType;

    public uint? Depth => _depth;
    public uint? ClassJobId => _classJobId;
    public uint? MapId => _mapId;

    public List<CraftItem> CraftItems => _craftItems;

    private uint? _depth;
    private uint? _classJobId;
    private uint? _mapId;
    private List<CraftItem> _craftItems;

    public CraftGrouping(CraftGroupType craftGroupType, List<CraftItem> craftItems, uint? depth = null, uint? classJobId = null, uint? mapId = null)
    {
        _craftGroupType = craftGroupType;
        _depth = depth;
        _craftItems = craftItems;
        _classJobId = classJobId;
        _mapId = mapId;
    }

    public string FormattedName()
    {
        var name = _craftGroupType.FormattedName();
        if (_depth != null)
        {
            name = _depth.Value.ConvertToOrdinal() + " Tier " + name;
        }

        if (_classJobId != null)
        {
            var classJob = Service.ExcelCache.GetCraftTypeSheet().GetRow(_classJobId.Value);
            if (classJob != null)
            {
                name = classJob.FormattedName + " - " + name;
            }
        }

        if (_mapId != null)
        {
            var map = Service.ExcelCache.GetMapSheet().GetRow(_mapId.Value);
            if (map != null)
            {
                name = map.FormattedName;
            }
        }

        return name;
    }
}