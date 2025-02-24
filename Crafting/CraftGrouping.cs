using System.Collections.Generic;
using CriticalCommonLib.Extensions;

namespace CriticalCommonLib.Crafting;

public class CraftGrouping
{
    private readonly CraftGroupType _craftGroupType;

    public CraftGroupType CraftGroupType => this._craftGroupType;

    public uint? Depth => this._depth;
    public uint? ClassJobId => this._classJobId;
    public uint? MapId => this._mapId;

    public List<CraftItem> CraftItems => this._craftItems;

    private uint? _depth;
    private uint? _classJobId;
    private uint? _mapId;
    private readonly List<CraftItem> _craftItems;

    public CraftGrouping(CraftGroupType craftGroupType, List<CraftItem> craftItems, uint? depth = null, uint? classJobId = null, uint? mapId = null)
    {
        this._craftGroupType = craftGroupType;
        this._depth = depth;
        this._craftItems = craftItems;
        this._classJobId = classJobId;
        this._mapId = mapId;
    }
}