using CriticalCommonLib.Enums;

namespace CriticalCommonLib.Extensions
{
    public static class InventoryTypeExtensions
    {
        public static InventoryType Convert(this FFXIVClientStructs.FFXIV.Client.Game.InventoryType type)
        {
            return (InventoryType)(int)type;
        }
    }
}