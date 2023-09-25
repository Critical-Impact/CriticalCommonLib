using System;
using System.Collections.Generic;
using CriticalCommonLib.Sheets;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace CriticalCommonLib {
    public class TryOn : IDisposable
    {
        private int tryOnDelay = 10;
        private readonly Queue<(uint itemid, byte stainId)> tryOnQueue = new();

        public TryOn()
        {
            try {
                CanUseTryOn = true;
                Service.Framework.Update += FrameworkUpdate;
            } catch (Exception ex) {
                Service.Log.Error(ex.ToString());
            }
        }

        public bool CanUseTryOn { get; }

        public void TryOnItem(ItemEx item, byte stainId = 0, bool hq = false)
        {
#if DEBUG
            Service.Log.Debug($"Try On: {item.NameString}");
#endif
            if (item.EquipSlotCategory?.Value == null) return;
            if (item.EquipSlotCategory.Row > 0 && item.EquipSlotCategory.Row != 6 && item.EquipSlotCategory.Row != 17 && (item.EquipSlotCategory.Value.OffHand <=0 || item.ItemUICategory.Row == 11)) {
                tryOnQueue.Enqueue((item.RowId + (uint) (hq ? 1000000 : 0), stainId));
            }
#if DEBUG
            else {
                Service.Log.Error($"Cancelled Try On: Invalid Item. ({item.EquipSlotCategory.Row}, {item.EquipSlotCategory.Value.OffHand}, {item.EquipSlotCategory.Value.Waist}, {item.EquipSlotCategory.Value.SoulCrystal})");
            }
#endif
        }

        public void OpenFittingRoom() {
            tryOnQueue.Enqueue((0, 0));
        }

        
        public void FrameworkUpdate(IFramework framework) {
            
            while (CanUseTryOn && tryOnQueue.Count > 0 && (tryOnDelay <= 0 || tryOnDelay-- <= 0)) {
                try {
                    var (itemId, stainId) = tryOnQueue.Dequeue();
                    tryOnDelay = 1;
                    AgentTryon.TryOn(0, itemId, stainId, 0, 0);
                } catch {
                    tryOnDelay = 5;
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
                    Service.Framework.Update -= FrameworkUpdate;
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
                Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}
