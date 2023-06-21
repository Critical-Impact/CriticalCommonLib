using System.Collections.Generic;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Crafting;

public class CraftGrouping
{
    private CraftGroupType _craftGroupType;

    public CraftGroupType CraftGroupType => _craftGroupType;

    public uint? Depth => _depth;
    public uint? ClassJobId => _classJobId;
    public uint? TerritoryTypeId => _territoryTypeId;

    public List<CraftItem> CraftItems => _craftItems;

    private uint? _depth;
    private uint? _classJobId;
    private uint? _territoryTypeId;
    private List<CraftItem> _craftItems;

    public CraftGrouping(CraftGroupType craftGroupType, List<CraftItem> craftItems, uint? depth = null, uint? classJobId = null, uint? territoryTypeId = null)
    {
        _craftGroupType = craftGroupType;
        _depth = depth;
        _craftItems = craftItems;
        _classJobId = classJobId;
        _territoryTypeId = territoryTypeId;
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

        if (_territoryTypeId != null)
        {
            var territoryType = Service.ExcelCache.GetTerritoryTypeExSheet().GetRow(_territoryTypeId.Value);
            if (territoryType != null)
            {
                name = territoryType.FormattedName + " - " + name;
            }
        }

        return name;
    }
}