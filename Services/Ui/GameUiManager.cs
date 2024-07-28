using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CriticalCommonLib.Services.Ui;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.Interop;

public class GameUiManager : IGameUiManager
{
    private static readonly unsafe AtkStage* stage = AtkStage.Instance();

    private readonly HashSet<Pointer<AtkUnitBase>> _visibleUnits = new(256);
    private readonly HashSet<Pointer<AtkUnitBase>> _removedUnits = new(16);
    private readonly Dictionary<Pointer<AtkUnitBase>, string> _nameCache = new(256);
    private readonly IFramework _framework;
    private readonly IGameGui _gameGui;
    private readonly Dictionary<WindowName, bool> _windowVisibility;

    public delegate void UiVisibilityChangedDelegate(WindowName windowName, bool? windowState);
    public delegate void UiUpdatedDelegate(WindowName windowName);
    public event UiVisibilityChangedDelegate? UiVisibilityChanged;

    public GameUiManager(IFramework framework, IGameGui gameGui)
    {
        _gameGui = gameGui;
        _framework = framework;
        _framework.Update += OnFrameworkUpdate;
        _windowVisibility = new Dictionary<WindowName, bool>();
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        _visibleUnits.Clear();

        foreach (var atkUnitBase in RaptureAtkModule.Instance()->RaptureAtkUnitManager.AtkUnitManager.AllLoadedUnitsList.Entries)
        {
            if (atkUnitBase.Value != null && atkUnitBase.Value->IsReady && atkUnitBase.Value->IsVisible)
                _visibleUnits.Add(atkUnitBase);
        }

        _removedUnits.Clear();

        foreach (var (address, name) in _nameCache)
        {
            if (!_visibleUnits.Contains(address) && _removedUnits.Add(address))
            {
                _nameCache.Remove(address);
                if (Enum.TryParse(name, out WindowName actualWindowName))
                {
                    Service.Framework.RunOnFrameworkThread(() =>
                    {
                        _windowVisibility[actualWindowName] = true;
                        UiVisibilityChanged?.Invoke(actualWindowName, false);
                    });
                };
            }
        }

        foreach (var address in _visibleUnits)
        {
            if (_nameCache.ContainsKey(address))
                continue;

            var name = address.Value->NameString;
            _nameCache.Add(address, name);
            if (Enum.TryParse(name, out WindowName actualWindowName))
            {
                Service.Framework.RunOnFrameworkThread(() =>
                {
                    _windowVisibility[actualWindowName] = true;
                    UiVisibilityChanged?.Invoke(actualWindowName, true);
                });
            }

        }
    }

    public static unsafe T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged {
        for (var i = 0; i < uldManager.NodeListCount; i++) {
            var n = uldManager.NodeList[i];
            if (n->NodeId != nodeId || type != null && n->Type != type.Value) continue;
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
            var focusedAddonList = focusedUnitsList->Entries;

            for (var i = 0; i < focusedAddonList.Length; i++)
            {
                var addon = focusedAddonList[i];
                var addonName = addon.Value->NameString;

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
            _framework.Update -= OnFrameworkUpdate;
            GC.SuppressFinalize(this);
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