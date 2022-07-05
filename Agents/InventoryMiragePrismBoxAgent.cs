using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Agents
{
    [Agent(AgentId.ArmouryBoard)]
    [StructLayout(LayoutKind.Explicit, Size = 59)]
    public unsafe partial struct InventoryMiragePrismBoxAgent
    {
        [FieldOffset(0)] public AgentInterface AgentInterface;
        [FieldOffset(58)] public byte SelectedPage;
    }
}