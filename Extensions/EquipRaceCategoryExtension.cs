using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Extensions
{
    public enum CharacterSex {
        Male = 0,
        Female = 1,
        Either = 2,
        Both = 3,
        FemaleOnly = 4,
        MaleOnly = 5,
        NotApplicable = 6,
    };
    public enum CharacterRace {
        None = 0,
        Hyur = 1,
        Elezen = 2,
        Lalafell = 3,
        Miqote = 4,
        Roegadyn = 5,
        AuRa = 6,
        Hrothgar = 7,
        Viera = 8,
        Any = 99
    };

    public static class EquipRaceCategoryExtension
    {
        public static bool AllowsRaceSex(this EquipRaceCategory erc, CharacterRace race, CharacterSex sex)
        {
            var raceId = (short) race; 
            return sex switch {
                CharacterSex.Both when (erc.Male == false || erc.Female == false) => false,
                CharacterSex.Either when (erc.Male == false && erc.Female == false) => false,
                CharacterSex.Female when erc.Female == false => false,
                CharacterSex.FemaleOnly when erc.Male || !erc.Female => false,
                CharacterSex.MaleOnly when erc.Female || !erc.Male => false,
                CharacterSex.Male when erc.Male == false => false,
                _ => raceId switch {
                    0 => false,
                    1 => erc.Hyur,
                    2 => erc.Elezen,
                    3 => erc.Lalafell,
                    4 => erc.Miqote,
                    5 => erc.Roegadyn,
                    6 => erc.AuRa,
                    7 => erc.Unknown6, // Hrothgar
                    8 => erc.Unknown7, // Viera
                    99 => true,
                    _ => false
                }
            };
        }

        public static CharacterRace EquipRace(this EquipRaceCategory erc)
        {
            if (erc.Hyur && erc.Elezen && erc.Lalafell && erc.Miqote && erc.Roegadyn && erc.Unknown6 && erc.AuRa && erc.Unknown6 && erc.Unknown7)
            {
                return CharacterRace.Any;
            }

            if (erc.Hyur)
            {
                return CharacterRace.Hyur;
            }

            if (erc.Elezen)
            {
                return CharacterRace.Elezen;
            }

            if (erc.Lalafell)
            {
                return CharacterRace.Lalafell;
            }

            if (erc.Miqote)
            {
                return CharacterRace.Miqote;
            }

            if (erc.Roegadyn)
            {
                return CharacterRace.Roegadyn;
            }

            if (erc.AuRa)
            {
                return CharacterRace.AuRa;
            }

            if (erc.Unknown6)
            {
                return CharacterRace.Hrothgar;
            }

            if (erc.Unknown7)
            {
                return CharacterRace.Viera;
            }

            return CharacterRace.None;
        }
        public static bool AllowsRace(this EquipRaceCategory erc, CharacterRace race)
        {
            var raceId = (short) race; 
            return raceId switch {
                0 => false,
                1 => erc.Hyur,
                2 => erc.Elezen,
                3 => erc.Lalafell,
                4 => erc.Miqote,
                5 => erc.Roegadyn,
                6 => erc.AuRa,
                7 => erc.Unknown6, // Hrothgar
                8 => erc.Unknown7, // Viera
                99 => true,
                _ => false
            };
        }
    }
}