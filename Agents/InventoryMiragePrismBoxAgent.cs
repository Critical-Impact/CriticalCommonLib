using System;
using System.Runtime.InteropServices;
using CriticalCommonLib.Models;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Agents
{
    [StructLayout(LayoutKind.Explicit, Size = 114336)]
    public unsafe partial struct InventoryMiragePrismBoxAgentSettings
    {
        [FieldOffset(176 + (136 * 800) + 4165)] public InventoryMiragePrismBoxSearchOrder SearchOrder;
        [FieldOffset(176 + (136 * 800) + 4184)] public byte SearchLevel;
        [FieldOffset(176 + (136 * 800) + 4186)] public InventoryMiragePrismBoxGender SearchGender;
        [FieldOffset(176 + (136 * 800) + 4186 + 6)] public Utf8String SearchText;
        [FieldOffset(176 + (136 * 800) + 4186 + 110)] public Utf8String QuickSearchText;
    }

    public enum InventoryMiragePrismBoxGender
    {
        NotSpecified = 0,
        CurrentGender = 1,
        Male = 2,
        Female = 3
    }
    public enum InventoryMiragePrismBoxSearchOrder
    {
        Descending = 0,
        Ascending = 2
    }
    
    [Agent(AgentId.MiragePrismPrismBox)]
    [StructLayout(LayoutKind.Explicit, Size = 59)]
    public unsafe partial struct InventoryMiragePrismBoxAgent
    {
        [FieldOffset(0)] public AgentInterface AgentInterface;
        [FieldOffset(40)] public InventoryMiragePrismBoxAgentSettings* InterfaceSettings;
        [FieldOffset(58)] public byte SelectedPage;
        
        public InventoryMiragePrismBoxGender SearchGender
        {
            get
            {
                if (!AgentInterface.IsAgentActive()) return 0;
                return InterfaceSettings->SearchGender;
            }
        }
        
        public IntPtr* SearchGenderPtr
        {
            get
            {
                return (nint*)(&InterfaceSettings->SearchGender);
            }
        }

        public byte SearchLevel
        {
            get
            {
                if (!AgentInterface.IsAgentActive()) return 0;
                return InterfaceSettings->SearchLevel;
            }
        }

        public string SearchText
        {
            get
            {
                if (!AgentInterface.IsAgentActive()) return "";
                return InterfaceSettings->SearchText.ToString();
            }
        }
        public string QuickSearchText
        {
            get
            {
                if (!AgentInterface.IsAgentActive()) return "";
                return InterfaceSettings->QuickSearchText.ToString();
            }
        }

        public InventoryMiragePrismBoxSearchOrder SearchOrder
        {
            get
            {
                if (!AgentInterface.IsAgentActive()) return InventoryMiragePrismBoxSearchOrder.Descending;
                return InterfaceSettings->SearchOrder;
            }
        }

        public GlamourItem[] GlamourItems {
            get {
                GlamourItem[] pages = new GlamourItem[800];

                if (!AgentInterface.IsAgentActive()) return pages;
                
                var agents = Framework.Instance()->GetUiModule()->GetAgentModule();
                var dresserAgent = agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);

                var glamPlatePointer = *(IntPtr*)((IntPtr)dresserAgent + 40) + 176;
                if (glamPlatePointer == IntPtr.Zero) return pages;
                
                for (int plateNumber = 0; plateNumber < 800; plateNumber++) {
                    var glamItem = *(GlamourItem*)(glamPlatePointer + plateNumber * 136);
                    
                    pages[plateNumber] = glamItem;

                }
                return pages;
            }
        }
    }
}