using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public abstract unsafe class UiAtkAddon
    {
        public AtkResNode* _resNode;
        public unsafe AtkResNode* GetNodeById(uint id)
        {
            var component = (AtkComponentNode*) _resNode;
            var componentInfo = component->Component->UldManager;
            var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
            for (var j = 0; j < componentInfo.NodeListCount; j++)
            {
                if (componentInfo.NodeList[j]->NodeID == id)
                {
                    return componentInfo.NodeList[j];
                }
            }

            PluginLog.Verbose("Could not find node with ID " + id);
            return null;
        }
    }
}