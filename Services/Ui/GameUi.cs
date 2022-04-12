using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using CriticalCommonLib.Helpers;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class GameUi : IDisposable
    {
        public delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);
        public delegate void* AddonOnSetup(AtkUnitBase* atkUnitBase, void* a2, void* a3);
        public delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
        
        private AtkUnitBase* selectedUnitBase = null;
        
        private Dictionary<WindowName, bool> _windowVisibility;
        private List<WindowName> _windowVisibilityWatchList;
        private Dictionary<WindowName,GameWindow> _windowCache;
        private Dictionary<WindowName, int> _tabCache;
        private Dictionary<string, HookWrapper<AddonOnUpdate>> _updateHooks;
        public delegate void UiVisibilityChangedDelegate(WindowName windowName, bool? windowState);
        public event UiVisibilityChangedDelegate? UiVisibilityChanged;
        private delegate IntPtr HideShowNamedUiElementDelegate(IntPtr pThis);        
        private readonly Hook<HideShowNamedUiElementDelegate> _hideHook, _showHook;

        private static readonly string HideNamedUiElementSignature = "40 57 48 83 EC 20 48 8B F9 48 8B 89 C8 00 00 00 48 85 C9 0F ?? ?? ?? ?? ?? 8B 87 B0 01 00 00 C1 E8 07 A8 01";
        private static readonly string ShowNamedUiElementSignature = "40 53 48 83 EC 40 48 8B 91 C8 00 00 00 48 8B D9 48 85 D2";
        
        /*private delegate* unmanaged<IntPtr, void> _originalFunctionPointer = null;
        private DetourDelegate _detourPtr;

        private delegate void DetourDelegate(IntPtr thisPtr);

        void MyCoolDetour(IntPtr retainerList)
        {
            my_cool_stuff();
            _originalFunctionPointer(retainerList);
        }

        void Attach(AtkUnitBase* retainerList)
        {
            var detourPtr = Marshal.GetFunctionPointerForDelegate<DetourDelegate>(  MyCoolDetour);
            _originalFunctionPointer = (delegate* unmanaged<IntPtr, void>)retainerList->VTable[40];
            retainerList->VTable[40] = detourPtr;
        }*/
        
        public static HookWrapper<AddonOnUpdate> HookAfterAddonUpdate(void* address, NoReturnAddonOnUpdate after) => HookAfterAddonUpdate(new IntPtr(address), after);
        public static HookWrapper<AddonOnUpdate> HookAfterAddonUpdate(AtkUnitBase* atkUnitBase, NoReturnAddonOnUpdate after) => HookAfterAddonUpdate(atkUnitBase->AtkEventListener.vfunc[40], after);
        public static HookWrapper<AddonOnUpdate> HookAfterAddonUpdate(IntPtr address, NoReturnAddonOnUpdate after) {
            Hook<AddonOnUpdate>? hook = null;
            hook = new Hook<AddonOnUpdate>(address, (atkUnitBase, nums, strings) => {
                if (hook != null)
                {
                    var retVal = hook.Original(atkUnitBase, nums, strings);
                    try {
                        after(atkUnitBase, nums, strings);
                    } catch (Exception ex) {
                        PluginLog.Error(ex.ToString());
                        hook.Disable();
                    }
                    return retVal;
                }

                return null;
            });
            var wh = new HookWrapper<AddonOnUpdate>(hook);
            return wh;
        }

        
        public GameUi()
        {
            _windowVisibility = new Dictionary<WindowName, bool>();
            _windowVisibilityWatchList = new List<WindowName>();
            _windowCache = new Dictionary<WindowName, GameWindow>();
            _tabCache = new Dictionary<WindowName, int>();
            _updateHooks = new();

            var hideNamedUiElementAddress = Service.Scanner.ScanText(HideNamedUiElementSignature);
            var showNamedUiElementAddress = Service.Scanner.ScanText(ShowNamedUiElementSignature);

            _hideHook = new Hook<HideShowNamedUiElementDelegate>(hideNamedUiElementAddress,
                this.HideNamedUiElementDetour);
            _showHook = new Hook<HideShowNamedUiElementDelegate>(showNamedUiElementAddress,
                this.ShowNamedUiElementDetour);
            
            _hideHook.Enable();
            _showHook.Enable();
                
            Service.Framework.Update += FrameworkOnOnUpdateEvent;
        }
        
        public static AtkResNode* GetNodeByID(AtkUldManager uldManager, uint nodeId, NodeType? type = null) => GetNodeByID<AtkResNode>(uldManager, nodeId, type);
        public static T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged {
            for (var i = 0; i < uldManager.NodeListCount; i++) {
                var n = uldManager.NodeList[i];
                if (n->NodeID != nodeId || type != null && n->Type != type.Value) continue;
                return (T*)n;
            }
            return null;
        }
        
        private unsafe IntPtr ShowNamedUiElementDetour(IntPtr pThis) {
            var res = _showHook.Original(pThis);
            var windowName = Marshal.PtrToStringUTF8(pThis + 8)!;
            WindowName actualWindowName;
            if (WindowName.TryParse(windowName, out actualWindowName))
            {
                PluginLog.Log("Adding " + windowName + " from window cache");
                _windowVisibility[actualWindowName] = true;
                if (actualWindowName == WindowName.RetainerList)
                {
                    var retainerList = GetRetainerList();
                    if (retainerList != null)
                    {
                        _windowCache[actualWindowName] = retainerList;
                    }
                }
                AddAddonUpdateHook(actualWindowName);
                WindowStateChanged(actualWindowName, true);
            }
            return res;
        }

        private unsafe IntPtr HideNamedUiElementDetour(IntPtr pThis) {
            var res = _hideHook.Original(pThis);
            var windowName = Marshal.PtrToStringUTF8(pThis + 8)!;
            WindowName actualWindowName;
            if (WindowName.TryParse(windowName, out actualWindowName))
            {
                _windowVisibility[actualWindowName] = false;
                if (_windowCache.ContainsKey(actualWindowName))
                {
                    PluginLog.Log("Removing " + windowName + " from window cache");
                    _windowCache.Remove(actualWindowName);
                }
                RemoveAddonUpdateHook(actualWindowName);
                WindowStateChanged(actualWindowName, false);
            }
            return res;
        }

        private void WindowStateChanged(WindowName item, bool isWindowVisible)
        {
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
                            UiVisibilityChanged?.Invoke(item, isWindowVisible);
                        }
                    }
                    else
                    {
                        _tabCache[WindowName.Inventory] = currentTab;
                        UiVisibilityChanged?.Invoke(item, isWindowVisible);
                    }
                }
                else
                {
                    UiVisibilityChanged?.Invoke(item, isWindowVisible);
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
                            UiVisibilityChanged?.Invoke(item,isWindowVisible);
                        }
                    }
                    else
                    {
                        _tabCache[WindowName.Inventory] = currentTab;
                        UiVisibilityChanged?.Invoke(item,isWindowVisible);
                    }
                }
                else
                {
                    UiVisibilityChanged?.Invoke(item, isWindowVisible);
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
                            UiVisibilityChanged?.Invoke(item, isWindowVisible);
                        }
                    }
                    else
                    {
                        _tabCache[WindowName.Inventory] = currentTab;
                        UiVisibilityChanged?.Invoke(item, isWindowVisible);
                    }
                }
                else
                {
                    UiVisibilityChanged?.Invoke(item, isWindowVisible);
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
                            UiVisibilityChanged?.Invoke(item, isWindowVisible);
                        }
                    }
                    else
                    {
                        _tabCache[WindowName.InventoryRetainer] = currentTab;
                        UiVisibilityChanged?.Invoke(item, isWindowVisible);
                    }
                }
                else
                {
                    UiVisibilityChanged?.Invoke(item, isWindowVisible);
                }
            }
            else if (item == WindowName.InventoryBuddy)
            {
                if (isWindowVisible)
                {
                    var saddlebagUiAddon = GetChocoboSaddlebag();
                    if (saddlebagUiAddon != null)
                    {
                        var currentTab = saddlebagUiAddon.SaddleBagSelected;
                        if (_tabCache.ContainsKey(WindowName.InventoryBuddy))
                        {
                            if (currentTab != _tabCache[WindowName.InventoryBuddy])
                            {
                                PluginLog.Verbose("GameUi: Saddlebag tab changed to " + currentTab);
                                _tabCache[WindowName.InventoryBuddy] = currentTab;
                                UiVisibilityChanged?.Invoke(item, isWindowVisible);
                            }
                        }
                        else
                        {
                            _tabCache[WindowName.InventoryBuddy] = currentTab;
                            UiVisibilityChanged?.Invoke(item, isWindowVisible);
                        }
                    }
                }
                else
                {
                    UiVisibilityChanged?.Invoke(item, isWindowVisible);
                }
            }
            else if (item == WindowName.InventoryBuddy2)
            {
                if (isWindowVisible)
                {
                    var saddlebagUiAddon = GetChocoboSaddlebag2();
                    if (saddlebagUiAddon != null)
                    {
                        var currentTab = saddlebagUiAddon.SaddleBagSelected;
                        if (_tabCache.ContainsKey(WindowName.InventoryBuddy2))
                        {
                            if (currentTab != _tabCache[WindowName.InventoryBuddy2])
                            {
                                PluginLog.Verbose("GameUi: Saddlebag tab changed to " + currentTab);
                                _tabCache[WindowName.InventoryBuddy2] = currentTab;
                                UiVisibilityChanged?.Invoke(item, isWindowVisible);
                            }
                        }
                        else
                        {
                            _tabCache[WindowName.InventoryBuddy2] = currentTab;
                            UiVisibilityChanged?.Invoke(item, isWindowVisible);
                        }
                    }
                }
                else
                {
                    UiVisibilityChanged?.Invoke(item, isWindowVisible);
                }
            }
            else
            {
                UiVisibilityChanged?.Invoke(item, isWindowVisible);
            }
        }

        private void FrameworkOnOnUpdateEvent(Framework framework)
        {
            
                
            
        }

        public void WatchWindowState(WindowName windowName)
        {
            if (!_windowVisibilityWatchList.Contains(windowName))
            {
                _windowVisibilityWatchList.Add(windowName);
                AddAddonUpdateHook(windowName);
            }
        }

        private void AddAddonUpdateHook(WindowName windowName)
        {
            var name = windowName.ToString();
            var atkBase = GetWindow(windowName.ToString());
            if (atkBase != null)
            {
                PluginLog.Log("Trying to add " + name + " after hook");
                if (!_updateHooks.ContainsKey(name))
                {
                    PluginLog.Log("Creating hook");
                    var hook = HookAfterAddonUpdate(atkBase,
                        (atkUnitBase, data, arrayData) => AfterUpdate(name, atkUnitBase, data, arrayData));
                    hook.Enable();
                    _updateHooks.Add(name, hook);
                }
                else
                {
                    PluginLog.Log("Enabling hook");
                    _updateHooks[name].Enable();
                }
            }
        }

        private void RemoveAddonUpdateHook(WindowName windowName)
        {
            var name = windowName.ToString();
            if (_updateHooks.ContainsKey(name))
            {
                var hook = _updateHooks[name];
                hook.Dispose();
                _updateHooks.Remove(name);
                PluginLog.Log("Disabling hook");
            }
        }

        private void AfterUpdate(String windowName, AtkUnitBase* atkunitbase, NumberArrayData** numberarraydata, StringArrayData** stringarraydata)
        {
            WindowName actualWindowName;
            if (WindowName.TryParse(windowName, out actualWindowName))
            {
                if (actualWindowName == WindowName.RetainerList)
                {
                    UiVisibilityChanged?.Invoke(actualWindowName, true);
                    PluginLog.Error("Update for " + windowName + " has been hit.");
                }
            }

        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public AtkUnitBase* GetWindow(String windowName) {
            var atkBase = Service.Gui.GetAddonByName(windowName, 1);
            if (atkBase == IntPtr.Zero)
            {
                return null;
            }
            return (AtkUnitBase*) atkBase;
        }

        public bool IsWindowLoaded(WindowName windowName)
        {
            var atkBase = Service.Gui.GetAddonByName(windowName.ToString(), 1);
            if (atkBase == IntPtr.Zero)
            {
                return false;
            }
            return true;
        }
        
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public bool IsWindowVisible(WindowName windowName)
        {
            if (!IsWindowLoaded(windowName))
            {
                return false;
            }
            var atkUnitBase = GetWindow(windowName.ToString());
            return atkUnitBase->IsVisible;
        }

        public InventoryGrid? GetRetainerGrid(int index)
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
                var sortedList = list.OrderBy(c => c._resNode->Y).ThenBy(c => c._resNode->X).ToList();
                //TODO: Handle retainer tabs?
                return new InventoryGrid(sortedList, new List<InventoryTabItem>());
            }

            return null;
        }
        
        public SaddlebagUIAddon? GetChocoboSaddlebag()
        {
            WindowName windowName = WindowName.InventoryBuddy;

            if (IsWindowVisible(windowName))
            {
                var buddyGrid = GetWindow(windowName.ToString());
                var saddleBagUiAddon = new SaddlebagUIAddon(buddyGrid);
                return saddleBagUiAddon;
            }

            return null;
        }
        
        public SaddlebagUIAddon? GetChocoboSaddlebag2()
        {
            WindowName windowName = WindowName.InventoryBuddy2;

            if (IsWindowVisible(windowName))
            {
                var buddyGrid = GetWindow(windowName.ToString());
                var saddleBagUiAddon = new SaddlebagUIAddon(buddyGrid);
                return saddleBagUiAddon;
            }

            return null;
        }
        
        public InventoryGrid? GetPrimaryInventoryGrid(int index)
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
                var sortedList = list.OrderBy(c => c._resNode->Y).ThenBy(c => c._resNode->X).ToList();
                return new InventoryGrid(sortedList, new List<InventoryTabItem>());
            }

            return null;
        }
        
        private int GetNormalInventoryGridIndex()
        {
            WindowName windowName = WindowName.Inventory;
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
                                if (name.Length > 0)
                                {
                                    if (int.TryParse(name[..1], out tabIndex))
                                    {
                                        return tabIndex;
                                    }
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
        
        public InventoryGrid? GetNormalInventoryGrid(int index)
        {
            WindowName windowName = WindowName.Inventory;
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
                                    var textValue = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue;
                                    if (textValue.Length > 0) name = textValue[..1];
                                    int tabIndex;
                                    if (name != "" && int.TryParse(name, out tabIndex))
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
        private InventoryGrid? GetNormalInventorySubgrid(List<InventoryTabItem> tabItems)
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
                var sortedList = list.OrderBy(c => c._resNode->Y).ThenBy(c => c._resNode->X).ToList();
                return new InventoryGrid(sortedList, tabItems);
            }

            return null;
        }
        
        public InventoryGrid? GetNormalRetainerInventoryGrid(int index)
        {
            WindowName windowName = WindowName.InventoryRetainer;
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
        
        private InventoryGrid? GetNormalRetainerInventorySubgrid(List<InventoryTabItem> tabItems)
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
                var sortedList = list.OrderBy(c => c._resNode->Y).ThenBy(c => c._resNode->X).ToList();
                return new InventoryGrid(sortedList, tabItems);
            }

            return null;
        }
        private int GetLargeInventoryGridIndex()
        {
            WindowName windowName = WindowName.InventoryLarge;
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
                                if (name.Length > 0)
                                {
                                    if (int.TryParse(name[..1], out tabIndex))
                                    {
                                        return tabIndex;
                                    }
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
                                if (name.Length > 0)
                                {
                                    if (int.TryParse(name[..1], out tabIndex))
                                    {
                                        return tabIndex;
                                    }
                                }
                            }
                        }
                    }
                }
                
            }

            return -1;
        }
        public InventoryGrid? GetLargeInventoryGrid(int index)
        {
            WindowName windowName = WindowName.InventoryLarge;
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
                                    var textValue = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue;
                                    if (textValue.Length > 0) name = textValue[..1];
                                    int tabIndex;
                                    if (name != "" && int.TryParse(name, out tabIndex))
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
        public InventoryGrid? GetLargeRetainerInventoryGrid()
        {
            WindowName windowName = WindowName.InventoryRetainerLarge;
            if (IsWindowVisible(windowName))
            {
                var primaryGrid = GetWindow(windowName.ToString());
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
                            var name = "";
                            for (var j2 = 0; j2 < componentInfo.NodeListCount; j2++)
                            {
                                var subNode2 = componentInfo.NodeList[j2];
                                if (subNode2->Type == NodeType.NineGrid)
                                {
                                    if (subNode2->NodeID == 3 && subNode2->IsVisible)
                                    {
                                    }
                                }
                                else if (subNode2->Type == NodeType.Text)
                                {
                                    var textNode = (AtkTextNode*) subNode2;
                                    var textValue = MemoryHelper.ReadSeString(&textNode->NodeText).TextValue;
                                    if (textValue.Length > 0) name = textValue[..1];
                                    int tabIndex;
                                    if (name != "" &&int.TryParse(name, out tabIndex))
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
        private InventoryGrid? GetLargeInventorySubgrid(int index, List<InventoryTabItem> tabItems)
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
                var sortedList = list.OrderBy(c => c._resNode->Y).ThenBy(c => c._resNode->X).ToList();
                return new InventoryGrid(sortedList, tabItems);
            }

            return null;
        }
        public RetainerList? GetRetainerList()
        {
            List<RetainerListItem> list = new List<RetainerListItem>();
            if (IsWindowVisible(WindowName.RetainerList))
            {
                if (_windowCache.ContainsKey(WindowName.RetainerList))
                {
                    return (RetainerList)_windowCache[WindowName.RetainerList];
                }
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
                            var listNode = (AtkComponentNode*) subNode;
                            var listUldManager = listNode->Component->UldManager;
                            var listAbsoluteX = absoluteX + subNode->X;
                            var listAbsoluteY = absoluteY + subNode->Y;
                            for (var j2 = 0; j2 < listUldManager.NodeListCount; j2++)
                            {
                                var subNode2 = listUldManager.NodeList[j2];
                                if ((int) subNode2->Type >= 1000)
                                {
                                    var component2 = (AtkComponentNode*) subNode;
                                    var componentInfo2 = component2->Component->UldManager;
                                    var objectInfo2 = (AtkUldComponentInfo*) componentInfo2.Objects;
                                    if (objectInfo2->ComponentType == ComponentType.List)
                                    {
                                        var listItemComponent = (AtkComponentNode*) subNode2;
                                        var listItemManager = listItemComponent->Component->UldManager;
                                        for (var j3 = 0; j3 < listItemManager.NodeListCount; j3++)
                                        {
                                            var gridNode = listItemManager.NodeList[j3];
                                            if (gridNode->Type == NodeType.Text && gridNode->NodeID == 3)
                                            {
                                                var retainerNameNode = (AtkTextNode*) gridNode;
                                                var retainerName =
                                                    Marshal.PtrToStringAnsi(
                                                        new IntPtr(retainerNameNode->NodeText.StringPtr));
                                                //Seem to be selection nodes or something that have no text
                                                if (!string.IsNullOrEmpty(retainerName))
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

        public void Dispose()
        {
            foreach (var hook in _updateHooks)
            {
                hook.Value.Dispose();
            }
            _hideHook.Dispose();
            _showHook.Dispose();
            Service.Framework.Update -= FrameworkOnOnUpdateEvent;
        }
    }
}