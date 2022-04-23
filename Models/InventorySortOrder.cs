using System.Collections.Generic;

namespace CriticalCommonLib.Models
{
    public struct InventorySortOrder
    {
        private Dictionary<ulong, RetainerSortOrder> _retainerInventories;
        private Dictionary<string, List<(int slotIndex, int containerIndex)>> _normalInventories;

        public InventorySortOrder(Dictionary<ulong, RetainerSortOrder> retainerInventories, Dictionary<string, List<(int slotIndex, int containerIndex)>> normalInventories)
        {
            _retainerInventories = retainerInventories;
            _normalInventories = normalInventories;
        }

        public Dictionary<ulong, RetainerSortOrder> RetainerInventories => _retainerInventories;

        public Dictionary<string, List<(int slotIndex, int containerIndex)>> NormalInventories => _normalInventories;
    }
}