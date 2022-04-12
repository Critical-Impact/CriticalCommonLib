using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public abstract unsafe class UiAddon
    {
        public AtkUnitBase* _unitBase;
        public unsafe AtkResNode* GetNodeById(uint id)
        {
            for (var j = 0; j < _unitBase->UldManager.NodeListCount; j++)
            {
                var subNode = _unitBase->UldManager.NodeList[j];
                if (subNode->NodeID == id)
                {
                    return subNode;
                }
            }

            return null;
        }
        public unsafe AtkResNode*[] GetNodesByComponentType(ComponentType type)
        {
            var arrayLength = 0;
            for (var j = 0; j < _unitBase->UldManager.NodeListCount; j++)
            {
                var subNode = _unitBase->UldManager.NodeList[j];
                if ((int) subNode->Type >= 1000)
                {
                    var component2 = (AtkComponentNode*) subNode;
                    var componentInfo2 = component2->Component->UldManager;
                    var objectInfo2 = (AtkUldComponentInfo*) componentInfo2.Objects;
                    if (objectInfo2->ComponentType == type)
                    {
                        arrayLength++;
                    }
                }
            }
            var resNodes = new AtkResNode*[arrayLength];
            arrayLength = 0;
            for (var j = 0; j < _unitBase->UldManager.NodeListCount; j++)
            {
                var subNode = _unitBase->UldManager.NodeList[j];
                if ((int) subNode->Type >= 1000)
                {
                    var component2 = (AtkComponentNode*) subNode;
                    var componentInfo2 = component2->Component->UldManager;
                    var objectInfo2 = (AtkUldComponentInfo*) componentInfo2.Objects;
                    if (objectInfo2->ComponentType == type)
                    {
                        resNodes[arrayLength] = subNode;
                        arrayLength++;
                    }
                }
            }

            return resNodes;
        }
    }
}