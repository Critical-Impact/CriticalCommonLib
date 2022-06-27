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