using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class AtkSelectIconString : AtkOverlay
    {
        public readonly uint ListComponent = 3;
        public readonly uint MenuItemString = 2;
        public override void Update()
        {
            
        }

        public override WindowName WindowName { get; set; } = WindowName.SelectIconString;

        public void SetColor(uint listIndex, Vector4? newColour)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            var atkUnitBase = atkBaseWrapper.AtkUnitBase;
            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(ListComponent);
            if (listNode == null || (ushort)listNode->AtkResNode.Type < 1000) return;

            var renderer = GameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,listIndex == 0 ? 5U : 51000U + listIndex);
            if (renderer == null || !renderer->AtkResNode.IsVisible()) return;
            var itemText = (AtkTextNode*) renderer->Component->UldManager.SearchNodeById(MenuItemString);
            if (itemText != null)
            {
                if (newColour.HasValue)
                {
                    itemText->TextColor = Utils.ColorFromVector4(newColour.Value);
                }
            }
        }

        public void ResetColors()
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            var atkUnitBase = atkBaseWrapper.AtkUnitBase;
            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(ListComponent);
            if (listNode == null || (ushort)listNode->AtkResNode.Type < 1000) return;

            foreach(var originalColour in _originalColours)
            {
                var renderer = GameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,(uint)(originalColour.Key == 0 ? 5U : 51000U + originalColour.Key));
                if (renderer == null || !renderer->AtkResNode.IsVisible()) return;
                var itemText = (AtkTextNode*)renderer->Component->UldManager.SearchNodeById(MenuItemString);
                if (itemText != null)
                {
                    itemText->TextColor = originalColour.Value;
                }
            }
        }

        private Dictionary<int, ByteColor> _originalColours = new Dictionary<int, ByteColor>();
        public void SetColors(List<Vector4?> newColours)
        {
            var atkBaseWrapper = AtkUnitBase;
            if (atkBaseWrapper == null)
            {
                Service.Log.Verbose("null atk base");
            }
            else
            {
                if (atkBaseWrapper.AtkUnitBase == null)
                {
                    Service.Log.Verbose("null atk base inside wrtapper");
                }
            }
            if (atkBaseWrapper == null || atkBaseWrapper.AtkUnitBase == null) return;
            var atkUnitBase = atkBaseWrapper.AtkUnitBase;
            Service.Log.Verbose("searching for list node");

            var listNode = (AtkComponentNode*)atkUnitBase->GetNodeById(ListComponent);
            if (listNode == null || (ushort)listNode->AtkResNode.Type < 1000) return;
            Service.Log.Verbose("found the correct list component");
            for (var index = 0; index < newColours.Count; index++)
            {
                var item = newColours[index];
                var renderer = GameUiManager.GetNodeByID<AtkComponentNode>(listNode->Component->UldManager,(uint)(index == 0 ? 5U : 51000U + index));
                if (renderer == null || !renderer->AtkResNode.IsVisible()) return;
                var itemText = (AtkTextNode*)renderer->Component->UldManager.SearchNodeById(MenuItemString);
                Service.Log.Verbose("searching for item text");
                if (itemText != null)
                {
                    if (item.HasValue)
                    {
                        Service.Log.Verbose("found the correct item text to set");
                        if (!_originalColours.ContainsKey(index))
                        {
                            _originalColours[index] = itemText->TextColor;
                        }
                        itemText->TextColor = Utils.ColorFromVector4(item.Value);
                    }
                    else
                    {
                        Service.Log.Verbose("item does not have value");
                        if (_originalColours.ContainsKey(index))
                        {
                            itemText->TextColor = _originalColours[index];
                        }
                    }
                }
            }
        }
    }
}