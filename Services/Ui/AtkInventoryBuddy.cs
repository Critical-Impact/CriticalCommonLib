using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Addons;
using CriticalCommonLib.Enums;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkInventoryBuddy : AtkOverlay
    {
        public override void Update()
        {
            var currentTab = CurrentTab;
            if (currentTab != -1 && currentTab != _storedTab)
            {
                _storedTab = currentTab;
                SendUpdatedEvent();
            }
        }

        public override WindowName WindowName { get; set; } = WindowName.InventoryBuddy;
        private readonly int DragDropOffset = 10;
        private readonly int DragDropOffset2 = 46;
        private readonly int TabOffset = 7;
        private int? _storedTab;

        public AtkInventoryBuddy(IGameGui gameGui) : base(gameGui)
        {
        }

        public unsafe int CurrentTab
        {
            get
            {
                var addon = AtkUnitBase;
                if (addon != null && addon.AtkUnitBase != null)
                {
                    var buddyAddon = (InventoryBuddyAddon*) addon.AtkUnitBase;
                    return buddyAddon->CurrentTab;
                }
                return -1;
            }
        }

        public unsafe void SetColor(InventoryType bag, Vector2 position, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            var offset = DragDropOffset;
            if (bag == InventoryType.SaddleBag1 || bag == InventoryType.PremiumSaddleBag0)
            {
                offset = DragDropOffset2;
            }
            var nodeId = (uint)(position.X + (position.Y * 5) + offset);
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

            var offset = DragDropOffset;
            if (bag == InventoryType.SaddleBag1 || bag == InventoryType.PremiumSaddleBag1)
            {
                offset = DragDropOffset2;
            }

            foreach (var positionColor in positions)
            {
                Vector4? newColour = positionColor.Value;
                var position = positionColor.Key;

                var nodeId = (uint) (position.X + (position.Y * 5) + offset);
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