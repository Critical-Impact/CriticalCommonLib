using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Addons;
using CriticalCommonLib.Enums;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkInventoryLarge : AtkOverlay
    {
        public AtkInventoryLarge(IGameGui gameGui) : base(gameGui)
        {
        }
        
        public override WindowName WindowName { get; set; } = WindowName.InventoryLarge;
        private readonly int DragDropOffset = 3;
        private readonly int TabOffset = 7;
        
        private int? _storedTab;

        public override HashSet<WindowName>? ExtraWindows { get; } = new HashSet<WindowName>()
        {
            WindowName.InventoryGrid0,
            WindowName.InventoryGrid1
        };
        
        public override void Update()
        {
            var currentTab = CurrentTab;
            if (currentTab != -1 && currentTab != _storedTab)
            {
                _storedTab = currentTab;
                SendUpdatedEvent();
            }
        }
        
        public unsafe int CurrentTab
        {
            get
            {
                var addon = AtkUnitBase;
                if (addon != null && addon.AtkUnitBase != null)
                {
                    var largeAddon = (InventoryLargeAddon*) addon.AtkUnitBase;
                    return largeAddon->CurrentTab;
                }
                return -1;
            }
        }
        
        public unsafe void SetColor(InventoryType bag, Vector2 position, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            AtkBaseWrapper? bagBase = null;
            if (bag == InventoryType.Bag0)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0);
            }
            else if(bag == InventoryType.Bag1)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1);
            }
            else if(bag == InventoryType.Bag2)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0);
            }
            else if(bag == InventoryType.Bag3)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1);
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

        public unsafe void SetTabColors(Dictionary<uint, Vector4?> indexedTabColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            foreach (var colour in indexedTabColours)
            {
                Vector4? newColour = colour.Value;
                var tab = colour.Key;
                
                var nodeId = (uint) (tab + TabOffset);
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
        
        public unsafe void SetColors(InventoryType bag, Dictionary<Vector2, Vector4?> positions)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            AtkBaseWrapper? bagBase = null;
            if (bag == InventoryType.Bag0)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0);
            }
            else if(bag == InventoryType.Bag1)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1);
            }
            else if(bag == InventoryType.Bag2)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid0);
            }
            else if(bag == InventoryType.Bag3)
            {
                bagBase = GetAtkUnitBase(WindowName.InventoryGrid1);
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
    }
}