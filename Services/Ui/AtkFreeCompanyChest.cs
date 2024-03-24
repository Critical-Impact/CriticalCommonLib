using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Addons;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkFreeCompanyChest : AtkOverlay
    {
        public override WindowName WindowName { get; set; } = WindowName.FreeCompanyChest;
        private readonly int DragDropOffset = 23;
        private readonly int TabOffset = 10;

        public unsafe FreeCompanyTab CurrentTab
        {
            get
            {
                var addon = AtkUnitBase;
                if (addon != null && addon.AtkUnitBase != null)
                {
                    var largeAddon = (InventoryFreeCompanyChestAddon*) addon.AtkUnitBase;
                    return largeAddon->CurrentTab;
                }
                return FreeCompanyTab.Unknown;
            }
        }

        private FreeCompanyTab? _storedTab = null;
        
        public override void Update()
        {
            var currentTab = CurrentTab;
            if (currentTab != FreeCompanyTab.Unknown && currentTab != _storedTab)
            {
                _storedTab = currentTab;
                SendUpdatedEvent();
            }
        }

        public unsafe void SetColor(Vector2 position, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            var nodeId = (uint)(position.X + (position.Y * 5) + DragDropOffset);
            
            var dragDropNode = (AtkComponentNode*)atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
            if (dragDropNode == null || (ushort)dragDropNode->AtkResNode.Type < 1000) return;
            var atkResNode = (AtkResNode*) dragDropNode;
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

        public unsafe void SetTabColors(Dictionary<FreeCompanyTab, Vector4?> indexedTabColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;

            foreach (var colour in indexedTabColours)
            {
                Vector4? newColour = colour.Value;
                var actualTab = colour.Key switch
                {
                    FreeCompanyTab.One => 0,
                    FreeCompanyTab.Two => 1,
                    FreeCompanyTab.Three => 2,
                    FreeCompanyTab.Four => 3,
                    FreeCompanyTab.Five => 4,
                    _ => 0
                };

                var nodeId = (uint) (actualTab + TabOffset);
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
        
        public unsafe void SetColors(Dictionary<Vector2, Vector4?> positions)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;

            foreach (var positionColor in positions)
            {
                Vector4? newColour = positionColor.Value;
                var position = positionColor.Key;
                
                var nodeId = (uint) (position.X + (position.Y * 5) + DragDropOffset);
                var dragDropNode = (AtkComponentNode*) atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
                if (dragDropNode == null || (ushort) dragDropNode->AtkResNode.Type < 1000) return;
                var atkResNode = (AtkResNode*) dragDropNode;
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