using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.CustomAtk
{
    [StructLayout(LayoutKind.Explicit, Size = 248)]
    public struct AtkComponentRadioButton
    {
        [FieldOffset(0)]
        public AtkComponentBase AtkComponentBase;

        [FieldOffset(234)] public byte IsCheckedByte;

        public bool IsChecked => IsCheckedByte == 4;
    }
}