using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Extensions
{
    public enum CharacterSex {
        Male = 0,
        Female = 1,
        Either = 2,
        Both = 3
    };

    public static class EquipRaceCategoryExtension
    {
        public static bool AllowsRaceSex(this EquipRaceCategory erc, uint raceId, CharacterSex sex) {
            return sex switch {
                CharacterSex.Both when (erc.Male == false || erc.Female == false) => false,
                CharacterSex.Either when (erc.Male == false && erc.Female == false) => false,
                CharacterSex.Female when erc.Female == false => false,
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
                    _ => false
                }
            };
        }
    }
}