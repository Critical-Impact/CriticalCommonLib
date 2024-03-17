using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public abstract class AtkOverlay : IAtkOverlay
    {
        private string? _windowNameStr;

        public virtual unsafe AtkBaseWrapper? AtkUnitBase
        {
            get
            {
                var intPtr = Service.GameGui.GetAddonByName(WindowName.ToString(), 1);
                if (intPtr == IntPtr.Zero)
                {
                    return null;
                }
                return new AtkBaseWrapper((AtkUnitBase*) intPtr);
            }
        }

        public virtual unsafe bool HasAddon
        {
            get
            {
                if (Service.GameGui == null)
                {
                    return false;
                }
                var intPtr = Service.GameGui.GetAddonByName(WindowName.ToString(), 1);
                if (intPtr == IntPtr.Zero)
                {
                    return false;
                }

                return true;
            }
        }

        public unsafe AtkBaseWrapper? GetAtkUnitBase(WindowName windowName)
        {
            var intPtr = Service.GameGui.GetAddonByName(windowName.ToString(), 1);
            if (intPtr == IntPtr.Zero)
            {
                return null;
            }
            return new AtkBaseWrapper((AtkUnitBase*) intPtr);
        }
        public abstract WindowName WindowName { get; set; }

        public string WindowNameStr
        {
            get
            {
                return _windowNameStr ??= WindowName.ToString();
            }
        }
        public virtual HashSet<WindowName>? ExtraWindows { get; } = null;
        public bool ChildrenReady(Dictionary<string, bool> windowState)
        {
            var mainWindowReady = windowState.ContainsKey(WindowNameStr) && windowState.ContainsKey(WindowNameStr);
            if (ExtraWindows == null)
            {
                return mainWindowReady;
            }

            foreach (var extraWindow in ExtraWindows)
            {
                if (!windowState.ContainsKey(extraWindow.ToString()) || windowState[extraWindow.ToString()] == false)
                {
                    return false;
                }
            }

            return true;

        }

        public abstract bool ShouldDraw { get; set; }
        public abstract bool Draw();
        public abstract void Setup();
        
        public virtual void Update()
        {
            
        }
    }
}