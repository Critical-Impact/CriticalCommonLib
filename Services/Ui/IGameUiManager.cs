using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public interface IGameUiManager : IDisposable
    {
        event GameUiManager.UiVisibilityChangedDelegate? UiVisibilityChanged;
        bool IsWindowVisible(WindowName windowName);
        unsafe AtkUnitBase* GetWindow(String windowName);
        IntPtr GetWindowAsPtr(String windowName);
        bool IsWindowLoaded(WindowName windowName);
        bool IsWindowFocused(WindowName windowName);
        bool IsWindowFocused(string windowName);
        public unsafe bool TryGetAddonByName<T>(string Addon, out T* AddonPtr) where T : unmanaged;
        unsafe T* GetNodeByID<T>(AtkUldManager uldManager, uint nodeId, NodeType? type = null) where T : unmanaged;
    }
}