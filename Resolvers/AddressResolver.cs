using Dalamud.Game;
using System;

namespace CriticalCommonLib.Resolvers {
    class AddressResolver : BaseAddressResolver {
        public IntPtr TryOn { get; private set; }
        protected override void Setup64Bit(SigScanner sig) {
            this.TryOn = sig.ScanText("E8 ?? ?? ?? ?? EB 35 BA ?? ?? ?? ??");
        }
    }
}