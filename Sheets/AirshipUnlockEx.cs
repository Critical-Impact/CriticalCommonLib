using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class AirshipUnlockEx : AirshipUnlock
{
    public LazyRow<AirshipExplorationPointEx> AirshipExplorationPointEx;
    public LazyRow<AirshipExplorationPointEx> AirshipExplorationPointUnlockEx;
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        AirshipExplorationPointEx =
            new LazyRow<AirshipExplorationPointEx>(gameData, AirshipExplorationPoint.Row, language);
        AirshipExplorationPointUnlockEx =
            new LazyRow<AirshipExplorationPointEx>(gameData, AirshipExplorationPointUnlock.Row, language);
    }
}