using System;
using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Rows;

using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace CriticalCommonLib.Services;

public class TryOn : IDisposable
{
    private readonly IFramework _framework;
    private readonly IPluginLog _pluginLog;
    private int _tryOnDelay = 10;
    private readonly Queue<(uint itemid, byte stainId, bool keepApplied)> _tryOnQueue = new();

    public TryOn(IFramework framework, IPluginLog pluginLog)
    {
        _framework = framework;
        _pluginLog = pluginLog;
        try {
            CanUseTryOn = true;
            _framework.Update += FrameworkUpdate;
        } catch (Exception ex) {
            _pluginLog.Error(ex.ToString());
        }
    }

    public bool CanUseTryOn { get; }

    public void TryOnItem(List<ItemRow> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            TryOnItem(item, 0, false, index != 0);
        }
    }

    public void TryOnItem(List<RowRef<Item>> items)
    {
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            TryOnItem(item, 0, false, index != 0);
        }
    }

    public void TryOnItem(ItemRow item, byte stainId = 0, bool hq = false, bool keepApplied = false)
    {
        if (item.EquipSlotCategory == null) return;
        if (item.EquipSlotCategory.RowId > 0 && item.EquipSlotCategory.RowId != 6 && item.EquipSlotCategory.RowId != 17 && (item.EquipSlotCategory?.Base.OffHand <=0 || item.Base.ItemUICategory.RowId == 11)) {
            _tryOnQueue.Enqueue((item.RowId + (uint) (hq ? 1000000 : 0), stainId, keepApplied));
        }
    }

    public void TryOnItem(RowRef<Item> item, byte stainId = 0, bool hq = false, bool keepApplied = false)
    {
        if (!item.IsValid) return;
        if (!item.Value.EquipSlotCategory.IsValid) return;
        if (item.Value.EquipSlotCategory.RowId > 0 && item.Value.EquipSlotCategory.RowId != 6 && item.Value.EquipSlotCategory.RowId != 17 && (item.Value.EquipSlotCategory.ValueNullable?.OffHand <=0 || item.Value.ItemUICategory.RowId == 11)) {
            _tryOnQueue.Enqueue((item.RowId + (uint) (hq ? 1000000 : 0), stainId, keepApplied));
        }
    }

    public void OpenFittingRoom() {
        _tryOnQueue.Enqueue((0, 0, false));
    }


    public unsafe void FrameworkUpdate(IFramework framework) {

        while (CanUseTryOn && _tryOnQueue.Count > 0 && (_tryOnDelay <= 0 || _tryOnDelay-- <= 0)) {
            try {
                var (itemId, stainId, keepApplied) = _tryOnQueue.Dequeue();
                var charaView = (AgentTryOn2*)AgentTryon.Instance();
                charaView->SaveDeleteOutfit = keepApplied;
                _tryOnDelay = 1;
                AgentTryon.TryOn(0, itemId, stainId);
            } catch {
                _tryOnDelay = 5;
                break;
            }
        }
    }

    private bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if(!_disposed && disposing)
        {
            if (CanUseTryOn)
            {
                _framework.Update -= FrameworkUpdate;
            }
        }
        _disposed = true;
    }

    ~TryOn()
    {
#if DEBUG
        // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
        // disposed by the programmer.

        if( _disposed == false )
        {
            _pluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
        }
#endif
        Dispose (true);
    }
}