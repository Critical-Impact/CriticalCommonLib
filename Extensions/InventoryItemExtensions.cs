using System;
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

            if (item.ItemID != otherItem.ItemID)
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

            if ((item.ItemID != 0 || otherItem.ItemID != 0) && item.Condition != otherItem.Condition)
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

            if (item.MateriaGrade[0] != otherItem.MateriaGrade[0])
            {
                return false;
            }

            if (item.MateriaGrade[1] != otherItem.MateriaGrade[1])
            {
                return false;
            }

            if (item.MateriaGrade[2] != otherItem.MateriaGrade[2])
            {
                return false;
            }

            if (item.MateriaGrade[3] != otherItem.MateriaGrade[3])
            {
                return false;
            }

            if (item.MateriaGrade[4] != otherItem.MateriaGrade[4])
            {
                return false;
            }

            if (item.Stain != otherItem.Stain)
            {
                return false;
            }

            if (item.GlamourID != otherItem.GlamourID)
            {
                return false;
            }

            return true;
        }
    }
}