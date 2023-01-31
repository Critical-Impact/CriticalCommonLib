using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class CompanyCraftSupplyItemEx : CompanyCraftSupplyItem
    {
        public LazyRow<ItemEx> ItemEx;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            ItemEx = new LazyRow<ItemEx>(gameData, Item.Row, language);
        }
    }
}