using System;
using System.Runtime.InteropServices;
using CriticalCommonLib.Agents;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Addons
{
    using FFXIVClientStructs.FFXIV.Client.UI.Agent;

    [StructLayout(LayoutKind.Explicit, Size = 545)]

    public unsafe struct SubmarinePartsMenuAddon
    {
        [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

        public uint Phase
        {
            get
            {
                var phaseNode = AtkUnitBase.GetNodeById(16)->GetAsAtkTextNode();
                var textValue = Utils.ReadSeString(phaseNode->NodeText).TextValue;
                var values = textValue.Split('/', StringSplitOptions.TrimEntries);
                if (values.Length > 1)
                {
                    if (uint.TryParse(values[0], out uint result))
                    {
                        return result;
                    }
                }

                return 0;
            }
        }

        public uint CurrentConstructionQuality
        {
            get
            {
                var phaseNode = AtkUnitBase.GetNodeById(10)->GetAsAtkTextNode();
                var textValue = Utils.ReadSeString(phaseNode->NodeText).TextValue;
                var values = textValue.Split('/', StringSplitOptions.TrimEntries);
                if (values.Length > 1)
                {
                    if (uint.TryParse(values[0], out uint result))
                    {
                        return result;
                    }
                }

                return 0;
            }
        }

        public uint AmountHandedIn(int index)
        {
            var listComponentNode = (AtkComponentNode*) AtkUnitBase.GetNodeById(38);
            if (listComponentNode == null || (ushort) listComponentNode->AtkResNode.Type < 1000) return 0;
            var component = (AtkComponentList*) listComponentNode->Component;
            for (var i = 0; i < 6 && i < component->ListLength; i++)
            {
                if (i != index) continue;
                var listItem = component->ItemRendererList[i].AtkComponentListItemRenderer;
                
                var uldManager = listItem->AtkComponentButton.AtkComponentBase.UldManager;
                if (uldManager.NodeListCount < 14) continue;
                var resNode = uldManager.SearchNodeById(14);
                if (resNode == null) continue;
                var textNode = (AtkTextNode*) resNode;
                var textValue = Utils.ReadSeString(textNode->NodeText).TextValue;
                var values = textValue.Split('/', StringSplitOptions.TrimEntries);
                if (values.Length > 1)
                {
                    if (uint.TryParse(values[0], out uint result))
                    {
                        return result;
                    }
                }
            }
            return 0;
        }

        //Could be backtracked from company craft but I am lazy
        public uint AmountNeeded(int index)
        {
            var listComponentNode = (AtkComponentNode*) AtkUnitBase.GetNodeById(38);
            if (listComponentNode == null || (ushort) listComponentNode->AtkResNode.Type < 1000) return 0;
            var component = (AtkComponentList*) listComponentNode->Component;
            for (var i = 0; i < 6 && i < component->ListLength; i++)
            {
                if (i != index) continue;
                var listItem = component->ItemRendererList[i].AtkComponentListItemRenderer;
                
                var uldManager = listItem->AtkComponentButton.AtkComponentBase.UldManager;
                if (uldManager.NodeListCount < 14) continue;
                var resNode = uldManager.SearchNodeById(14);
                if (resNode == null) continue;
                var textNode = (AtkTextNode*) resNode;
                var textValue = Utils.ReadSeString(textNode->NodeText).TextValue;
                var values = textValue.Split('/', StringSplitOptions.TrimEntries);
                if (values.Length > 1)
                {
                    if (uint.TryParse(values[1], out uint result))
                    {
                        return result;
                    }
                }
            }
            return 0;
        }

        public uint RequiredItemId(int index)
        {
            if (index > 6)
            {
                return 0;
            }
            var agentInterface = Service.GameGui.FindAgentInterface("SubmarinePartsMenu");

            if (agentInterface == IntPtr.Zero) return 0;
            var agent = (AgentInterface*) agentInterface;
            if (agent->IsAgentActive())
            {
                var subAgent = (SubmarinePartsMenuAgent*) agent;
                return subAgent->RequiredItems[index];
            }
            return 0;
        }

        public uint ResultItemId
        {
            get
            {
                var agentInterface = Service.GameGui.FindAgentInterface("SubmarinePartsMenu");
                if (agentInterface == IntPtr.Zero) return 0;
                var agent = (AgentInterface*) agentInterface;
                if (agent->IsAgentActive())
                {
                    var subAgent = (SubmarinePartsMenuAgent*) agent;
                    return subAgent->ResultItem;
                }
                return 0;
            }
        }
    }
}