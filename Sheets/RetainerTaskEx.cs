using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class RetainerTaskEx : RetainerTask
    {
        public LazyRow<ClassJobCategoryEx> ClassJobCategoryEx { get; set; } = null!;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            ClassJobCategoryEx = new LazyRow<ClassJobCategoryEx>(gameData, ClassJobCategory.Row, language);
        }
    }
}