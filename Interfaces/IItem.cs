using AllaganLib.GameSheets.Sheets.Rows;


namespace CriticalCommonLib.Interfaces;

public interface IItem
{
    public uint ItemId { get; set; }

    public ItemRow Item { get; }

}