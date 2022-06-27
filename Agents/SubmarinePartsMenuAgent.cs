using System.Linq;
using System.Runtime.InteropServices;
using CriticalCommonLib.Services;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Agents
{
    //Agent: 234
    [StructLayout(LayoutKind.Explicit, Size = 70)]
    public unsafe partial struct SubmarinePartsMenuAgent
    {
        [FieldOffset(0)] public AgentInterface AgentInterface;
        [FieldOffset(148)] public uint ResultItem;
        [FieldOffset(156)] public unsafe fixed uint RequiredItems[6];
    }
}