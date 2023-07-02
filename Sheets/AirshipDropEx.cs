using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class AirshipDropEx : AirshipDrop
{
    public LazyRow<ItemEx> ItemEx = null!;

    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        ItemEx = new LazyRow<ItemEx>(gameData, ItemId, language);
    }
}