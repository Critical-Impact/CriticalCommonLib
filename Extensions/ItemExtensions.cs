using CriticalCommonLib.Services;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Extensions
{
    public static class ItemExtensions
    {
        public static bool CanTryOn(this Item item)
        {
            if (item.EquipSlotCategory?.Value == null) return false;
            if (item.EquipSlotCategory.Row > 0 && item.EquipSlotCategory.Row != 6 && item.EquipSlotCategory.Row != 17 && (item.EquipSlotCategory.Value.OffHand <=0 || item.ItemUICategory.Row == 11))
            {
                return true;
            }

            return false;
        }

        public static bool CanBeCrafted(this Item item)
        {
            return ExcelCache.CanCraftItem(item.RowId);
        }
        
        public static bool CanBeBought(this Item item)
        {
            return ExcelCache.IsItemGilShopBuyable(item.RowId);
        }
        
        public static bool IsItemAvailableAtTimedNode(this Item item)
        {
            return ExcelCache.IsItemAvailableAtTimedNode(item.RowId);
        }
        
        public static bool IsEventItem(this Item item)
        {
            return GetEventItem(item) != null;
        }

        public static EventItem? GetEventItem(this Item item)
        {
            return ExcelCache.GetEventItem(item.RowId);
        }
        
        public static string FormattedRarity(this Item item)
        {
            switch (item.Rarity)
            {
                case 1:
                    return "Normal";
                case 2:
                    return "Scarce";
                case 3:
                    return "Artifact";
                case 4:
                    return "Relic";
                case 7:
                    return "Aetherial";
                default:
                    return "Unknown";
            }
        }
    }

}