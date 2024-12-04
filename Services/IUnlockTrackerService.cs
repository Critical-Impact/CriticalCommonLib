using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Rows;

namespace CriticalCommonLib.Services;

public interface IUnlockTrackerService
{
    delegate void ItemUnlockStatusChangedDelegate();

    event ItemUnlockStatusChangedDelegate? ItemUnlockStatusChanged;

    HashSet<uint> UnlockedItems { get; }
    bool? IsUnlocked(ItemRow item, bool notify = true);
    void QueueUnlockCheck(uint itemId);
    void QueueUnlockCheck(ItemRow item);
}