using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class ENpcPlaceEx : ENpcPlace
{
    public LazyRow< TerritoryTypeEx > TerritoryTypeEx;

    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        TerritoryTypeEx = new LazyRow<TerritoryTypeEx>(gameData, TerritoryTypeId, language);
    }
}