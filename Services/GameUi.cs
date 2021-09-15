using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using CriticalCommonLib;
using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services
{
    public unsafe class GameUi : IDisposable
    {
        private delegate AtkStage* GetAtkStageSingleton();
        private GetAtkStageSingleton getAtkStageSingleton;
        
        private const int UnitListCount = 18;
        private AtkUnitBase* selectedUnitBase = null;
        
        private readonly string[] listNames = new string[UnitListCount]{
            "Depth Layer 1",
            "Depth Layer 2",
            "Depth Layer 3",
            "Depth Layer 4",
            "Depth Layer 5",
            "Depth Layer 6",
            "Depth Layer 7",
            "Depth Layer 8",
            "Depth Layer 9",
            "Depth Layer 10",
            "Depth Layer 11",
            "Depth Layer 12",
            "Depth Layer 13",
            "Loaded Units",
            "Focused Units",
            "Units 16",
            "Units 17",
            "Units 18"
        };
        private SigScanner _targetModuleScanner;
        private Framework _framework;
        private Dictionary<WindowName, bool> _windowVisibility;
        private List<WindowName> _windowVisibilityWatchList;
        private Dictionary<WindowName,GameWindow> _windowCache;
        public delegate void UiVisibilityChangedDelegate(WindowName windowName);
        public event UiVisibilityChangedDelegate UiVisibilityChanged;

        public enum WindowName
        {
            RetainerList,
            RetainerGrid0,
            RetainerGrid1,
            RetainerGrid2,
            RetainerGrid3,
            RetainerGrid4,
            InventoryGrid0E,
            InventoryGrid1E,
            InventoryGrid2E,
            InventoryGrid3E,
            InventoryBuddy, //Chocobo Saddlebag
        }
        
        public GameUi(SigScanner targetModuleScanner, Framework framework)
        {
            _targetModuleScanner = targetModuleScanner;
            _framework = framework;
            if (getAtkStageSingleton == null) {
                var getSingletonAddr = _targetModuleScanner.ScanText("E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF");
                this.getAtkStageSingleton = Marshal.GetDelegateForFunctionPointer<GetAtkStageSingleton>(getSingletonAddr);
            }

            _windowVisibility = new Dictionary<WindowName, bool>();
            _windowVisibilityWatchList = new List<WindowName>();
            _framework.Update += FrameworkOnOnUpdateEvent;
            _windowCache = new Dictionary<WindowName, GameWindow>();
        }
        

        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            for (var index = 0; index < _windowVisibilityWatchList.Count; index++)
            {
                var item = _windowVisibilityWatchList[index];
                var isWindowVisible = IsWindowVisible(item);
                if (!_windowVisibility.ContainsKey(item) || _windowVisibility[item] != isWindowVisible)
                {
                    _windowCache.Remove(item);
                    _windowVisibility[item] = isWindowVisible;
                    UiVisibilityChanged?.Invoke(item);
                }
            }
        }

        public List<string> GetFocusedWindows() {
            var list = new List<string>();
            var stage = getAtkStageSingleton();
            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            for (var i = 0; i < unitManagers->Count; i++) {
                var unitManager = &unitManagers[i];
                var unitBaseArray = &(unitManager->AtkUnitEntries);

                for (var j = 0; j < unitManager->Count; j++) {
                    var unitBase = unitBaseArray[j];
                    if (unitBase->RootNode == null) continue;
                    var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));
                    list.Add(name);
                }
            }
            return list;
        }
        public List<string> GetLoadedWindows() {
            var list = new List<string>();

                var stage = getAtkStageSingleton();
                var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
                for (var i = 0; i < UnitListCount; i++) {
                    var unitManager = &unitManagers[i];
                    var unitBaseArray = &(unitManager->AtkUnitEntries);
                    for (var j = 0; j < unitManager->Count; j++) {
                        var unitBase = unitBaseArray[j];
                        if (unitBase->RootNode == null) continue;
                        if (!(unitBase->IsVisible && unitBase->RootNode->IsVisible)) continue;
                        var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));
                        if (name != null)
                        {
                            list.Add(name);
                        }
                    }
                }
            return list;
        }

        public bool RelatedWindowsVisible(List<string> relatedWindows)
        {
            return relatedWindows.Select(x => x)
                .Intersect(GetLoadedWindows())
                .Any(); 
        }

        public void WatchWindowState(WindowName windowName)
        {
            if (!_windowVisibilityWatchList.Contains(windowName))
            {
                _windowVisibilityWatchList.Add(windowName);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public AtkUnitBase* GetWindow(String windowName) {
            var stage = getAtkStageSingleton();
            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            var isVisible = false;
            for (var i = 0; i < unitManagers->Count; i++) {
                var unitManager = &unitManagers[i];
                var unitBaseArray = &(unitManager->AtkUnitEntries);
                for (var j = 0; j < unitManager->Count; j++) {
                    var unitBase = unitBaseArray[j];
                    if (unitBase == null || unitBase->RootNode == null) continue;
                    var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));
                    if (name == windowName)
                    {
                        return unitBase;
                    }
                }
            }
            return null;
        }
        
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public bool IsWindowVisible(WindowName windowName) {
            var stage = getAtkStageSingleton();
            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
            var isVisible = false;

            for (var i = 0; i < unitManagers->Count; i++)
            {
                var unitManager = &unitManagers[i];
                var unitBaseArray = &(unitManager->AtkUnitEntries);
                for (var j = 0; j < unitManager->Count; j++)
                {
                    var unitBase = unitBaseArray[j];
                    if (unitBase == null || unitBase->RootNode == null) continue;
                    var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));
                    if (unitBase->IsVisible && name == windowName.ToString())
                    {
                        isVisible = true;
                        break;
                    }
                }

                break;
            }

            return isVisible;
        }
        
        public AtkUnitBase* GetFocusedWindow() {
            var stage = getAtkStageSingleton();
            var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            for (var i = 0; i < unitManagers->Count; i++) {
                var unitManager = &unitManagers[i];
                var unitBaseArray = &(unitManager->AtkUnitEntries);

                for (var j = 0; j < unitManager->Count; j++) {
                    var unitBase = unitBaseArray[j];
                    return unitBase;
                }
            }
            return null;
        }

        public InventoryGrid GetRetainerGrid(int index)
        {
            WindowName windowName;
            switch (index)
            {
                case 0:
                    windowName = WindowName.RetainerGrid0;
                    break;
                case 1:
                    windowName = WindowName.RetainerGrid1;
                    break;
                case 2:
                    windowName = WindowName.RetainerGrid2;
                    break;
                case 3:
                    windowName = WindowName.RetainerGrid3;
                    break;
                case 4:
                    windowName = WindowName.RetainerGrid4;
                    break;
                default:
                    return null;
            }

            List<InventoryGridItem> list = new List<InventoryGridItem>();
            if (IsWindowVisible(windowName))
            {
                var retainerGrid = GetWindow("RetainerGrid" + index);
                for (var j = 0; j < retainerGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = retainerGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.DragDrop)
                        {
                            list.Add(new InventoryGridItem(subNode));
                        }
                    }
                }
                var sortedList = list.OrderBy(c => c.resNode->Y).ThenBy(c => c.resNode->X).ToList();
                return new InventoryGrid(sortedList);
            }

            return null;
        }
        
        
        public InventoryGrid GetPrimaryInventoryGrid(int index)
        {
            WindowName windowName;
            switch (index)
            {
                case 0:
                    windowName = WindowName.InventoryGrid0E;
                    break;
                case 1:
                    windowName = WindowName.InventoryGrid1E;
                    break;
                case 2:
                    windowName = WindowName.InventoryGrid2E;
                    break;
                case 3:
                    windowName = WindowName.InventoryGrid3E;
                    break;
                default:
                    return null;
            }

            List<InventoryGridItem> list = new List<InventoryGridItem>();
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow("InventoryGrid" + index + "E");
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.DragDrop)
                        {
                            list.Add(new InventoryGridItem(subNode));
                        }
                    }
                }
                var sortedList = list.OrderBy(c => c.resNode->Y).ThenBy(c => c.resNode->X).ToList();
                return new InventoryGrid(sortedList);
            }

            return null;
        }
        public RetainerList GetRetainerList()
        {
            List<RetainerListItem> list = new List<RetainerListItem>();
            if (IsWindowVisible(WindowName.RetainerList))
            {
                if (_windowCache.ContainsKey(WindowName.RetainerList))
                {
                    return (RetainerList) _windowCache[WindowName.RetainerList];
                }
                PluginLog.Verbose("GameUi: Retainer list visible");
                var primaryGrid = GetWindow("RetainerList");
                var absoluteX = primaryGrid->X;
                var absoluteY = primaryGrid->Y;
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.List)
                        {
                            PluginLog.Verbose("GameUi: Retainer interior list found");
                            var listNode = (AtkComponentNode*) subNode;
                            var listUldManager = listNode->Component->UldManager;
                            var listAbsoluteX = absoluteX + subNode->X;
                            var listAbsoluteY = absoluteY + subNode->Y;
                            for (var j2 = 0; j2 < listUldManager.NodeListCount; j2++)
                            {
                                var subNode2 = listUldManager.NodeList[j2];
                                PluginLog.Verbose("GameUi: " + subNode2->Type);
                                if ((int) subNode2->Type >= 1000)
                                {
                                    var component2 = (AtkComponentNode*) subNode;
                                    var componentInfo2 = component2->Component->UldManager;
                                    var objectInfo2 = (AtkUldComponentInfo*) componentInfo2.Objects;
                                    PluginLog.Verbose("GameUi: " + objectInfo2->ComponentType);
                                    if (objectInfo2->ComponentType == ComponentType.List)
                                    {
                                        PluginLog.Verbose("GameUi: Retainer interior list renderer found");
                                        var listItemComponent = (AtkComponentNode*) subNode2;
                                        var listItemManager = listItemComponent->Component->UldManager;
                                        for (var j3 = 0; j3 < listItemManager.NodeListCount; j3++)
                                        {
                                            var gridNode = listItemManager.NodeList[j3];
                                            if (gridNode->Type == NodeType.Text && gridNode->NodeID == 3)
                                            {
                                                PluginLog.Verbose("GameUi: Retainer text node found");
                                                var retainerNameNode = (AtkTextNode*) gridNode;
                                                var retainerName =
                                                    Marshal.PtrToStringAnsi(
                                                        new IntPtr(retainerNameNode->NodeText.StringPtr));
                                                //Seem to be selection nodes or something that have no text
                                                if (retainerName != "")
                                                {
                                                    var itemX = listAbsoluteX + listItemComponent->AtkResNode.X;
                                                    var itemY = listAbsoluteY + listItemComponent->AtkResNode.Y;
                                                    list.Add(new RetainerListItem(retainerNameNode, itemX, itemY, retainerName));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                var sortedList = list.OrderBy(c => c.RelativeY).ToList();
                var retainerList = new RetainerList(sortedList);
                _windowCache[WindowName.RetainerList] = retainerList;
                return retainerList;
            }

            return null;
        }

        public abstract class GameWindow
        {
            public string Name;
        }

        public class RetainerList : GameWindow
        {
            public List<RetainerListItem> _sortedItems;
            
            public RetainerList(List<RetainerListItem> sortedItems)
            {
                _sortedItems = sortedItems;
            }
            
            public void ClearColors()
            {
                foreach (var item in _sortedItems)
                {
                    item.textNode->TextColor = item.originalColor;
                    item.textNode->SetText(item.originalText);
                }
            }

            public void SetColor(string retainerName, string hexCode)
            {
                if (_sortedItems.Exists(c => c.RetainerName == retainerName))
                {
                    var textNode = _sortedItems.Find(c => c.RetainerName == retainerName);
                    if (textNode != null)
                    {
                        textNode.textNode->TextColor = Utils.ColorFromHex(hexCode, 255);
                    }
                }
            }

            public void SetText(string retainerName, string newText)
            {
                if (_sortedItems.Exists(c => c.RetainerName == retainerName))
                {
                    var textNode = _sortedItems.Find(c => c.RetainerName == retainerName);
                    if (textNode != null)
                    {
                        textNode.textNode->SetText(newText);
                    }
                }
            }

            public void SetTextAndColor(string retainerName, string newText, string hexCode)
            {
                if (_sortedItems.Exists(c => c.RetainerName == retainerName))
                {
                    var textNode = _sortedItems.Find(c => c.RetainerName == retainerName);
                    if (textNode != null)
                    {
                        textNode.textNode->SetText(newText);
                        textNode.textNode->TextColor = Utils.ColorFromHex(hexCode, 255);
                    }
                }
            }
        }
        
        public class RetainerListItem
        {
            public AtkTextNode* textNode;
            public ByteColor originalColor;
            public string originalText;
            public float RelativeX;
            public float RelativeY;
            public string RetainerName;

            public RetainerListItem(AtkTextNode* textNode, float relativeX, float relativeY, string retainerName)
            {
                RelativeX = relativeX;
                RelativeY = relativeY;
                RetainerName = retainerName;
                this.textNode = textNode;
                this.originalColor = textNode->TextColor;
                this.originalText = retainerName;
            }
        }

        public class InventoryGrid : GameWindow
        {
            private List<InventoryGridItem> _sortedItems;

            public InventoryGrid(List<InventoryGridItem> sortedItems)
            {
                _sortedItems = sortedItems;
            }

            public void ClearColors()
            {
                foreach (var item in _sortedItems)
                {
                    item.resNode->AddBlue = 0;
                    item.resNode->AddRed = 0;
                    item.resNode->AddGreen = 0;
                }
            }

            public void SetColor(int itemIndex, int red, int green, int blue)
            {
                if (itemIndex >= 0 && _sortedItems.Count > itemIndex)
                {
                    _sortedItems[itemIndex].resNode->AddBlue = (ushort) blue;
                    _sortedItems[itemIndex].resNode->AddRed = (ushort) red;
                    _sortedItems[itemIndex].resNode->AddGreen = (ushort) green;
                }
            }

            public List<InventoryGridItem> SortedItems => _sortedItems;
        }

        public class InventoryGridItem
        {
            public AtkResNode* resNode;

            public InventoryGridItem(AtkResNode* resNode)
            {
                this.resNode = resNode;
            }
        }


        public void Dispose()
        {
            _framework.Update -= FrameworkOnOnUpdateEvent;
        }
    }
}