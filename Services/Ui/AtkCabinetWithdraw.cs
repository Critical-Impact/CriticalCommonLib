using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Agents;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services.Ui
{
    using System.Runtime.InteropServices;
    using Addons;

    public class AtkCabinetWithdraw : AtkOverlay
    {
        public override WindowName WindowName { get; set; } = WindowName.CabinetWithdraw;
        private uint RadioButtonOffset = 12;
        private uint ListComponentNodeId = 30;

        public unsafe CabinetCategory? CurrentTab
        {
            get
            {
                if (AtkUnitBase != null)
                {
                    var cabinetWithdrawAddon = (AddonCabinetWithdraw*)this.AtkUnitBase.AtkUnitBase;
                    return cabinetWithdrawAddon->GetCabinetCategorySelected();
                }
                return null;
            }
        }

        private CabinetCategory? _storedTab;
        
        public override void Update()
        {
            var currentTab = CurrentTab;
            if (currentTab != null && currentTab != _storedTab)
            {
                _storedTab = currentTab;
                SendUpdatedEvent();
            }
        }

        public unsafe void SetColours(Dictionary<string, Vector4?> colours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            var listComponentNode = (AtkComponentNode*) atkBaseWrapper.AtkUnitBase->GetNodeById(ListComponentNodeId);
            if (listComponentNode == null || (ushort) listComponentNode->AtkResNode.Type < 1000) return;
            var component = (AtkComponentTreeList*) listComponentNode->Component;
            var list = component->Items;
            foreach(var listItem in list.Span)
            {
                var uldManager = listItem.Value->Renderer->AtkComponentButton.AtkComponentBase.UldManager;
                if (uldManager.NodeListCount < 4) continue;
                var atkResNode = uldManager.NodeList[3];
                var textNode = (AtkTextNode*) atkResNode;
                
                if (textNode == null) {
                    continue;
                }
                    
                if (textNode->NodeText.StringPtr[0] == 0x20) continue;
                var priceString = Utils.ReadSeString(textNode->NodeText).TextValue;
                priceString = priceString.Substring(0, priceString.Length - 1);
                textNode->SetText(priceString);
                if (colours.ContainsKey(priceString))
                {
                    var newColour = colours[priceString];
                    if (newColour.HasValue)
                    {
                        textNode->TextColor = Utils.ColorFromVector4(newColour.Value);
                    }
                    else
                    {
                        textNode->TextColor =  Utils.ColorFromVector4(ImGuiColors.DalamudWhite);
                    }
                }
                else
                {
                    textNode->TextColor =  Utils.ColorFromVector4(ImGuiColors.DalamudWhite);
                }
            }
        }

        public unsafe void SetTabColors(Dictionary<uint, Vector4?> indexedTabColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            
            foreach (var colour in indexedTabColours)
            {
                Vector4? newColour = colour.Value;
                var tab = colour.Key;
                var nodeId = (uint) (tab + RadioButtonOffset);
                var radioButton = (AtkComponentNode*) atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
                if (radioButton == null || (ushort) radioButton->AtkResNode.Type < 1000) return;
                var atkResNode = (AtkResNode*) radioButton;
                if (newColour.HasValue)
                {
                    atkResNode->Color.A = (byte) (newColour.Value.W * 255.0f);
                    atkResNode->AddBlue = (short) (newColour.Value.Z * 255.0f);
                    atkResNode->AddRed = (short) (newColour.Value.X * 255.0f);
                    atkResNode->AddGreen = (short) (newColour.Value.Y * 255.0f);
                }
                else
                {
                    atkResNode->Color.A = 255;
                    atkResNode->AddBlue = 0;
                    atkResNode->AddRed = 0;
                    atkResNode->AddGreen = 0;
                }
            }
        }
    }
}