using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class AtkRetainerList : AtkOverlay
    {
        private readonly IPluginLog _pluginLog;
        private readonly IGameUiManager _gameUiManager;
        public readonly uint ListComponent = 27;
        public readonly uint RetainerNameText = 3;

        public AtkRetainerList(IGameGui gameGui, IPluginLog pluginLog, IGameUiManager gameUiManager) : base(gameGui)
        {
            _pluginLog = pluginLog;
            _gameUiManager = gameUiManager;
        }

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
                        var renderer = _gameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,
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
                _pluginLog.Verbose("Couldn't find list node within retainer list.");
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
                            var renderer = _gameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,
                                i == 0 ? 4U : 41000U + i);
                            if (renderer == null || !renderer->AtkResNode.IsVisible()) continue;
                            var retainerText =
                                (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                            if (retainerText != null)
                            {
                                try
                                {
                                    retainerText->SetText(newNames[retainer->RetainerId]);
                                }
                                catch (Exception e)
                                {
                                    _pluginLog.Error("Failed to set new retainer name.", e);
                                }
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
                                _pluginLog.Verbose("Couldn't find retainer text node.");
                            }
                        }
                        else
                        {
                            var renderer = _gameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,
                                i == 0 ? 4U : 41000U + i, (NodeType) 1011);
                            if (renderer == null || !renderer->AtkResNode.IsVisible()) continue;
                            var retainerText =
                                (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                            if (retainerText != null)
                            {
                                try
                                {
                                    retainerText->SetText(retainer->NameString);
                                }
                                catch (Exception e)
                                {
                                    _pluginLog.Error("Failed to set new retainer name.", e);
                                }
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
                                _pluginLog.Verbose("Couldn't find retainer text node.");
                            }
                        }
                    }
                    else
                    {
                        _pluginLog.Verbose("Couldn't retrieve retainer by sorted index.");
                    }
                }
            }
            else
            {
                _pluginLog.Verbose("Couldn't retrieve retainer manager.");
            }
        }
    }
}