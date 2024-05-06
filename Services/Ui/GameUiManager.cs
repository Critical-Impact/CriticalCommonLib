using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CriticalCommonLib.Services.Ui;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

public class GameUiManager : IGameUiManager
{
    private readonly IGameInteropProvider _provider;

    private static readonly unsafe AtkStage* stage = AtkStage.GetSingleton();

    private delegate IntPtr HideShowNamedUiElementDelegate(IntPtr pThis);        

    [Signature("E8 ?? ?? ?? ?? 48 63 95", DetourName = nameof(HideNamedUiElementDetour))]
    private readonly Hook<HideShowNamedUiElementDelegate>? _hideNamedUiElementHook = null;
        
    [Signature("40 53 48 83 EC 40 48 8B 91", DetourName = nameof(ShowNamedUiElementDetour))]
    private readonly Hook<HideShowNamedUiElementDelegate>? _showNamedUiElementHook = null;
    
    private readonly IGameGui _gameGui;
    private readonly Dictionary<WindowName, bool> _windowVisibility;
    private readonly List<WindowName> _windowVisibilityWatchList;
    
    public delegate void UiVisibilityChangedDelegate(WindowName windowName, bool? windowState);
    public delegate void UiUpdatedDelegate(WindowName windowName);
    public event UiVisibilityChangedDelegate? UiVisibilityChanged;

    public GameUiManager(IGameInteropProvider provider, IGameGui gameGui)
    {
        _gameGui = gameGui;
        _provider = provider;
        provider.InitializeFromAttributes(this);
        _windowVisibility = new Dictionary<WindowName, bool>();
        _windowVisibilityWatchList = new List<WindowName>();
        _hideNamedUiElementHook?.Enable();
        _showNamedUiElementHook?.Enable();
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
                });
            }
        }
        catch
        {
            Service.Log.Debug("Exception while detouring ShowNamedUiElementDetour");
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
            Service.Log.Debug("Exception while detouring HideNamedUiElementDetour");
        }

        return _hideNamedUiElementHook!.Original(pThis);
    }
    
    public static unsafe T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged {
        for (var i = 0; i < uldManager.NodeListCount; i++) {
            var n = uldManager.NodeList[i];
            if (n->NodeID != nodeId || type != null && n->Type != type.Value) continue;
            return (T*)n;
        }
        Service.Log.Debug("Could not find with id " + nodeId);
        return null;
    }

    public bool IsWindowVisible(WindowName windowName)
    {
        if (!_windowVisibility.ContainsKey(windowName))
        {
            return false;
        }
        return _windowVisibility[windowName];
    }


    public unsafe AtkUnitBase* GetWindow(string windowName)
    {
        var atkBase = _gameGui.GetAddonByName(windowName, 1);
        if (atkBase == IntPtr.Zero)
        {
            return null;
        }
        return (AtkUnitBase*) atkBase;
    }

    public nint GetWindowAsPtr(string windowName)
    {
        var atkBase = Service.GameGui.GetAddonByName(windowName, 1);
        return atkBase;
    }

    public bool IsWindowLoaded(WindowName windowName)
    {
        var atkBase = Service.GameGui.GetAddonByName(windowName.ToString(), 1);
        if (atkBase == IntPtr.Zero)
        {
            return false;
        }
        return true;
    }

    public bool IsWindowFocused(WindowName windowName)
    {
        return IsWindowFocused(windowName.ToString());
    }

    public unsafe bool IsWindowFocused(string windowName)
    {
        try
        {
            var focusedUnitsList = &stage->RaptureAtkUnitManager->AtkUnitManager.FocusedUnitsList;
            var focusedAddonList = focusedUnitsList->EntriesSpan;

            for (var i = 0; i < focusedAddonList.Length; i++)
            {
                var addon = focusedAddonList[i];
                var addonName = Marshal.PtrToStringAnsi(new IntPtr(addon.Value->Name));

                if (addonName == windowName)
                {
                    return true;
                }
            }

            for (var i = 0; i < focusedUnitsList->Count; i++)
            {
                var addon = focusedAddonList[i];

            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public unsafe bool TryGetAddonByName<T>(string addon, out T* addonPtr) where T : unmanaged
    {
        var a = Service.GameGui.GetAddonByName(addon, 1);
        if (a == IntPtr.Zero)
        {
            addonPtr = null;
            return false;
        }
        else
        {
            addonPtr = (T*)a;
            return true;
        }
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
            Service.Log.Error("There is a disposable object which hasn't been disposed before the finalizer call: " + (this.GetType ().Name));
        }
#endif
        Dispose (true);
    }
}