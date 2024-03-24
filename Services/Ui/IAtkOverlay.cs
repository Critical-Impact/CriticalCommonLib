using System.Collections.Generic;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe interface IAtkOverlay
    {
        public AtkBaseWrapper? AtkUnitBase { get; }
        public WindowName WindowName { get; set; }
        public string WindowNameStr { get; }
        public HashSet<WindowName>? ExtraWindows { get; }

        public bool ChildrenReady(Dictionary<string, bool> windowState);
        public void Update();

        public bool HasAddon { get;  }
        
        public delegate void AtkUpdate();
        public event AtkUpdate? AtkUpdated;
    }
}