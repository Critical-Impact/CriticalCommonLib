namespace CriticalCommonLib.Crafting
{
    public class CraftItemSource
    {
        public uint ItemId { get; }
        public uint Quantity { get; set; }
        public uint Used { get; set; }
        public bool IsHq { get; }
        
        public int Left => (int)this.Quantity - (int)this.Used;

        public CraftItemSource(uint itemId, uint quantity, bool isHq)
        {
            this.ItemId = itemId;
            this.Quantity = quantity;
            this.IsHq = isHq;
        }

        public CraftItemSource(CraftItemSource original)
        {
            this.ItemId = original.ItemId;
            this.Quantity = original.Quantity;
            this.IsHq = original.IsHq;
        }

        /// <summary>
        /// Returns a given quantity of an item to the external source
        /// </summary>
        /// <param name="quantity">The quantity to return to the source</param>
        /// <returns>The amount of that could not be returned</returns>
        public uint ReturnQuantity(int quantity)
        {
            var used = (int)this.Used;
            if (this.Used == 0)
            {
                return (uint) quantity;
            }
            if (used - quantity < 0)
            {
                this.Used = 0;
                quantity = (quantity - used);
                return (uint) quantity;
            }

            this.Used -= (uint)quantity;
            return 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="quantity">The quantity that is required</param>
        /// <returns>The amount unfufilled</returns>
        public uint UseQuantity(int quantity)
        {
            var left = this.Left;

            //Nothing left in this source
            if (left == 0)
            {
                return (uint) quantity;
            }

            //taking away the quantity from what's left takes us into negatives, use what we can and return the remainder
            if (left - quantity < 0)
            {
                this.Used = this.Quantity;
                quantity = (quantity - left);
                return (uint) quantity;
            }

            //We can use some or all of it and are left with 0
            this.Used += (uint)quantity;
            return 0;

        }
    }
}