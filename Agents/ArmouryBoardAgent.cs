using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Agents
{
    [Agent(AgentId.ArmouryBoard)]
    [StructLayout(LayoutKind.Explicit, Size = 0x2E)]
    public unsafe partial struct ArmouryBoard
    {
        [FieldOffset(0x0)] public AgentInterface AgentInterface;
        [FieldOffset(0x2C)] public byte SelectedTab;
    }
}