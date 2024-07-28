using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Addons;
using CriticalCommonLib.Agents;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkInventoryMiragePrismBox : AtkOverlay
    {
        public override void Update()
        {
            
        }

        public override WindowName WindowName { get; set; } = WindowName.MiragePrismPrismBox;
        public int ButtonOffsetId = 31;
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
            if (category.Body == 1)
            {
                return DresserTab.Body;
            }
            if (category.Ears == 1)
            {
                return DresserTab.Ears;
            }
            if (category.Feet == 1)
            {
                return DresserTab.Feet;
            }
            if (category.Gloves == 1)
            {
                return DresserTab.Hands;
            }
            if (category.Head == 1)
            {
                return DresserTab.Head;
            }
            if (category.Legs == 1)
            {
                return DresserTab.Legs;
            }
            if (category.Neck == 1)
            {
                return DresserTab.Neck;
            }
            if (category.Wrists == 1)
            {
                return DresserTab.Wrists;
            }
            if (category.FingerL == 1)
            {
                return DresserTab.Fingers;
            }
            if (category.FingerR == 1)
            {
                return DresserTab.Fingers;
            }
            if (category.MainHand == 1)
            {
                return DresserTab.MainHand;
            }
            if (category.OffHand == 1)
            {
                return DresserTab.OffHand;
            }
            if (category.SoulCrystal == 1)
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
                    var actualAddon = (InventoryMiragePrismBoxAddon*) addon.AtkUnitBase;
                    return actualAddon->SelectedTab;
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
                    var actualAddon = (InventoryMiragePrismBoxAddon*) addon.AtkUnitBase;
                    return actualAddon->ClassJobSelected;
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
                    var actualAddon = (InventoryMiragePrismBoxAddon*) addon.AtkUnitBase;
                    return false;
                    //return actualAddon->OnlyDisplayRaceGenderItems == 1;
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