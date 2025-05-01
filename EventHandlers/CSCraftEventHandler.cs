using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace CriticalCommonLib.EventHandlers;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct CSCraftEventHandler
{
    [FieldOffset(0x48A)] public unsafe fixed ushort WKSClassLevels[2];
    [FieldOffset(0x48E)] public unsafe fixed ushort WKSClassJobs[2];

    public static CSCraftEventHandler* Instance()
    {
        return (CSCraftEventHandler*)EventFramework.Instance()->GetCraftEventHandler();
    }
}