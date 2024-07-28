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

        public int Left => (int)Quantity - (int)Used;

        public void Reset()
        {
            Used = 0;
        }

        public CraftPriceSource(uint itemId, uint quantity, bool isHq, uint unitPrice, uint worldId)
        {
            ItemId = itemId;
            Quantity = quantity;
            IsHq = isHq;
            UnitPrice = unitPrice;
            WorldId = worldId;
        }

        public CraftPriceSource(CraftPriceSource source, uint quantity)
        {
            ItemId = source.ItemId;
            Quantity = quantity;
            IsHq = source.IsHq;
            UnitPrice = source.UnitPrice;
            WorldId = source.WorldId;
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