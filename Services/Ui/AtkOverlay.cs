using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public abstract unsafe class AtkOverlay
    {
        public unsafe AtkUnitBase* AtkUnitBase
        {
            get
            {
                return (AtkUnitBase*) Service.Gui.GetAddonByName(WindowName.ToString(), 1);
            }
        }
        public abstract WindowName WindowName { get; set; }
        
        public abstract bool ShouldDraw { get; set; }
        public abstract bool Draw();
        public abstract void Setup();
    }
}