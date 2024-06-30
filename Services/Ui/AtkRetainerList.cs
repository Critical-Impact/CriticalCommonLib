using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class AtkRetainerList : AtkOverlay
    {
        public readonly uint ListComponent = 27;
        public readonly uint RetainerNameText = 3;
        public override void Update()
        {
            
        }

        public override WindowName WindowName { get; set; } = WindowName.RetainerList;
        public void SetName(ulong retainerId, string newName, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            var atkUnitBase = atkBaseWrapper.AtkUnitBase;
            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(ListComponent);
            if (listNode == null || (ushort)listNode->AtkResNode.Type < 1000) return;
            var retainerManager = RetainerManager.Instance();
            if (retainerManager != null)
            {
                for (uint i = 0; i < retainerManager->GetRetainerCount(); i++)
                {
                    var retainer = retainerManager->GetRetainerBySortedIndex(i);
                    if (retainer != null && retainer->RetainerId == retainerId)
                    {
                        var renderer = GameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,
                            i == 0 ? 4U : 41000U + i);
                        if (renderer == null || !renderer->AtkResNode.IsVisible()) continue;
                        var retainerText =
                            (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                        if (retainerText != null)
                        {
                            if (newColour.HasValue)
                            {
                                retainerText->TextColor = Utils.ColorFromVector4(newColour.Value);
                            }

                            retainerText->SetText(newName);
                        }

                        break;
                    }
                }
            }
        }
        
        public void SetNames(Dictionary<ulong, string> newNames,Dictionary<ulong, Vector4> newColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null) return;
            var atkUnitBase = atkBaseWrapper.AtkUnitBase;
            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(ListComponent);
            if (listNode == null || (ushort) listNode->AtkResNode.Type < 1000)
            {
                Service.Log.Verbose("Couldn't find list node within retainer list.");
                return;
            };
            var retainerManager = RetainerManager.Instance();
            if (retainerManager != null)
            {
                var retainerCount = 10;
                for (uint i = 0; i < retainerCount; i++)
                {
                    var retainer = retainerManager->GetRetainerBySortedIndex(i);
                    if (retainer != null)
                    {
                        if (newNames.ContainsKey(retainer->RetainerId))
                        {
                            var renderer = GameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,
                                i == 0 ? 4U : 41000U + i);
                            if (renderer == null || !renderer->AtkResNode.IsVisible()) continue;
                            var retainerText =
                                (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                            if (retainerText != null)
                            {
                                retainerText->SetText(newNames[retainer->RetainerId]);
                                if (newColours.ContainsKey(retainer->RetainerId))
                                {
                                    retainerText->TextColor = Utils.ColorFromVector4(newColours[retainer->RetainerId]);
                                }
                                else
                                {
                                    retainerText->TextColor =  Utils.ColorFromVector4(ImGuiColors.DalamudWhite);
                                }
                            }
                            else
                            {
                                Service.Log.Verbose("Couldn't find retainer text node.");
                            }
                        }
                        else
                        {
                            var renderer = GameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,
                                i == 0 ? 4U : 41000U + i, (NodeType) 1011);
                            if (renderer == null || !renderer->AtkResNode.IsVisible()) continue;
                            var retainerText =
                                (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                            if (retainerText != null)
                            {
                                retainerText->SetText(retainer->Name);
                                if (newColours.ContainsKey(retainer->RetainerId))
                                {
                                    retainerText->TextColor = Utils.ColorFromVector4(newColours[retainer->RetainerId]);
                                }
                                else
                                {
                                    retainerText->TextColor =  Utils.ColorFromVector4(ImGuiColors.DalamudWhite);
                                }
                            }
                            else
                            {
                                Service.Log.Verbose("Couldn't find retainer text node.");
                            }
                        }
                    }
                    else
                    {
                        Service.Log.Verbose("Couldn't retrieve retainer by sorted index.");
                    }
                }
            }
            else
            {
                Service.Log.Verbose("Couldn't retrieve retainer manager.");
            }
        }
    }
}