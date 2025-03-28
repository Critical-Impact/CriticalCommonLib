using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using CriticalCommonLib.Addons;
using CriticalCommonLib.Agents;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;


namespace CriticalCommonLib.Services.Ui
{
    public class AtkInventoryMiragePrismBox : AtkOverlay
    {
        public AtkInventoryMiragePrismBox(IGameGui gameGui) : base(gameGui)
        {
        }

        public override void Update()
        {

        }

        public override WindowName WindowName { get; set; } = WindowName.MiragePrismPrismBox;
        public int ButtonOffsetId = 32;
        public int RadioButtonOffsetId = 18;

        public enum DresserTab
        {
            MainHand = 0,
            OffHand = 1,
            Head = 2,
            Body = 3,
            Hands = 4,
            Legs = 5,
            Feet = 6,
            Ears = 7,
            Neck = 8,
            Wrists = 9,
            Fingers = 10
        }

        public static DresserTab? EquipSlotCategoryToDresserTab(EquipSlotCategory? category)
        {
            if (category == null)
            {
                return null;
            }
            if (category.Value.Body == 1)
            {
                return DresserTab.Body;
            }
            if (category.Value.Ears == 1)
            {
                return DresserTab.Ears;
            }
            if (category.Value.Feet == 1)
            {
                return DresserTab.Feet;
            }
            if (category.Value.Gloves == 1)
            {
                return DresserTab.Hands;
            }
            if (category.Value.Head == 1)
            {
                return DresserTab.Head;
            }
            if (category.Value.Legs == 1)
            {
                return DresserTab.Legs;
            }
            if (category.Value.Neck == 1)
            {
                return DresserTab.Neck;
            }
            if (category.Value.Wrists == 1)
            {
                return DresserTab.Wrists;
            }
            if (category.Value.FingerL == 1)
            {
                return DresserTab.Fingers;
            }
            if (category.Value.FingerR == 1)
            {
                return DresserTab.Fingers;
            }
            if (category.Value.MainHand == 1)
            {
                return DresserTab.MainHand;
            }
            if (category.Value.OffHand == 1)
            {
                return DresserTab.OffHand;
            }
            if (category.Value.SoulCrystal == 1)
            {
                return null;
            }
            return null;
        }

        public unsafe int CurrentTab
        {
            get
            {
                var addon = AtkUnitBase;
                if (addon != null && addon.AtkUnitBase != null)
                {
                    var agentMiragePrismPrismBox = AgentMiragePrismPrismBox.Instance();
                    if (agentMiragePrismPrismBox != null)
                    {
                        return agentMiragePrismPrismBox->TabIndex;
                    }
                }
                return -1;
            }
        }

        public unsafe uint ClassJobSelected
        {
            get
            {
                var addon = AtkUnitBase;
                if (addon != null && addon.AtkUnitBase != null)
                {
                    var actualAddon = (AddonMiragePrismPrismBox*)addon.AtkUnitBase;
                    return (uint)actualAddon->Param;
                }
                return 0;
            }
        }

        public unsafe bool OnlyDisplayRaceGenderItems
        {
            get
            {
                var addon = AtkUnitBase;
                if (addon != null && addon.AtkUnitBase != null)
                {
                    return false;
                }
                return false;
            }
        }

        public unsafe int CurrentPage
        {
            get
            {
                var agent = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework
                    .Instance()->UIModule->GetAgentModule()->GetAgentMiragePrismPrismBox();
                if (agent->IsAgentActive())
                {
                    return agent->PageIndex;
                }
                return -1;
            }
        }

        public unsafe void SetColors(Dictionary<Vector2, Vector4?> positions)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;

            foreach (var positionColor in positions)
            {
                Vector4? newColour = positionColor.Value;
                var position = positionColor.Key;

                var nodeId = (uint) (position.X + (position.Y * 10) + ButtonOffsetId);
                var componentNode = (AtkComponentNode*) atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
                if (componentNode == null || (ushort) componentNode->AtkResNode.Type < 1000) return;
                var atkResNode = (AtkResNode*) componentNode;
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

        public unsafe void SetColor(Vector2 position, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;

            var nodeId = (uint)(position.X + (position.Y * 10) + ButtonOffsetId);
            var buttonComponentNode = (AtkComponentNode*)atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
            if (buttonComponentNode == null || (ushort)buttonComponentNode->AtkResNode.Type < 1000) return;
            var atkResNode = (AtkResNode*) buttonComponentNode;
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

        public unsafe void SetTabColors(Dictionary<uint, Vector4?> indexedTabColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            foreach (var colour in indexedTabColours)
            {
                Vector4? newColour = colour.Value;
                var tab = colour.Key;

                var nodeId = (uint) (tab + RadioButtonOffsetId);
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