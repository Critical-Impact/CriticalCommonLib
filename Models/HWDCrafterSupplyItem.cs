using CriticalCommonLib.Sheets;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models;

public class HWDCrafterSupplyItem
{
    private LazyRow<HWDCrafterSupplyReward> _baseReward;
    private LazyRow<HWDCrafterSupplyReward> _midReward;
    private LazyRow<HWDCrafterSupplyReward> _highReward;
    private ushort _baseCollectableRating;
    private ushort _midCollectableRating;
    private ushort _highCollectableRating;
    private LazyRow<ItemEx> _item;

    public LazyRow<HWDCrafterSupplyReward> BaseReward => _baseReward;

    public LazyRow<HWDCrafterSupplyReward> MidReward => _midReward;

    public LazyRow<HWDCrafterSupplyReward> HighReward => _highReward;

    public ushort BaseCollectableRating => _baseCollectableRating;

    public ushort MidCollectableRating => _midCollectableRating;

    public ushort HighCollectableRating => _highCollectableRating;

    public LazyRow<ItemEx> Item => _item;

    public HWDCrafterSupplyItem(LazyRow<HWDCrafterSupplyReward> baseReward, LazyRow<HWDCrafterSupplyReward> midReward, LazyRow<HWDCrafterSupplyReward> highReward, LazyRow<ItemEx> item, ushort baseCollectableRating, ushort midCollectableRating, ushort highCollectableRating)
    {
        _baseReward = baseReward;
        _midReward = midReward;
        _highReward = highReward;
        _baseCollectableRating = baseCollectableRating;
        _midCollectableRating = midCollectableRating;
        _highCollectableRating = highCollectableRating;
        _item = item;
    }
}