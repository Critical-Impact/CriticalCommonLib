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
    public unsafe class GameUiManager : IDisposable
    {
        public delegate void* AddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);
        public delegate void* AddonOnSetup(AtkUnitBase* atkUnitBase, void* a2, void* a3);
        public delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
        
        private AtkUnitBase* selectedUnitBase = null;
        
        private Dictionary<WindowName, bool> _windowVisibility;
        private List<WindowName> _windowVisibilityWatchList;
        private Dictionary<WindowName, int> _tabCache;
        private Dictionary<string, HookWrapper<AddonOnUpdate>> _updateHooks;
        public delegate void UiVisibilityChangedDelegate(WindowName windowName, bool? windowState);
        public delegate void UiUpdatedDelegate(WindowName windowName);
        public event UiVisibilityChangedDelegate? UiVisibilityChanged;
        public event UiUpdatedDelegate? UiUpdated;
        private delegate IntPtr HideShowNamedUiElementDelegate(IntPtr pThis);        
        private readonly Hook<HideShowNamedUiElementDelegate> _hideHook, _showHook;

        private static readonly string HideNamedUiElementSignature = "40 57 48 83 EC 20 48 8B F9 48 8B 89 C8 00 00 00 48 85 C9 0F ?? ?? ?? ?? ?? 8B 87 B0 01 00 00 C1 E8 07 A8 01";
        private static readonly string ShowNamedUiElementSignature = "40 53 48 83 EC 40 48 8B 91 C8 00 00 00 48 8B D9 48 85 D2";
        
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

        
        public GameUiManager()
        {
            _windowVisibility = new Dictionary<WindowName, bool>();
            _windowVisibilityWatchList = new List<WindowName>();
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
        }
        
        public static AtkResNode* GetNodeByID(AtkUldManager uldManager, uint nodeId, NodeType? type = null) => GetNodeByID<AtkResNode>(uldManager, nodeId, type);
        public static T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged {
            for (var i = 0; i < uldManager.NodeListCount; i++) {
                var n = uldManager.NodeList[i];
                if (n->NodeID != nodeId || type != null && n->Type != type.Value) continue;
                return (T*)n;
            }
            PluginLog.Debug("Could not find with id " + nodeId);
            return null;
        }
        
        private unsafe IntPtr ShowNamedUiElementDetour(IntPtr pThis) {
            var res = _showHook.Original(pThis);
            var windowName = Marshal.PtrToStringUTF8(pThis + 8)!;
            WindowName actualWindowName;
            if (WindowName.TryParse(windowName, out actualWindowName))
            {
                _windowVisibility[actualWindowName] = true;
                UiVisibilityChanged?.Invoke(actualWindowName, true);
                AddAddonUpdateHook(actualWindowName);
            }
            return res;
        }

        private unsafe IntPtr HideNamedUiElementDetour(IntPtr pThis) {
            var res = _hideHook.Original(pThis);
            var windowName = Marshal.PtrToStringUTF8(pThis + 8)!;
            WindowName actualWindowName;
            if (WindowName.TryParse(windowName, out actualWindowName))
            {
                UiVisibilityChanged?.Invoke(actualWindowName, false);
                _windowVisibility[actualWindowName] = false;
            }
            return res;
        }


        public bool WatchWindowState(WindowName windowName)
        {
            if (!_windowVisibilityWatchList.Contains(windowName))
            {
                _windowVisibilityWatchList.Add(windowName);
                return AddAddonUpdateHook(windowName);
            }

            return true;
        }

        private bool AddAddonUpdateHook(WindowName windowName)
        {
            var name = windowName.ToString();
            var atkBase = GetWindow(windowName.ToString());
            if (atkBase != null)
            {
                if (!_updateHooks.ContainsKey(name))
                {
                    var hook = HookAfterAddonUpdate(atkBase,
                        (atkUnitBase, data, arrayData) => AfterUpdate(name, atkUnitBase, data, arrayData));
                    hook.Enable();
                    _updateHooks.Add(name, hook);
                    return true;
                }
                else
                {
                    _updateHooks[name].Enable();
                    return true;
                }
            }

            return false;
        }

        private void RemoveAddonUpdateHook(WindowName windowName)
        {
            var name = windowName.ToString();
            if (_updateHooks.ContainsKey(name))
            {
                var hook = _updateHooks[name];
                hook.Dispose();
                _updateHooks.Remove(name);
            }
        }

        private void AfterUpdate(String windowName, AtkUnitBase* atkunitbase, NumberArrayData** numberarraydata, StringArrayData** stringarraydata)
        {
            WindowName actualWindowName;
            if (WindowName.TryParse(windowName, out actualWindowName))
            {
                UiUpdated?.Invoke(actualWindowName);
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

        public void Dispose()
        {
            foreach (var hook in _updateHooks)
            {
                hook.Value.Dispose();
            }
            _hideHook.Dispose();
            _showHook.Dispose();
        }
    }
}