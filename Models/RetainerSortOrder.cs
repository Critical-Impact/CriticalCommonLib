using System.Collections.Generic;

namespace InventoryTools.Structs
{
    public struct RetainerSortOrder
    {
        private ulong _id;
        private List<(int slotIndex, int containerIndex)> _inventoryCoords;

        public RetainerSortOrder(ulong id, List<(int slotIndex, int containerIndex)> inventoryCoords)
        {
            this._id = id;
            this._inventoryCoords = inventoryCoords;
        }

        public List<(int slotIndex, int containerIndex)> InventoryCoords => _inventoryCoords;

        public ulong Id => _id;
    }
}