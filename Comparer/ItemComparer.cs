using System.Collections.Generic;
using CriticalCommonLib.Sheets;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Comparer
{
public class ItemComparer : IComparer<ItemEx> {
        public enum Option {
            None = 0,
            Ascending = 1,
            Descending = -1
        }

        #region Static

        public static readonly ItemComparer Default = new ItemComparer();

        #endregion

        #region Fields

        private Option _CategoryOption = Option.Ascending;
        private Option _EquipLevelOption = Option.Descending;
        private Option _ItemLevelOption = Option.Descending;

        #endregion

        #region Properties

        public Option CategoryOption { get { return _CategoryOption; } set { _CategoryOption = value; } }
        public Option EquipLevelOption { get { return _EquipLevelOption; } set { _EquipLevelOption = value; } }
        public Option ItemLevelOption { get { return _ItemLevelOption; } set { _ItemLevelOption = value; } }

        #endregion

        #region IComparer<InventoryItem> Members

        public int Compare(ItemEx? x, ItemEx? y) {
            if (x == y)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            var comp = CompareCategoryMajor(x, y);
            if (comp != 0)
                return comp;

            comp = CompareCategoryMinor(x, y);
            if (comp != 0)
                return comp;

            comp = CompareEquipLevel(x, y);
            if (comp != 0)
                return comp;

            comp = CompareItemLevel(x, y);
            if (comp != 0)
                return comp;

            return x.RowId.CompareTo(y.RowId);
        }

        public int CompareCategoryMajor(ItemEx x, ItemEx y) {
            return ((int)CategoryOption) * (x.ItemUICategory.Value?.OrderMajor  ?? 999).CompareTo(y.ItemUICategory.Value?.OrderMajor ?? 999);
        }

        public int CompareCategoryMinor(ItemEx x, ItemEx y) {
            return ((int)CategoryOption) * (x.ItemUICategory.Value?.OrderMajor  ?? 999).CompareTo(y.ItemUICategory.Value?.OrderMajor ?? 999);
        }

        public int CompareEquipLevel(ItemEx x, ItemEx y) {
            var ex = 0;
            var ey = 0;
            if (x.EquipSlotCategory.Row != 0)
            {
                ex = x.LevelEquip;
            }

            if (y.EquipSlotCategory.Row != 0)
            {
                ey = y.LevelEquip;
            }

            return ((int)EquipLevelOption) * ex.CompareTo(ey);
        }

        public int CompareItemLevel(ItemEx x, ItemEx y) {
            return ((int)ItemLevelOption) * x.LevelItem.Row.CompareTo(y.LevelItem.Row);
        }

        #endregion
    }
}