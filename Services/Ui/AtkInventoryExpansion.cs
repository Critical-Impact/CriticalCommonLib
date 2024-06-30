using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Enums;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkInventoryExpansion : AtkOverlay
    {
        public override WindowName WindowName { get; set; } = WindowName.InventoryExpansion;
        private readonly int DragDropOffset = 3;
        //Within the drag/drop component
        public readonly uint IconNodeId = 2;
        public readonly uint TextLeftNodeId = 136;
        public readonly uint TextRightNodeId = 137;
        
        public override HashSet<WindowName>? ExtraWindows { get; } = new HashSet<WindowName>()
        {
            WindowName.InventoryGrid0E,
            WindowName.InventoryGrid1E,
            WindowName.InventoryGrid2E,
            WindowName.InventoryGrid3E,
        };

        public override void Update()
        {
            
        }

        public unsafe void SetColor(InventoryType bag, Vector2 position, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            AtkBaseWrapper? bagBase = null;
            if (bag == InventoryType.Bag0)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0E);
            }
            else if(bag == InventoryType.Bag1)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1E);
            }
            else if(bag == InventoryType.Bag2)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid2E);
            }
            else if(bag == InventoryType.Bag3)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid3E);
            }

            if (bagBase != null)
            {
                var nodeId = (uint)(position.X + (position.Y * 5) + DragDropOffset);
                var dragDropNode = (AtkComponentNode*)bagBase.AtkUnitBase->GetNodeById(nodeId);
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
        }
        public unsafe void SetColors(InventoryType bag, Dictionary<Vector2, Vector4?> positions)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            AtkBaseWrapper? bagBase = null;
            if (bag == InventoryType.Bag0)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0E);
            }
            else if(bag == InventoryType.Bag1)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1E);
            }
            else if(bag == InventoryType.Bag2)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid2E);
            }
            else if(bag == InventoryType.Bag3)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid3E);
            }

            if (bagBase != null)
            {
                foreach (var positionColor in positions)
                {
                    Vector4? newColour = positionColor.Value;
                    var position = positionColor.Key;
                    
                    var nodeId = (uint) (position.X + (position.Y * 5) + DragDropOffset);
                    var dragDropNode = (AtkComponentNode*) bagBase.AtkUnitBase->GetNodeById(nodeId);
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
        
        public unsafe void HideIcons(InventoryType bag)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;

            AtkBaseWrapper? bagBase = null;
            if (bag == InventoryType.Bag0)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0E);
            }
            else if(bag == InventoryType.Bag1)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1E);
            }
            else if(bag == InventoryType.Bag2)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid2E);
            }
            else if(bag == InventoryType.Bag3)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid3E);
            }

            if (bagBase != null)
            {
                for (int x = 0; x < 35; x++)
                {
                    var nodeId = (uint) (x + DragDropOffset);
                    var dragDropNode = (AtkComponentNode*) bagBase.AtkUnitBase->GetNodeById(nodeId);
                    if (dragDropNode == null || (ushort) dragDropNode->AtkResNode.Type < 1000) return;

                    var iconNode = (AtkComponentNode*) dragDropNode->Component->UldManager.SearchNodeById(IconNodeId);
                    if (iconNode == null) continue;
                    if ((ushort) iconNode->AtkResNode.Type < 1000) return;
                    var isVisible = iconNode->AtkResNode.IsVisible();
                    if (isVisible)
                    {
                        iconNode->AtkResNode.NodeFlags ^= NodeFlags.Visible;
                    }
                }
            }
        }
        
        public unsafe void SetText(string leftText, string rightText)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;

            var leftTextNode = (AtkTextNode*)atkBaseWrapper.AtkUnitBase->GetNodeById(TextLeftNodeId);
            var rightTextNode = (AtkTextNode*)atkBaseWrapper.AtkUnitBase->GetNodeById(TextRightNodeId);
            if (leftTextNode != null)
            {
                leftTextNode->SetText(leftText);
            }
            if (rightTextNode != null)
            {
                rightTextNode->SetText(rightText);
            }
        }
    }
}