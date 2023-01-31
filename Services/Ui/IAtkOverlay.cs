namespace CriticalCommonLib.Services.Ui
{
    public unsafe interface IAtkOverlay
    {
        public AtkBaseWrapper? AtkUnitBase { get; }
        public WindowName WindowName { get; set; }
        
        public bool ShouldDraw { get; }
        public bool Draw();
        public void Setup();

        public void Update();

        public bool HasAddon { get;  }
    }
}