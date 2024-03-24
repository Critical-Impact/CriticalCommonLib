using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Interfaces;

public interface IItem
{
    public uint ItemId { get; set; }

    public ItemEx Item { get; }

}