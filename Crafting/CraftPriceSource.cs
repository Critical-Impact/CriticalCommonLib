namespace CriticalCommonLib.Crafting
{
    public class CraftPriceSource
    {
        public uint ItemId;
        public uint Quantity;
        public uint Used;
        public bool IsHq;
        public uint UnitPrice;
        public uint WorldId;

        public int Left => (int)this.Quantity - (int)this.Used;

        public void Reset()
        {
            this.Used = 0;
        }

        public CraftPriceSource(uint itemId, uint quantity, bool isHq, uint unitPrice, uint worldId)
        {
            this.ItemId = itemId;
            this.Quantity = quantity;
            this.IsHq = isHq;
            this.UnitPrice = unitPrice;
            this.WorldId = worldId;
        }

        public CraftPriceSource(CraftPriceSource source, uint quantity)
        {
            this.ItemId = source.ItemId;
            this.Quantity = quantity;
            this.IsHq = source.IsHq;
            this.UnitPrice = source.UnitPrice;
            this.WorldId = source.WorldId;
        }

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