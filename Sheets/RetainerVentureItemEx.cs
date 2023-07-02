using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class RetainerVentureItemEx : RetainerVentureItem
{
    public LazyRow< RetainerTaskRandomEx > RetainerTaskRandomEx = null!;
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        RetainerTaskRandomEx = new LazyRow<RetainerTaskRandomEx>(gameData, RetainerTaskRandom.Row, language);
    }
}