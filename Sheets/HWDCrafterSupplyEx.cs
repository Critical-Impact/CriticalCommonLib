using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class HWDCrafterSupplyEx : HWDCrafterSupply
{
    public LazyRow<ItemEx>[] ItemTradeInEx = null!;
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        ItemTradeInEx = ItemTradeIn.Select(c => new LazyRow<ItemEx>(gameData, c.Row, language)).ToArray();
        supplyItems = new Dictionary<uint, HWDCrafterSupplyItem>();
        for (var index = 0; index < ItemTradeInEx.Length; index++)
        {
            var item = ItemTradeInEx[index];
            supplyItems.TryAdd(item.Row,new HWDCrafterSupplyItem(BaseCollectableRewardPostPhase[index], MidCollectableRewardPostPhase[index], HighCollectableRewardPostPhase[index], item, BaseCollectableRating[index], MidCollectableRating[index], HighCollectableRating[index]));
        }
    }

    private Dictionary<uint, HWDCrafterSupplyItem> supplyItems = null!;

    public HWDCrafterSupplyItem? GetSupplyItem(uint itemId)
    {
        if (supplyItems.ContainsKey(itemId))
        {
            return supplyItems[itemId];
        }

        return null;
    }
}