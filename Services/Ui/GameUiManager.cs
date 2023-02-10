using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using CriticalCommonLib.Helpers;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class GameUiManager : IGameUiManager
    {
        public delegate void* AddonOnUpdateDelegate(AtkUnitBase* atkUnitBase, NumberArrayData** nums, StringArrayData** strings);
        public delegate void* AddonOnSetup(AtkUnitBase* atkUnitBase, void* a2, void* a3);
        public delegate void NoReturnAddonOnUpdate(AtkUnitBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
        
        private readonly Dictionary<WindowName, bool> _windowVisibility;
        private readonly List<WindowName> _windowVisibilityWatchList;
        private readonly Dictionary<string, HookWrapper<AddonOnUpdateDelegate>> _updateHooks;
        public delegate void UiVisibilityChangedDelegate(WindowName windowName, bool? windowState);
        public delegate void UiUpdatedDelegate(WindowName windowName);
        public event UiVisibilityChangedDelegate? UiVisibilityChanged;
        public event UiUpdatedDelegate? UiUpdated;
        
        private delegate IntPtr HideShowNamedUiElementDelegate(IntPtr pThis);        

        [Signature("E8 ?? ?? ?? ?? 48 63 95", DetourName = nameof(HideNamedUiElementDetour))]
        private readonly Hook<HideShowNamedUiElementDelegate>? _hideNamedUiElementHook = null;
        
        [Signature("40 53 48 83 EC 40 48 8B 91", DetourName = nameof(ShowNamedUiElementDetour))]
        private readonly Hook<HideShowNamedUiElementDelegate>? _showNamedUiElementHook = null;
        
        public static HookWrapper<AddonOnUpdateDelegate> HookAfterAddonUpdate(void* address, NoReturnAddonOnUpdate after) => HookAfterAddonUpdate(new IntPtr(address), after);
        public static HookWrapper<AddonOnUpdateDelegate> HookAfterAddonUpdate(AtkUnitBase* atkUnitBase, NoReturnAddonOnUpdate after) => HookAfterAddonUpdate(atkUnitBase->AtkEventListener.vfunc[40], after);
        public static HookWrapper<AddonOnUpdateDelegate> HookAfterAddonUpdate(IntPtr address, NoReturnAddonOnUpdate after) {
            Hook<AddonOnUpdateDelegate>? hook = null;
            hook = Hook<AddonOnUpdateDelegate>.FromAddress(address, (atkUnitBase, nums, strings) => {
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
            var wh = new HookWrapper<AddonOnUpdateDelegate>(hook);
            return wh;
        }
        
        public GameUiManager()
        {
            SignatureHelper.Initialise(this);
            _windowVisibility = new Dictionary<WindowName, bool>();
            _windowVisibilityWatchList = new List<WindowName>();
            _updateHooks = new();
            _hideNamedUiElementHook?.Enable();
            _showNamedUiElementHook?.Enable();
        }
        
        public static T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged {
            for (var i = 0; i < uldManager.NodeListCount; i++) {
                var n = uldManager.NodeList[i];
                if (n->NodeID != nodeId || type != null && n->Type != type.Value) continue;
                return (T*)n;
            }
            PluginLog.Debug("Could not find with id " + nodeId);
            return null;
        }
        
        private IntPtr ShowNamedUiElementDetour(IntPtr pThis) {
            try
            {
                var windowName = Marshal.PtrToStringUTF8(pThis + 8)!;
                if (Enum.TryParse(windowName, out WindowName actualWindowName))
                {
                    Service.Framework.RunOnFrameworkThread(() =>
                    {
                        _windowVisibility[actualWindowName] = true;
                        UiVisibilityChanged?.Invoke(actualWindowName, true);
                        AddAddonUpdateHook(actualWindowName);
                    });
                }
            }
            catch
            {
                PluginLog.Debug("Exception while detouring ShowNamedUiElementDetour");
            }
            
            return _showNamedUiElementHook!.Original(pThis);
        }

        private IntPtr HideNamedUiElementDetour(IntPtr pThis) {
            try
            {
                var windowName = Marshal.PtrToStringUTF8(pThis + 8)!;
                if (Enum.TryParse(windowName, out WindowName actualWindowName))
                {
                    Service.Framework.RunOnFrameworkThread(() =>
                    {
                        UiVisibilityChanged?.Invoke(actualWindowName, false);
                        _windowVisibility[actualWindowName] = false;
                    });
                }
            }
            catch
            {
                PluginLog.Debug("Exception while detouring HideNamedUiElementDetour");
            }

            return _hideNamedUiElementHook!.Original(pThis);
        }

        public bool IsWindowVisible(WindowName windowName)
        {
            if (!_windowVisibility.ContainsKey(windowName))
            {
                return false;
            }
            return _windowVisibility[windowName];
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

        private void AfterUpdate(String windowName, AtkUnitBase* atkunitbase, NumberArrayData** numberarraydata, StringArrayData** stringarraydata)
        {
            Service.Framework.RunOnTick(() =>
            {
                WindowName actualWindowName;
                if (WindowName.TryParse(windowName, out actualWindowName))
                {
                    Service.Framework.RunOnFrameworkThread(() => { UiUpdated?.Invoke(actualWindowName); });
                }
            });

        }

        public AtkUnitBase* GetWindow(String windowName) {
            var atkBase = Service.Gui.GetAddonByName(windowName, 1);
            if (atkBase == IntPtr.Zero)
            {
                return null;
            }
            return (AtkUnitBase*) atkBase;
        }

        public IntPtr GetWindowAsPtr(String windowName) {
            var atkBase = Service.Gui.GetAddonByName(windowName, 1);
            return atkBase;
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
        
        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(!_disposed && disposing)
            {
                foreach (var hook in _updateHooks)
                {
                    hook.Value.Dispose();
                }
                _hideNamedUiElementHook?.Dispose();
                _showNamedUiElementHook?.Dispose();
            }
            _disposed = true;         
        }
        
        ~GameUiManager()
        {
#if DEBUG
            // In debug-builds, make sure that a warning is displayed when the Disposable object hasn't been
            // disposed by the programmer.

            if( _disposed == false )
            {
                PluginLog.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
            }
#endif
            Dispose (true);
        }
    }
}