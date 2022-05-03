using System.Linq;
using System.Runtime.InteropServices;
using CriticalCommonLib.Services;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
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
            return ExcelCache.GetSheet<CabinetCategory>().Single(c => c.MenuOrder == selectedTab);
        }
    }
}