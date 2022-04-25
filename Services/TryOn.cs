

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CriticalCommonLib.Resolvers;
using Dalamud.Game;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Logging;

namespace CriticalCommonLib {
    public class TryOn : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate byte TryOnDelegate(uint unknownCanEquip, uint itemBaseId, ulong stainColor, uint itemGlamourId, byte unknownByte);

        private readonly TryOnDelegate? tryOn;

        private int tryOnDelay = 10;

        private readonly Queue<(uint itemid, uint stain)> tryOnQueue = new();


        public TryOn()
        {
            try {
                var address = new AddressResolver();
                address.Setup(Service.Scanner);
                tryOn = Marshal.GetDelegateForFunctionPointer<TryOnDelegate>(address.TryOn);
                CanUseTryOn = true;
                Service.Framework.Update += FrameworkUpdate;
            } catch (Exception ex) {
                PluginLog.LogError(ex.ToString());
            }
        }

        public bool CanUseTryOn { get; }

        public void TryOnItem(Item item, uint stain = 0, bool hq = false)
        {
#if DEBUG
            PluginLog.Debug($"Try On: {item.Name}");
#endif
            if (item.EquipSlotCategory?.Value == null) return;
            if (item.EquipSlotCategory.Row > 0 && item.EquipSlotCategory.Row != 6 && item.EquipSlotCategory.Row != 17 && (item.EquipSlotCategory.Value.OffHand <=0 || item.ItemUICategory.Row == 11)) {
                tryOnQueue.Enqueue((item.RowId + (uint) (hq ? 1000000 : 0), stain));
            }
#if DEBUG
            else {
                PluginLog.Error($"Cancelled Try On: Invalid Item. ({item.EquipSlotCategory.Row}, {item.EquipSlotCategory.Value.OffHand}, {item.EquipSlotCategory.Value.Waist}, {item.EquipSlotCategory.Value.SoulCrystal})");
            }
#endif
        }

        public void OpenFittingRoom() {
            tryOnQueue.Enqueue((0, 0));
        }

        
        public void FrameworkUpdate(Framework framework) {
            
            while (tryOn != null && CanUseTryOn && tryOnQueue.Count > 0 && (tryOnDelay <= 0 || tryOnDelay-- <= 0)) {
                try {
                    var (itemId, stain) = tryOnQueue.Dequeue();
                    tryOnDelay = 1;
                    tryOn(0xFF, itemId, stain, 0, 0);

                } catch {
                    tryOnDelay = 5;
                    break;
                }
            }
        }


        public void Dispose() {
            if (CanUseTryOn)
            {
                Service.Framework.Update -= FrameworkUpdate;
            }
        }
    }
}
