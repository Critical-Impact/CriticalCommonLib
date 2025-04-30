using System.Collections.Generic;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Crafting;

public class CraftGrouping
{
    private readonly CraftGroupType _craftGroupType;

    public CraftGroupType CraftGroupType => this._craftGroupType;

    public uint? Depth => this._depth;
    public uint? CraftTypeId => this._craftTypeId;
    public uint? MapId => this._mapId;

    public List<CraftItem> CraftItems => this._craftItems;

    private uint? _depth;
    private uint? _craftTypeId;
    private uint? _mapId;
    private readonly List<CraftItem> _craftItems;

    public CraftGrouping(CraftGroupType craftGroupType, List<CraftItem> craftItems, uint? depth = null, uint? craftTypeId = null, uint? mapId = null)
    {
        this._craftGroupType = craftGroupType;
        this._depth = depth;
        this._craftItems = craftItems;
        this._craftTypeId = craftTypeId;
        this._mapId = mapId;
    }
}