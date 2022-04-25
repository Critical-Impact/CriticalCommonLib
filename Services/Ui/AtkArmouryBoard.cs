using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Agents;
using CriticalCommonLib.Enums;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public abstract class AtkArmouryBoard : AtkOverlay
    {
        public override WindowName WindowName { get; set; } = WindowName.ArmouryBoard;
        public int DragDropOffset = 71;
        //Within the drag/drop component
        public uint IconNodeId = 2;
        //Within the icon node
        public uint ImageNodeId = 9;
        
        public int RadioButtonOffset = 7;

        public unsafe int CurrentTab
        {
            get
            {
                var agent = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework
                    .Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.ArmouryBoard);
                if (agent->IsAgentActive())
                {
                    var armouryAgent = (ArmouryBoard*) agent;
                    return armouryAgent->SelectedTab;
                }
                return -1;
            }
        }

        public InventoryType? CurrentBagLocation
        {
            get
            {
                return NumberToBag.ContainsKey(CurrentTab) ? NumberToBag[CurrentTab] : null;
            }
        }

        public Dictionary<InventoryType, int> BagToNumber = new()
        {
            {InventoryType.ArmoryMain, 0},
            {InventoryType.ArmoryHead, 1},
            {InventoryType.ArmoryBody, 2},
            {InventoryType.ArmoryHand, 3},
            {InventoryType.ArmoryLegs, 4},
            {InventoryType.ArmoryFeet, 5},
            {InventoryType.ArmoryOff, 6},
            {InventoryType.ArmoryEar, 7},
            {InventoryType.ArmoryNeck, 8},
            {InventoryType.ArmoryWrist, 9},
            {InventoryType.ArmoryRing, 10},
            {InventoryType.ArmorySoulCrystal, 11},
        };

        public Dictionary<int, InventoryType> NumberToBag = new()
        {
            {0,InventoryType.ArmoryMain},
            {1,InventoryType.ArmoryHead},
            {2,InventoryType.ArmoryBody},
            {3,InventoryType.ArmoryHand},
            {4,InventoryType.ArmoryLegs},
            {5,InventoryType.ArmoryFeet},
            {6,InventoryType.ArmoryOff},
            {7,InventoryType.ArmoryEar},
            {8,InventoryType.ArmoryNeck},
            {9,InventoryType.ArmoryWrist},
            {10,InventoryType.ArmoryRing},
            {11,InventoryType.ArmorySoulCrystal},
        };
        
        

        public unsafe void SetTabColors(Dictionary<InventoryType, Vector4?> indexedTabColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            foreach (var colour in indexedTabColours)
            {
                Vector4? newColour = colour.Value;
                var tab = colour.Key;
                var tabNumber = BagToNumber[tab];
                
                var nodeId = (uint) (tabNumber + RadioButtonOffset);
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

        
        public unsafe void SetColors(InventoryType bag, Dictionary<Vector2, Vector4?> positions)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            if (!BagToNumber.ContainsKey(bag))
            {
                PluginLog.Error("bag to number does not contain " + bag);
                return;
            }

            var bagLocation = BagToNumber[bag];
            if (bagLocation == CurrentTab)
            {
                foreach (var positionColor in positions)
                {
                    Vector4? newColour = positionColor.Value;
                    var position = positionColor.Key;
                
                    var nodeId = (uint) (position.X + DragDropOffset);
                    var dragDropNode = (AtkComponentNode*) atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
                    if (dragDropNode == null || (ushort) dragDropNode->AtkResNode.Type < 1000) return;
                    var atkResNode = (AtkResNode*) dragDropNode;
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

                    var iconNode = (AtkComponentNode*) dragDropNode->Component->UldManager.SearchNodeById(IconNodeId);
                    if (iconNode == null || (ushort) iconNode->AtkResNode.Type < 1000) continue;
                    
                    var imageNode = iconNode->Component->UldManager.SearchNodeById(ImageNodeId);
                    if (imageNode == null) continue;

                    imageNode->Color.A = 255;
                    imageNode->MultiplyRed = 100;
                    imageNode->MultiplyGreen = 100;
                    imageNode->MultiplyBlue = 100;

                    

                }
            }
        }
    }
}