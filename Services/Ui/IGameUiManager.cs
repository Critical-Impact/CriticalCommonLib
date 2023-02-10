using System;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public interface IGameUiManager : IDisposable
    {
        event GameUiManager.UiVisibilityChangedDelegate? UiVisibilityChanged;
        event GameUiManager.UiUpdatedDelegate? UiUpdated;
        bool IsWindowVisible(WindowName windowName);
        bool WatchWindowState(WindowName windowName);
        unsafe AtkUnitBase* GetWindow(String windowName);
        IntPtr GetWindowAsPtr(String windowName);
        bool IsWindowLoaded(WindowName windowName);
    }
}