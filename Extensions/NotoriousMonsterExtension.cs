



using Lumina.Excel.Sheets;

namespace CriticalCommonLib.Extensions;

public static class NotoriousMonsterExtension
{
    public static unsafe string RankFormatted(this NotoriousMonster notoriousMonster)
    {
        switch (notoriousMonster.Rank)
        {
            case 1:
                return "B";
            case 2:
                return "A";
            case 3:
                return "S";
        }
        return "N/A";
    }
}