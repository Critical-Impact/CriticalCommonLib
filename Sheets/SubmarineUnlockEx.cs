using Lumina;
using Lumina.Data;
using Lumina.Excel;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Sheets;

public class SubmarineUnlockEx : SubmarineUnlock
{
    public LazyRow<SubmarineExplorationEx> SubmarineExplorationEx = null!;
    public LazyRow<SubmarineExplorationEx> SubmarineExplorationUnlockEx = null!;
    public override void PopulateData(GameData gameData, Language language)
    {
        base.PopulateData(gameData, language);
        SubmarineExplorationEx =
            new LazyRow<SubmarineExplorationEx>(gameData, SubmarineExploration.Row, language);
        SubmarineExplorationUnlockEx =
            new LazyRow<SubmarineExplorationEx>(gameData, SubmarineExplorationUnlock.Row, language);
    }
}