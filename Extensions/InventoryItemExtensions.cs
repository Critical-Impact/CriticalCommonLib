using FFXIVClientStructs.FFXIV.Client.Game;

namespace CriticalCommonLib.Extensions
{
    public static class InventoryItemExtensions
    {
        public static unsafe bool IsSame(this InventoryItem item, InventoryItem otherItem, bool includeSpiritBond = true)
        {
            if (item.Container != otherItem.Container)
            {
                return false;
            }

            if (item.Slot != otherItem.Slot)
            {
                return false;
            }

            if (item.ItemId != otherItem.ItemId)
            {
                return false;
            }

            if (item.Quantity != otherItem.Quantity)
            {
                return false;
            }

            if (includeSpiritBond && item.Spiritbond != otherItem.Spiritbond)
            {
                return false;
            }

            if ((item.ItemId != 0 || otherItem.ItemId != 0) && item.Condition != otherItem.Condition)
            {
                return false;
            }

            if (item.Flags != otherItem.Flags)
            {
                return false;
            }

            if (item.Materia[0] != otherItem.Materia[0])
            {
                return false;
            }

            if (item.Materia[1] != otherItem.Materia[1])
            {
                return false;
            }

            if (item.Materia[2] != otherItem.Materia[2])
            {
                return false;
            }

            if (item.Materia[3] != otherItem.Materia[3])
            {
                return false;
            }

            if (item.Materia[4] != otherItem.Materia[4])
            {
                return false;
            }

            if (item.MateriaGrades[0] != otherItem.MateriaGrades[0])
            {
                return false;
            }

            if (item.MateriaGrades[1] != otherItem.MateriaGrades[1])
            {
                return false;
            }

            if (item.MateriaGrades[2] != otherItem.MateriaGrades[2])
            {
                return false;
            }

            if (item.MateriaGrades[3] != otherItem.MateriaGrades[3])
            {
                return false;
            }

            if (item.MateriaGrades[4] != otherItem.MateriaGrades[4])
            {
                return false;
            }

            if (item.Stains[0] != otherItem.Stains[0])
            {
                return false;
            }

            if (item.Stains[1] != otherItem.Stains[1])
            {
                return false;
            }

            if (item.GlamourId != otherItem.GlamourId)
            {
                return false;
            }

            return true;
        }
    }
}