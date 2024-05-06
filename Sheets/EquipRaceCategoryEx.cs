using CriticalCommonLib.Enums;

namespace CriticalCommonLib.Sheets
{
    public class EquipRaceCategoryEx : Lumina.Excel.GeneratedSheets.EquipRaceCategory
    {
        public CharacterRace EquipRace
        {
            get
            {
                if (Hyur && Elezen && Lalafell && Miqote && Roegadyn && Unknown6 && AuRa && Unknown6 && Unknown7)
                {
                    return CharacterRace.Any;
                }

                if (Hyur)
                {
                    return CharacterRace.Hyur;
                }

                if (Elezen)
                {
                    return CharacterRace.Elezen;
                }

                if (Lalafell)
                {
                    return CharacterRace.Lalafell;
                }

                if (Miqote)
                {
                    return CharacterRace.Miqote;
                }

                if (Roegadyn)
                {
                    return CharacterRace.Roegadyn;
                }

                if (AuRa)
                {
                    return CharacterRace.AuRa;
                }

                if (Unknown6)
                {
                    return CharacterRace.Hrothgar;
                }

                if (Unknown7)
                {
                    return CharacterRace.Viera;
                }

                return CharacterRace.None;
            }
        }
    }
}