namespace CriticalCommonLib.Models.ItemSources;

public interface IItemSource
{
    int Icon { get; }
    string Name { get; }
    uint? Count { get; }
    string FormattedName { get; }
    bool CanOpen { get; }
}