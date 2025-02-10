using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Services.Ui;

public class AtkShop : AtkOverlay
{
    public AtkShop(IGameGui gameGui) : base(gameGui)
    {
    }
    
    public override void Update()
    {
    }

    public override WindowName WindowName { get; set; } = WindowName.Shop;
}