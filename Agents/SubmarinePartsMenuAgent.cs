using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Agents
{
    using FFXIVClientStructs.FFXIV.Client.UI.Agent;

    //Agent: 234
    [StructLayout(LayoutKind.Explicit, Size = 70)]
    public unsafe partial struct SubmarinePartsMenuAgent
    {
        [FieldOffset(0)] public AgentInterface AgentInterface;
        [FieldOffset(148)] public uint ResultItem;
        [FieldOffset(156)] public unsafe fixed uint RequiredItems[6];
    }
}