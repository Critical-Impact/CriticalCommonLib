using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Models;
using Dalamud.Logging;

namespace CriticalCommonLib.Services;

public class InventoryHistory : IDisposable
{
    public delegate void HistoryLoggedDelegate(List<InventoryChange> inventoryChanges);
    public event HistoryLoggedDelegate? OnHistoryLogged;
    private readonly IInventoryMonitor _monitor;
    private List<InventoryChange> _history;
    private HashSet<InventoryChangeReason>? _reasonsToLog;
    private bool _enabled = false;

    public bool Enabled => _enabled;
    public HashSet<InventoryChangeReason> ReasonsToLog => _reasonsToLog ?? new HashSet<InventoryChangeReason>();

    public InventoryHistory(IInventoryMonitor monitor)
    {
        _history = new List<InventoryChange>();
        _monitor = monitor;
        monitor.OnInventoryChanged += MonitorOnOnInventoryChanged;
    }

    public void Enable()
    {
        _enabled = true;
    }

    public void Disable()
    {
        _enabled = false;
    }

    /// <summary>
    /// Sets the types of changes to the inventories to log
    /// </summary>
    /// <param name="changeReasons"></param>
    public void SetChangeReasonsToLog(HashSet<InventoryChangeReason> changeReasons)
    {
        _reasonsToLog = changeReasons;
    }

    /// <summary>
    /// Clears the reasons to log and will start logging all history.
    /// </summary>
    public void ClearChangeReasonsToLog()
    {
        _reasonsToLog = null;
    }

    public void ClearHistory()
    {
        _history = new List<InventoryChange>();
        OnHistoryLogged?.Invoke(new ());
    }

    private void MonitorOnOnInventoryChanged(List<InventoryChange> inventoryChanges, InventoryMonitor.ItemChanges? itemChanges)
    {
        if (!_enabled) return;
        PluginLog.Verbose("Original Changes: ");
        for (var index = 0; index < inventoryChanges.Count; index++)
        {
            var change = inventoryChanges[index];
            var fromItemDebugName = change.FromItem?.DebugName ?? "Unknown";
            var toDebugName = change.ToItem?.DebugName ?? "Unknown";
            PluginLog.Verbose("Changed: " + fromItemDebugName + " switched to " + toDebugName + " because it " +
                              change.InventoryChangeReason);
        }

        PluginLog.Verbose("Analyzed Changes: ");
        var changes = AnalyzeInventoryChanges(inventoryChanges.Where(c => c.FirstLoad == false).ToList());
        foreach (var change in changes)
        {
            var fromItemDebugName = change.FromItem?.DebugName ?? "Unknown";
            var toDebugName = change.ToItem?.DebugName ?? "Unknown";
            PluginLog.Verbose("Changed: " + fromItemDebugName + " switched to " + toDebugName + " because it " + change.InventoryChangeReason);
        }

        var newChanges = changes.Where(c => (_reasonsToLog == null || _reasonsToLog.Contains(c.InventoryChangeReason)) &&  c.InventoryChangeReason != InventoryChangeReason.Moved).ToList();
        OnHistoryLogged?.Invoke(newChanges);
        _history = _history.Concat(newChanges).ToList();
    }

    public List<InventoryChange> GetHistory()
    {
        return _history.ToList();
    }

    public void LoadExistingHistory(List<InventoryChange> history)
    {
        _history = history;
    }

    private void ScannerOnBagsChanged(List<BagChange> changes)
    {
        
    }

    public void ParseBagChangeEvent(List<BagChange> changes)
    {
        
    }
    
    public List<InventoryChange> AnalyzeInventoryChanges(List<InventoryChange> changes)
    {
        uint newChangeId = (uint)(_history.Count + 1);
        var processedFrom = new HashSet<int>();
        var processedTo = new HashSet<int>();
        var processedChanges = new List<InventoryChange>();

        //Test each item for movement of items first
        for (int i = 0; i < changes.Count; i++)
        {
            var change = changes[i];
            var fromItem = change.FromItem;
            var toItem = change.ToItem;
            if (fromItem != null && toItem != null)
            {
                if (fromItem.IsSamePosition(toItem))
                {
                    var quantityDiff = (int)fromItem.Quantity - (int)toItem.Quantity;
                    if (quantityDiff != 0)
                    {
                        var matchingQuantityDiffIndex =
                            FindMatchingQuantityDiff(changes, i, quantityDiff, processedFrom, processedTo);
                        if (matchingQuantityDiffIndex != -1)
                        {
                            //Found an item where the quantity taken from our item matches the amount gained by the item
                            var matchingChange = changes[matchingQuantityDiffIndex];
                            processedChanges.Add(new InventoryChange(fromItem, matchingChange.ToItem,
                                InventoryChangeReason.Moved, newChangeId));
                            processedFrom.Add(i);
                            processedFrom.Add(matchingQuantityDiffIndex);
                            processedTo.Add(i);
                            processedTo.Add(matchingQuantityDiffIndex);
                        }
                    }
                }
            }
        }

        for (int i = 0; i < changes.Count; i++)
        {
            var change = changes[i];
            var fromItem = change.FromItem;
            var toItem = change.ToItem;

            if (fromItem != null && toItem != null)
            {
                var matchingIndexFrom = FindMatchingChangeDifferentPositionFrom(changes,  toItem, processedFrom);

                if (matchingIndexFrom != -1)
                {
                    var matchingChange = changes[matchingIndexFrom];

                    if (matchingChange.FromItem == null || matchingChange.FromItem.ItemId == 0 && matchingChange.ToItem != null)
                    {
                        // Item moved or empty slot
                        processedChanges.Add(new InventoryChange(fromItem, matchingChange.ToItem,InventoryChangeReason.Moved, newChangeId));
                    }
                    else if (matchingChange.FromItem != null && matchingChange.FromItem.IsSameItem(toItem) == null)
                    {
                        // Item moved or empty slot
                        processedChanges.Add(new InventoryChange(matchingChange.FromItem, toItem,
                            InventoryChangeReason.Moved, newChangeId));
                        
                    }
                    processedFrom.Add(matchingIndexFrom);
                    processedTo.Add(i);
                }
                var matchingIndexTo = FindMatchingChangeDifferentPositionTo(changes, fromItem, processedTo);

                if (matchingIndexTo != -1)
                {
                    var matchingChange = changes[matchingIndexTo];
                    
                    if (matchingChange.ToItem == null || matchingChange.ToItem.ItemId == 0 && matchingChange.FromItem != null)
                    {
                        // Item moved or empty slot
                        processedChanges.Add(new InventoryChange(matchingChange.FromItem, toItem,
                            InventoryChangeReason.Moved, newChangeId));
                    }
                    else if (matchingChange.ToItem != null && matchingChange.ToItem.IsSameItem(fromItem) == null)
                    {
                        // Item moved or empty slot
                        processedChanges.Add(new InventoryChange(fromItem, matchingChange.ToItem, InventoryChangeReason.Moved, newChangeId));
                    }
                    processedTo.Add(matchingIndexTo);
                    processedFrom.Add(i);
                }

                if (matchingIndexFrom == -1 && matchingIndexTo == -1 && !processedTo.Contains(i) && !processedFrom.Contains(i))
                {
                    if (ProcessSingleItem(fromItem, toItem, processedChanges, newChangeId))
                    {
                        processedFrom.Add(i);
                        processedTo.Add(i);
                    }
                }
            }
        }

        return processedChanges;
    }
    
    

    private static bool ProcessSingleItem(InventoryItem fromItem, InventoryItem toItem, List<InventoryChange> processedChanges, uint newChangeId)
    {
        if (fromItem.IsSamePosition(toItem))
        {
            var changeReason = fromItem.IsSameItem(toItem);
            if (changeReason != null)
            {
                if (changeReason == InventoryChangeReason.ItemIdChanged && (fromItem.ItemId == 0 || toItem.ItemId == 0))
                {
                    if (fromItem.ItemId == 0)
                    {
                        processedChanges.Add(new InventoryChange(fromItem, toItem, InventoryChangeReason.Added, newChangeId));
                        return true;
                    }
                    else
                    {
                        processedChanges.Add(new InventoryChange(fromItem, toItem, InventoryChangeReason.Removed, newChangeId));
                        return true;
                    }
                }
                else if (changeReason == InventoryChangeReason.ItemIdChanged && (fromItem.ItemId != 0 && toItem.ItemId != 0))
                {
                    //We found no match earlier and the item IDs are not the same, one item was destroyed and one item was created
                    processedChanges.Add(new InventoryChange(fromItem, null, InventoryChangeReason.Removed, newChangeId));
                    processedChanges.Add(new InventoryChange(null, toItem, InventoryChangeReason.Added, newChangeId));
                    return true;
                }
                else
                {
                    processedChanges.Add(new InventoryChange(fromItem, toItem, changeReason.Value, newChangeId));
                    return true;
                }
            }
        }

        return false;
    }

    private int FindMatchingChangeDifferentPositionFrom(List<InventoryChange> changes, InventoryItem toItem, HashSet<int> processedFrom)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            if (processedFrom.Contains(i)) continue;
            
            var change = changes[i];
            if (change.FromItem != null && !change.FromItem.IsSamePosition(toItem) && change.FromItem.IsSameItem(toItem) == null && change.FromItem.ItemId != 0 && toItem.ItemId != 0)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindMatchingChangeDifferentPositionTo(List<InventoryChange> changes, InventoryItem fromItem, HashSet<int> processedTo)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            if (processedTo.Contains(i)) continue;

            var change = changes[i];
            if (change.ToItem != null && !change.ToItem.IsSamePosition(fromItem) && change.ToItem.IsSameItem(fromItem) == null)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindMatchingQuantityDiff(List<InventoryChange> changes, int currentIndex, int quantityDiff, HashSet<int> processedFrom, HashSet<int> processedTo)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            if (i == currentIndex) continue;
            if (processedTo.Contains(i)) continue;
            if (processedFrom.Contains(i)) continue;

            var change = changes[i];
            var currentChange = changes[currentIndex];
            if (change.ToItem != null && change.FromItem != null && currentChange.FromItem != null && currentChange.ToItem != null)
            {
                if ( currentChange.FromItem.IsSamePosition(currentChange.ToItem) && change.FromItem.IsSamePosition(change.ToItem))
                {
                    //Moving whole item to new slot
                    if (currentChange.FromItem.ItemId == change.ToItem.ItemId && currentChange.ToItem.ItemId == 0 &&
                        change.FromItem.ItemId == 0)
                    {
                        var diff = (int)change.ToItem.Quantity - (int)change.FromItem.Quantity;
                        if (diff == quantityDiff)
                        {
                            return i;
                        }
                    }
                    //Moving a quantity of an item from one stack to another stack
                    if (currentChange.FromItem.ItemId == currentChange.ToItem.ItemId && change.FromItem.ItemId == change.ToItem.ItemId && currentChange.FromItem.ItemId == change.FromItem.ItemId)
                    {
                        var diff = (int)change.ToItem.Quantity - (int)change.FromItem.Quantity;
                        if (diff == quantityDiff)
                        {
                            return i;
                        }
                    }
                    
                    //Moving an item from an existing stack to make a new stack
                    if (currentChange.FromItem.ItemId == currentChange.ToItem.ItemId && change.FromItem.ItemId == 0 && currentChange.ToItem.ItemId == change.ToItem.ItemId)
                    {
                        var diff = (int)change.ToItem.Quantity - (int)change.FromItem.Quantity;
                        if (diff == quantityDiff)
                        {
                            return i;
                        }
                    }
                    
                    //Moving an item from an existing stack to collapse stacks
                    if (change.FromItem.ItemId == change.ToItem.ItemId && currentChange.ToItem.ItemId == 0 && currentChange.FromItem.ItemId == change.ToItem.ItemId)
                    {
                        var diff = (int)change.ToItem.Quantity - (int)change.FromItem.Quantity;
                        if (diff == quantityDiff)
                        {
                            return i;
                        }
                    }
                }
            }
        }

        return -1;
    }

    public void Dispose()
    {
        _monitor.OnInventoryChanged -= MonitorOnOnInventoryChanged;
    }
}