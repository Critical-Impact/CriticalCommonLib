using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Parsing;

namespace CriticalCommonLib.Services.Ui
{
    public abstract unsafe class AtkRetainerList : AtkOverlay
    {
        public readonly uint ListComponent = 24;
        public readonly uint RetainerNameText = 3;
        public override WindowName WindowName { get; set; } = WindowName.RetainerList;

        public void SetName(AtkUnitBase* atkUnitBase, ulong retainerId, string newName, Vector4? newColour)
        {
            if (atkUnitBase == null) return;
            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(ListComponent);
            if (listNode == null || (ushort)listNode->AtkResNode.Type < 1000) return;
            var retainerManager = RetainerManager.Instance();
            for (uint i = 0; i < 10; i++)
            {
                var retainer = retainerManager->GetRetainerBySortedIndex(i);
                if (retainer->RetainerID == retainerId)
                {
                    var renderer = GameUi.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager, i == 0 ? 4U : 41000U + i, (NodeType) 1011);
                    if (renderer == null || !renderer->AtkResNode.IsVisible) continue;
                    var retainerText = (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                    if (newColour.HasValue)
                    {
                        retainerText->TextColor = Utils.ColorFromVector4(newColour.Value);
                    }
                    retainerText->SetText(newName);
                    break;
                }
            }
        }
        
        public void SetNames(AtkUnitBase* atkUnitBase, Dictionary<ulong, string> newNames, Vector4? newColour)
        {
            if (atkUnitBase == null) return;
            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(24);
            if (listNode == null || (ushort)listNode->AtkResNode.Type < 1000) return;
            var retainerManager = RetainerManager.Instance();
            for (uint i = 0; i < 10; i++)
            {
                var retainer = retainerManager->GetRetainerBySortedIndex(i);
                if (newNames.ContainsKey(retainer->RetainerID))
                {
                    var renderer = GameUi.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager, i == 0 ? 4U : 41000U + i, (NodeType) 1011);
                    if (renderer == null || !renderer->AtkResNode.IsVisible) continue;
                    var retainerText = (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                    retainerText->SetText(newNames[retainer->RetainerID]);
                }
                else
                {
                    var renderer = GameUi.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager, i == 0 ? 4U : 41000U + i, (NodeType) 1011);
                    if (renderer == null || !renderer->AtkResNode.IsVisible) continue;
                    var retainerText = (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(RetainerNameText);
                    retainerText->SetText(retainer->Name);
                }
            }
        }
    }
}