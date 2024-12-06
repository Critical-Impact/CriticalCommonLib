using System.Collections.Generic;
using System.Linq;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Models;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.Exd;
using Lumina.Excel.Sheets;

namespace CriticalCommonLib.Services;

public unsafe class UnlockTrackerService : IUnlockTrackerService
{
    private readonly ItemSheet _itemSheet;
    private readonly IPluginLog _pluginLog;
    private readonly IDataManager _dataManager;
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly IInventoryMonitor _inventoryMonitor;
    private Queue<uint> _unlockedItemsToCheck;

    public UnlockTrackerService(ItemSheet itemSheet, IPluginLog pluginLog, IDataManager dataManager,
        IClientState clientState, IFramework framework, IInventoryMonitor inventoryMonitor)
    {
        _itemSheet = itemSheet;
        _pluginLog = pluginLog;
        _dataManager = dataManager;
        _clientState = clientState;
        _framework = framework;
        _inventoryMonitor = inventoryMonitor;
        _framework.Update += FrameworkOnUpdate;
        _unlockedItemsToCheck = new Queue<uint>();
        _clientState.Login += ClientStateOnLogin;
        _clientState.Logout += ClientStateOnLogout;
        _inventoryMonitor.OnInventoryChanged += ItemRemoved;
        if (_clientState.IsLoggedIn) ClientStateOnLogin();
    }

    private void ItemRemoved(List<InventoryChange> inventoryChanges, InventoryMonitor.ItemChanges? _)
    {
        if (inventoryChanges.Count != 1) return;
        var type = (ActionType?)inventoryChanges[0].Item.Base.ItemAction.ValueNullable?.Type;
        if (type != null) QueueUnlockCheck(inventoryChanges[0].Item);
    }

    private void ClientStateOnLogout(int type, int code)
    {
        UnlockedItems = new HashSet<uint>();
        _unlockedItemsToCheck.Clear();
        _pluginLog.Verbose("Character was logged out, clearing item unlocks.");
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        CheckUnlockedItemQueue();
    }

    private void CheckUnlockedItemQueue()
    {
        if (_unlockedItemsToCheck.TryPeek(out var result))
        {
            var item = _itemSheet.GetRow(result);
            var unlockStatus = IsUnlocked(item, false);
            if (unlockStatus != null)
            {
                if (_clientState.LocalContentId == 0)
                {
                    _unlockedItemsToCheck.Clear();
                    return;
                }

                _unlockedItemsToCheck.Dequeue();
                if (unlockStatus == true) UnlockedItems.Add(item.RowId);

                if (_unlockedItemsToCheck.Count == 0)
                {
                    _pluginLog.Verbose("Checked all items for acquisition status.");
                    Service.Framework.RunOnTick(() => { ItemUnlockStatusChanged?.Invoke(); });
                }
                else
                {
                    CheckUnlockedItemQueue();
                }
            }
        }
    }

    private void ClientStateOnLogin()
    {
        UnlockedItems = new HashSet<uint>();
        QueueAllUnlockedItems();
    }

    public void QueueAllUnlockedItems()
    {
        if (_clientState.LocalContentId == 0) return;
        _pluginLog.Verbose("Checking all valid items for unlock status.");
        foreach (var item in _dataManager.GetExcelSheet<Item>().Where(c => c.ItemAction.RowId != 0))
        {
            var type = (ActionType?)item.ItemAction.ValueNullable?.Type;
            if (type == null) continue;
            _unlockedItemsToCheck.Enqueue(item.RowId);
        }
    }

    public HashSet<uint> UnlockedItems { get; set; } = new();


    /// <summary>
    /// Returns the unlock status of an item
    /// </summary>
    /// <param name="itemRow">The item to check</param>
    /// <param name="notify">Should event subscribers be notified that an item was unlocked?</param>
    /// <returns>a boolean indicating if it is unlocked or null if the ExdModule has not loaded the required data</returns>
    public bool? IsUnlocked(ItemRow itemRow, bool notify = true)
    {
        bool? unlocked = null;
        var item = itemRow.Base;
        if (item.ItemAction.RowId == 0)
            return false;

        switch ((ActionType)item.ItemAction.Value.Type)
        {
            case ActionType.Companion:
                unlocked = UIState.Instance()->IsCompanionUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.BuddyEquip:
                unlocked = UIState.Instance()->Buddy.CompanionInfo.IsBuddyEquipUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.Mount:
                unlocked = PlayerState.Instance()->IsMountUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.SecretRecipeBook:
                unlocked = PlayerState.Instance()->IsSecretRecipeBookUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.UnlockLink:
                unlocked = UIState.Instance()->IsUnlockLinkUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.TripleTriadCard when item.AdditionalData.Is<TripleTriadCard>():
                unlocked = UIState.Instance()->IsTripleTriadCardUnlocked((ushort)item.AdditionalData.RowId);
                break;
            case ActionType.FolkloreTome:
                unlocked = PlayerState.Instance()->IsFolkloreBookUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.OrchestrionRoll when item.AdditionalData.Is<Orchestrion>():
                unlocked = PlayerState.Instance()->IsOrchestrionRollUnlocked(item.AdditionalData.RowId);
                break;
            case ActionType.FramersKit:
                unlocked = PlayerState.Instance()->IsFramersKitUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.Ornament:
                unlocked = PlayerState.Instance()->IsOrnamentUnlocked(item.ItemAction.Value.Data[0]);
                break;
            case ActionType.Glasses:
                unlocked = PlayerState.Instance()->IsGlassesUnlocked((ushort)item.AdditionalData.RowId);
                break;
        }

        if (unlocked != null)
        {
            if (unlocked == true)
            {
                UnlockedItems.Add(item.RowId);
                if (notify)
                {
                    Service.Framework.RunOnTick(() => { ItemUnlockStatusChanged?.Invoke(); });
                }
            }

            return unlocked;
        }

        var row = ExdModule.GetItemRowById(item.RowId);
        if (row == null) return null;
        return UIState.Instance()->IsItemActionUnlocked(row) == 1;
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _framework.Update -= FrameworkOnUpdate;
            _clientState.Login -= ClientStateOnLogin;
            _clientState.Logout -= ClientStateOnLogout;
            _inventoryMonitor.OnInventoryChanged -= ItemRemoved;
        }

        _disposed = true;
    }

    public event IUnlockTrackerService.ItemUnlockStatusChangedDelegate? ItemUnlockStatusChanged;

    public void QueueUnlockCheck(uint itemId)
    {
        _unlockedItemsToCheck.Enqueue(itemId);
    }

    public void QueueUnlockCheck(ItemRow item)
    {
        _unlockedItemsToCheck.Enqueue(item.RowId);
    }
}