using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class SubmarineUnlockEx : SubmarineUnlock
{
    public LazyRow<SubmarineExplorationEx> SubmarineExplorationEx;
    public LazyRow<SubmarineExplorationEx> SubmarineExplorationUnlockEx;
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        SubmarineExplorationEx =
            new LazyRow<SubmarineExplorationEx>(gameData, SubmarineExploration.Row, language);
        SubmarineExplorationUnlockEx =
            new LazyRow<SubmarineExplorationEx>(gameData, SubmarineExplorationUnlock.Row, language);
    }
}