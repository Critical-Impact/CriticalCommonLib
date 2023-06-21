using System;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace CriticalCommonLib.Extensions
{
    public static class InventoryItemExtensions
    {
        public static unsafe int HashCode(this InventoryItem inventoryItem, bool includeSpiritbond = true)
        {
            var hashCode = new HashCode();
            hashCode.Add((int)inventoryItem.Container);
            hashCode.Add(inventoryItem.Slot);
            hashCode.Add(inventoryItem.ItemID);
            hashCode.Add(inventoryItem.Quantity);
            if (includeSpiritbond)
            {
                hashCode.Add(inventoryItem.Spiritbond);
            }

            hashCode.Add(inventoryItem.Condition);
            hashCode.Add((int)inventoryItem.Flags);
            hashCode.Add(inventoryItem.Materia[0]);
            hashCode.Add(inventoryItem.Materia[1]);
            hashCode.Add(inventoryItem.Materia[2]);
            hashCode.Add(inventoryItem.Materia[3]);
            hashCode.Add(inventoryItem.Materia[4]);
            hashCode.Add(inventoryItem.MateriaGrade[0]);
            hashCode.Add(inventoryItem.MateriaGrade[1]);
            hashCode.Add(inventoryItem.MateriaGrade[2]);
            hashCode.Add(inventoryItem.MateriaGrade[3]);
            hashCode.Add(inventoryItem.MateriaGrade[4]);
            hashCode.Add(inventoryItem.Stain);
            return hashCode.ToHashCode();
        }
        
        public static unsafe bool IsSame(this InventoryItem item, InventoryItem otherItem)
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

            if (item.Spiritbond != otherItem.Spiritbond)
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