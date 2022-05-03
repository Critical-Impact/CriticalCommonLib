using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib.Addons;
using CriticalCommonLib.Agents;
using CriticalCommonLib.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services.Ui
{
    public abstract class AtkCabinetWithdraw : AtkOverlay
    {
        public override WindowName WindowName { get; set; } = WindowName.CabinetWithdraw;
        public override bool ShouldDraw { get; set; }
        private uint RadioButtonOffset = 3;
        private uint ListComponentNodeId = 24;

        public unsafe CabinetCategory? CurrentTab
        {
            get
            {
                var agent = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework
                    .Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.Cabinet);
                if (agent->IsAgentActive())
                {
                    var armouryAgent = (CabinetWithdrawAgent*) agent;
                    return armouryAgent->GetCabinetCategorySelected();
                }
                return null;
            }
        }

        private CabinetCategory? _storedTab = null;
        
        public override void Update()
        {
            var currentTab = CurrentTab;
            if (currentTab != null && currentTab != _storedTab)
            {
                _storedTab = currentTab;
                Draw();
            }
        }

        public unsafe void SetColours(Dictionary<string, Vector4?> colours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            var listComponentNode = (AtkComponentNode*) atkBaseWrapper.AtkUnitBase->GetNodeById(ListComponentNodeId);
            if (listComponentNode == null || (ushort) listComponentNode->AtkResNode.Type < 1000) return;
            var component = (AtkComponentList*) listComponentNode->Component;
            for (var i = 0; i < 15 && i < component->ListLength; i++)
            {
                var listItem = component->ItemRendererList[i].AtkComponentListItemRenderer;

                var uldManager = listItem->AtkComponentButton.AtkComponentBase.UldManager;
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
                    atkResNode->AddBlue = (ushort) (newColour.Value.Z * 255.0f);
                    atkResNode->AddRed = (ushort) (newColour.Value.X * 255.0f);
                    atkResNode->AddGreen = (ushort) (newColour.Value.Y * 255.0f);
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