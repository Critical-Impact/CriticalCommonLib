using System;
using System.Collections;
using Lumina.Excel.Sheets;


namespace CriticalCommonLib.Models
{
    public static class ActionTypeExt {
        private static readonly ActionType[] Valid = (ActionType[]) Enum.GetValues(typeof(ActionType));
        public static bool IsValidAction(ItemAction? action) {
            if (action == null || action.Value.RowId == 0) {
                return false;
            }
            var type = (ActionType) action.Value.Action.RowId;
            return ((IList) Valid).Contains(type);
        }
    }
    public enum ActionType : ushort {
        Companion = 853,
        BuddyEquip = 1013,
        Mount = 1322,
        SecretRecipeBook = 2136,
        UnlockLink = 2633, // riding maps, blu totems, emotes/dances, hairstyles
        TripleTriadCard = 3357,
        FolkloreTome = 4107,
        OrchestrionRoll = 25183,
        FramersKit = 29459,
        // FieldNotes = 19743, // bozjan field notes (server side, but cached)
        Ornament = 20086,
        Glasses = 37312,
        CompanySealVouchers = 41120, // can use = is in grand company, is unlocked = always false
    }
}