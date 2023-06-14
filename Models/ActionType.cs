using System;
using System.Collections;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models
{
    public static class ActionTypeExt {
        private static readonly ActionType[] Valid = (ActionType[]) Enum.GetValues(typeof(ActionType));
        public static bool IsValidAction(ItemAction? action) {
            if (action == null || action.RowId == 0) {
                return false;
            }
            var type = (ActionType) action.Type;
            return ((IList) Valid).Contains(type);
        }
    }
    public enum ActionType : ushort {
        Minions = 853, // minions
        Bardings = 1_013, // bardings
        Mounts = 1_322, // mounts
        CrafterBooks = 2_136, // crafter books
        Miscellaneous = 2_633, // riding maps, blu totems, emotes/dances, hairstyles
        Cards = 3_357, // cards
        GathererBooks = 4_107, // gatherer books
        OrchestrionRolls = 25_183, // orchestrion rolls
        // these appear to be server-side
        // FieldNotes = 19_743, // bozjan field notes
        FashionAccessories = 20_086, // fashion accessories
        // missing: 2_894 (always false),
        FramersKits = 29_459,
    }
}