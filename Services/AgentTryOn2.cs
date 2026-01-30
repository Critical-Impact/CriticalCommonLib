using System.Runtime.InteropServices;

namespace CriticalCommonLib.Services;

//TODO: Remove me once CS has SaveDeleteOutfit
[StructLayout(LayoutKind.Explicit, Size = 0x368)]
public struct AgentTryOn2
{
    [FieldOffset(0x366)] public bool SaveDeleteOutfit;
}