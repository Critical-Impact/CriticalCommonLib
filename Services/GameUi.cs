using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using CriticalCommonLib;
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Text;
using Vector3 = System.Numerics.Vector3;

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
        private Dictionary<WindowName, int> _tabCache;
        public delegate void UiVisibilityChangedDelegate(WindowName windowName);
        public event UiVisibilityChangedDelegate UiVisibilityChanged;

        public enum WindowName
        {
            RetainerList,
            InventoryRetainer,//For normal retainer inventory
            RetainerGrid,//For normal retainer inventory
            
            RetainerGrid0,//For expanded retainer inventory
            RetainerGrid1,//For expanded retainer inventory
            RetainerGrid2,//For expanded retainer inventory
            RetainerGrid3,//For expanded retainer inventory
            RetainerGrid4,//For expanded retainer inventory
            InventoryRetainerLarge,//For expanded retainer inventory
            
            InventoryGrid0E,//For open all inventory
            InventoryGrid1E,//For open all inventory
            InventoryGrid2E,//For open all inventory
            InventoryGrid3E,//For open all inventory
            
            InventoryBuddy, //Chocobo Saddlebag,
            
            Inventory, //For normal inventory
            InventoryGrid, //For normal inventory
            InventoryLarge, //For expanded inventory
            InventoryGrid0, //For expanded inventory
            InventoryGrid1, //For expanded inventory
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
            _tabCache = new Dictionary<WindowName, int>();
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
                    if (!isWindowVisible && _tabCache.ContainsKey(item))
                    {
                        _tabCache.Remove(item);
                    }
                    UiVisibilityChanged?.Invoke(item);
                }
                //Special handling of tabs in normal inventory, maybe rework this to use the cache and add events to that object so you can bind directly to when the tab changes
                if (item == WindowName.Inventory)
                {
                    if (isWindowVisible)
                    {
                        var currentTab = GetNormalInventoryGridIndex();
                        if (_tabCache.ContainsKey(WindowName.Inventory))
                        {
                            if (currentTab != _tabCache[WindowName.Inventory])
                            {
                                PluginLog.Verbose("GameUi: Inventory tab changed");
                                _tabCache[WindowName.Inventory] = currentTab;
                                UiVisibilityChanged?.Invoke(item);
                            }
                        }
                        else
                        {
                            _tabCache[WindowName.Inventory] = currentTab;
                            UiVisibilityChanged?.Invoke(item);
                        }
                    }
                }
                else if (item == WindowName.InventoryLarge)
                {
                    if (isWindowVisible)
                    {
                        var currentTab = GetLargeInventoryGridIndex();
                        if (_tabCache.ContainsKey(WindowName.InventoryLarge))
                        {
                            if (currentTab != _tabCache[WindowName.InventoryLarge])
                            {
                                PluginLog.Verbose("GameUi: Large inventory tab changed");
                                _tabCache[WindowName.InventoryLarge] = currentTab;
                                UiVisibilityChanged?.Invoke(item);
                            }
                        }
                        else
                        {
                            _tabCache[WindowName.Inventory] = currentTab;
                            UiVisibilityChanged?.Invoke(item);
                        }
                    }
                }
                else if (item == WindowName.InventoryRetainerLarge)
                {
                    if (isWindowVisible)
                    {
                        var currentTab = GetLargeRetainerInventoryGridIndex();
                        if (_tabCache.ContainsKey(WindowName.InventoryRetainerLarge))
                        {
                            if (currentTab != _tabCache[WindowName.InventoryRetainerLarge])
                            {
                                PluginLog.Verbose("GameUi: Large retainer inventory tab changed");
                                _tabCache[WindowName.InventoryRetainerLarge] = currentTab;
                                UiVisibilityChanged?.Invoke(item);
                            }
                        }
                        else
                        {
                            _tabCache[WindowName.Inventory] = currentTab;
                            UiVisibilityChanged?.Invoke(item);
                        }
                    }
                }
                else if (item == WindowName.InventoryRetainer)
                {
                    if (isWindowVisible)
                    {
                        var currentTab = GetNormalRetainerInventoryGridIndex();
                        if (_tabCache.ContainsKey(WindowName.InventoryRetainer))
                        {
                            if (currentTab != _tabCache[WindowName.InventoryRetainer])
                            {
                                PluginLog.Verbose("GameUi: Inventory tab changed to " + currentTab);
                                _tabCache[WindowName.InventoryRetainer] = currentTab;
                                UiVisibilityChanged?.Invoke(item);
                            }
                        }
                        else
                        {
                            _tabCache[WindowName.InventoryRetainer] = currentTab;
                            UiVisibilityChanged?.Invoke(item);
                        }
                    }
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
                //TODO: Handle retainer tabs?
                return new InventoryGrid(sortedList, new List<InventoryTabItem>());
            }

            return null;
        }
        
        public InventoryGrid GetChocoboSaddlebag(int index)
        {
            WindowName windowName = WindowName.InventoryBuddy;

            List<InventoryGridItem> list = new List<InventoryGridItem>();
            if (IsWindowVisible(windowName))
            {
                var buddyGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < buddyGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = buddyGrid->UldManager.NodeList[j];
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
                var sortedList = list.OrderBy(c => c.resNode->Y + c.resNode->ParentNode->Y).ThenBy(c => c.resNode->X + c.resNode->ParentNode->X).ToList();
                var finalList = new List<InventoryGridItem>();
                for (var i = 0; i < sortedList.Count; i++)
                {
                    var item = sortedList[i];
                    if ((i / 5) % 2 == 0 && index == 0)
                    {
                        finalList.Add(item);
                    }
                    else if((i / 5) % 2 == 1 && index == 1)
                    {
                        finalList.Add(item);
                    }
                }


                return new InventoryGrid(finalList, new List<InventoryTabItem>());
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
                return new InventoryGrid(sortedList, new List<InventoryTabItem>());
            }

            return null;
        }
        
        private int GetNormalInventoryGridIndex()
        {
            WindowName windowName = WindowName.Inventory;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var name = "";
                            var currentTab = false;
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                        currentTab = true;
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    name = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue;
                                }
                            }

                            if (currentTab)
                            {
                                int tabIndex = 0;
                                if (int.TryParse(name.Substring(0, 1), out tabIndex))
                                {
                                    return tabIndex;
                                }
                                PluginLog.Verbose("GameUi: Could not parse tab index: " + name);
                            }
                        }
                    }
                }
                
            }

            return -1;
        }
        
        private int GetNormalRetainerInventoryGridIndex()
        {
            WindowName windowName = WindowName.InventoryRetainer;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    //This is a hack but until someone comes up with a good way of determining the radio button status we just need to go by the color
                                    if (textNode->EdgeColor.R == 240 && textNode->EdgeColor.B == 55 &&
                                        textNode->EdgeColor.G == 142)
                                    {
                                        return (int)(subNode->X / 25.0f);
                                    }
                                }
                            }
                        }
                    }
                }
                
            }

            return -1;
        }
        
        public InventoryGrid GetNormalInventoryGrid(int index)
        {
            WindowName windowName = WindowName.Inventory;
            var activeTab = 0;
            index++;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                var returnTab = false;
                var tabItems = new List<InventoryTabItem>();
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var currentTab = false;
                            var name = "";
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                        currentTab = true;
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    name = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue.Substring(0, 1);
                                    int tabIndex;
                                    if (int.TryParse(name, out tabIndex))
                                    {
                                        tabItems.Add(new InventoryTabItem(subNode, tabIndex));
                                    }
                                    else
                                    {
                                        PluginLog.Verbose("GameUi: Could not parse tab index: " + name);
                                    }
                                }
                            }

                            if (currentTab && name == index.ToString())
                            {
                                PluginLog.Verbose("GameUi: Found normal inventory that is active: " + name);
                                returnTab = true;
                            }
                        }
                    }
                }
                if (returnTab)
                {
                    return GetNormalInventorySubgrid(tabItems);
                }
                
            }

            return null;
        }
        private InventoryGrid GetNormalInventorySubgrid(List<InventoryTabItem> tabItems)
        {
            WindowName windowName = WindowName.InventoryGrid;

            List<InventoryGridItem> list = new List<InventoryGridItem>();
            if (IsWindowVisible(windowName))
            {
                var inventoryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < inventoryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = inventoryGrid->UldManager.NodeList[j];
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
                return new InventoryGrid(sortedList, tabItems);
            }

            return null;
        }
        
        
        
        public InventoryGrid GetNormalRetainerInventoryGrid(int index)
        {
            WindowName windowName = WindowName.InventoryRetainer;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                var returnTab = false;
                var tabItems = new List<InventoryTabItem>();
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var currentTab = false;
                            int tabIndex = -1;
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    //This is a hack but until someone comes up with a good way of determining the radio button status we just need to go by the color
                                    var tabIndex2 = (int) (subNode->X / 25.0f);
                                    PluginLog.Verbose("GameUi: normal retainer inventory tab index: " + tabIndex2);
                                    if (textNode->EdgeColor.R == 240 && textNode->EdgeColor.B == 55 &&
                                        textNode->EdgeColor.G == 142)
                                    {
                                        PluginLog.Verbose("GameUi: normal retainer tab edge color matched: " + tabIndex2);
                                        currentTab = true;
                                        tabIndex = tabIndex2;
                                    }
                                    tabItems.Add(new InventoryTabItem(subNode, tabIndex2));
                                }
                            }

                            if (currentTab && tabIndex == index)
                            {
                                PluginLog.Verbose("GameUi: Found normal retainer inventory that is active: " + tabIndex);
                                returnTab = true;
                            }
                        }
                    }
                }
                if (returnTab)
                {
                    return GetNormalRetainerInventorySubgrid(tabItems);
                }
                
            }

            return null;
        }
        
        private InventoryGrid GetNormalRetainerInventorySubgrid(List<InventoryTabItem> tabItems)
        {
            WindowName windowName = WindowName.RetainerGrid;

            List<InventoryGridItem> list = new List<InventoryGridItem>();
            if (IsWindowVisible(windowName))
            {
                var inventoryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < inventoryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = inventoryGrid->UldManager.NodeList[j];
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
                return new InventoryGrid(sortedList, tabItems);
            }

            return null;
        }
        
        
        private int GetLargeInventoryGridIndex()
        {
            WindowName windowName = WindowName.InventoryLarge;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var name = "";
                            var currentTab = false;
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                        currentTab = true;
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    name = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue;
                                }
                            }

                            if (currentTab)
                            {
                                int tabIndex = 0;
                                if (int.TryParse(name.Substring(0, 1), out tabIndex))
                                {
                                    return tabIndex;
                                }
                            }
                        }
                    }
                }
                
            }

            return -1;
        }
        
        
        private int GetLargeRetainerInventoryGridIndex()
        {
            WindowName windowName = WindowName.InventoryRetainerLarge;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var name = "";
                            var currentTab = false;
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                        currentTab = true;
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    name = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue;
                                }
                            }

                            if (currentTab)
                            {
                                int tabIndex = 0;
                                if (int.TryParse(name.Substring(0, 1), out tabIndex))
                                {
                                    return tabIndex;
                                }
                            }
                        }
                    }
                }
                
            }

            return -1;
        }
        
        
        public InventoryGrid GetLargeInventoryGrid(int index)
        {
            WindowName windowName = WindowName.InventoryLarge;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                var returnTab = false;
                var tabItems = new List<InventoryTabItem>();
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var currentTab = false;
                            var name = "";
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                        currentTab = true;
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    name = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue.Substring(0, 1);
                                    int tabIndex;
                                    if (int.TryParse(name, out tabIndex))
                                    {
                                        tabItems.Add(new InventoryTabItem(subNode, tabIndex));
                                    }
                                }
                            }

                            if (currentTab)
                            {
                                if (name == "1" && index is 0 or 1)
                                {
                                    returnTab = true;
                                }
                                else if (name == "2" && index is 2 or 3)
                                {
                                    returnTab = true;
                                }
                            }
                        }
                    }
                }
                if (returnTab)
                {
                    return GetLargeInventorySubgrid(index, tabItems);
                }
                
            }

            return null;
        }
        
        public InventoryGrid GetLargeRetainerInventoryGrid()
        {
            WindowName windowName = WindowName.InventoryRetainerLarge;
            var activeTab = 0;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
                var returnTab = false;
                var tabItems = new List<InventoryTabItem>();
                for (var j = 0; j < primaryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = primaryGrid->UldManager.NodeList[j];
                    if ((int) subNode->Type >= 1000)
                    {
                        var component = (AtkComponentNode*) subNode;
                        var componentInfo = component->Component->UldManager;
                        var objectInfo = (AtkUldComponentInfo*) componentInfo.Objects;
                        if (objectInfo->ComponentType == ComponentType.RadioButton)
                        {
                            var currentTab = false;
                            var name = "";
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                        currentTab = true;
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    name = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue.Substring(0, 1);
                                    int tabIndex;
                                    if (int.TryParse(name, out tabIndex))
                                    {
                                        tabItems.Add(new InventoryTabItem(subNode, tabIndex));
                                    }
                                }
                            }
                        }
                    }
                }

                return new InventoryGrid(new List<InventoryGridItem>(), tabItems);

            }

            return null;
        }
        private InventoryGrid GetLargeInventorySubgrid(int index, List<InventoryTabItem> tabItems)
        {
            WindowName windowName = index is 0 or 2 ? WindowName.InventoryGrid0 : WindowName.InventoryGrid1;

            List<InventoryGridItem> list = new List<InventoryGridItem>();
            if (IsWindowVisible(windowName))
            {
                var inventoryGrid = GetWindow(windowName.ToString());
                for (var j = 0; j < inventoryGrid->UldManager.NodeListCount; j++)
                {
                    var subNode = inventoryGrid->UldManager.NodeList[j];
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
                return new InventoryGrid(sortedList, tabItems);
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
            private List<InventoryTabItem> _inventoryTabs;

            public InventoryGrid(List<InventoryGridItem> sortedItems, List<InventoryTabItem> tabItems)
            {
                _sortedItems = sortedItems;
                _inventoryTabs = tabItems.OrderBy(c => c.tabIndex).ToList();
            }

            public void ClearColors()
            {
                foreach (var item in _sortedItems)
                {
                    item.resNode->AddBlue = 0;
                    item.resNode->AddRed = 0;
                    item.resNode->AddGreen = 0;
                }
                foreach (var item in _inventoryTabs)
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

            public void SetTabColor(int tabIndex, int red, int green, int blue)
            {
                if (tabIndex >= 0 && _inventoryTabs.Count > tabIndex)
                {
                    _inventoryTabs[tabIndex].resNode->AddBlue = (ushort) blue;
                    _inventoryTabs[tabIndex].resNode->AddRed = (ushort) red;
                    _inventoryTabs[tabIndex].resNode->AddGreen = (ushort) green;
                }
            }

            public void SetColor(int itemIndex, Vector3 color)
            {
                if (itemIndex >= 0 && _sortedItems.Count > itemIndex)
                {
                    _sortedItems[itemIndex].resNode->AddBlue = (ushort) (color.Z * 255.0f);
                    _sortedItems[itemIndex].resNode->AddRed = (ushort) (color.X * 255.0f);
                    _sortedItems[itemIndex].resNode->AddGreen = (ushort) (color.Y * 255.0f);
                }
            }

            public void SetTabColor(int tabIndex, Vector3 color)
            {
                if (tabIndex >= 0 && _inventoryTabs.Count > tabIndex)
                {
                    _inventoryTabs[tabIndex].resNode->AddBlue = (ushort) (color.Z * 255.0f);
                    _inventoryTabs[tabIndex].resNode->AddRed = (ushort) (color.X * 255.0f);
                    _inventoryTabs[tabIndex].resNode->AddGreen = (ushort) (color.Y * 255.0f);
                }
            }
        }

        public class InventoryGridItem
        {
            public AtkResNode* resNode;

            public InventoryGridItem(AtkResNode* resNode)
            {
                this.resNode = resNode;
            }
        }

        public class InventoryTabItem
        {
            public AtkResNode* resNode;
            public int tabIndex;

            public InventoryTabItem(AtkResNode* resNode, int tabIndex)
            {
                this.resNode = resNode;
                this.tabIndex = tabIndex;
            }
        }


        public void Dispose()
        {
            _framework.Update -= FrameworkOnOnUpdateEvent;
        }
    }
}