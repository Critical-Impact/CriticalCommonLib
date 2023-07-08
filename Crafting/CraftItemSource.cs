namespace CriticalCommonLib.Crafting
{
    public class CraftItemSource
    {
        public uint ItemId;
        public uint Quantity;
        public uint Used;
        public bool IsHq;

        public int Left => (int)Quantity - (int)Used;

        public CraftItemSource(uint itemId, uint quantity, bool isHq)
        {
            ItemId = itemId;
            Quantity = quantity;
            IsHq = isHq;
        }

        /// <summary>
        /// Returns a given quantity of an item to the external source
        /// </summary>
        /// <param name="quantity">The quantity to return to the source</param>
        /// <returns>The amount of that could not be returned</returns>
        public uint ReturnQuantity(int quantity)
        {
            var used = (int)Used;
            if (Used == 0)
            {
                return (uint) quantity;
            }
            if (used - quantity < 0)
            {
                Used = Quantity;
                quantity = (quantity - used);
                return (uint) quantity;
            }

            Used += (uint)quantity;
            return 0;
        }

        public uint UseQuantity(int quantity)
        {
            var left = Left;
            
            //Nothing left in this source
            if (left == 0)
            {
                return (uint) quantity;
            }

            //taking away the quantity from what's left takes us into negatives, use what we can and return the remainder
            if (left - quantity < 0)
            {
                Used = Quantity;
                quantity = (quantity - left);
                return (uint) quantity;
            }

            //We can use some or all of it and are left with 0
            Used += (uint)quantity;
            return 0;

        }
    }
}