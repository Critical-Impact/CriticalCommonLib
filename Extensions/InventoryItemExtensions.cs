using System;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace CriticalCommonLib.Extensions
{
    public static class InventoryItemExtensions
    {
        public static unsafe int HashCode(this InventoryItem inventoryItem, bool includeSpiritbond = true)
        {
            if (inventoryItem.ItemID == 0)
            {
                return 0;
            }
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
    }
}