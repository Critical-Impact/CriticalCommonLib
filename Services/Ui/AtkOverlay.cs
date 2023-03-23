using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public abstract class AtkOverlay : IAtkOverlay
    {
        public virtual unsafe AtkBaseWrapper? AtkUnitBase
        {
            get
            {
                var intPtr = Service.Gui.GetAddonByName(WindowName.ToString(), 1);
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
                if (Service.Gui == null)
                {
                    return false;
                }
                var intPtr = Service.Gui.GetAddonByName(WindowName.ToString(), 1);
                if (intPtr == IntPtr.Zero)
                {
                    return false;
                }

                return true;
            }
        }

        public unsafe AtkBaseWrapper? GetAtkUnitBase(WindowName windowName)
        {
            var intPtr = Service.Gui.GetAddonByName(windowName.ToString(), 1);
            if (intPtr == IntPtr.Zero)
            {
                return null;
            }
            return new AtkBaseWrapper((AtkUnitBase*) intPtr);
        }
        public abstract WindowName WindowName { get; set; }
        public virtual HashSet<WindowName>? ExtraWindows { get; } = null;
        public abstract bool ShouldDraw { get; set; }
        public abstract bool Draw();
        public abstract void Setup();
        
        public virtual void Update()
        {
            
        }
    }
}