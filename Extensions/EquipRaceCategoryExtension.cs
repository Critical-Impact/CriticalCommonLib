using CriticalCommonLib.Enums;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Extensions
{
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