using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Agents
{
    [Agent(AgentId.Cabinet)]
    [StructLayout(LayoutKind.Explicit, Size = 70)]
    public unsafe partial struct CabinetWithdrawAgent
    {
        [FieldOffset(0)] public AgentInterface AgentInterface;
        [FieldOffset(69)] public byte SelectedTab;

        public CabinetCategory GetCabinetCategorySelected()
        {
            var selectedTab = SelectedTab;
            return Service.ExcelCache.GetCabinetCategorySheet().Single(c => c.MenuOrder == selectedTab);
        }
    }
}