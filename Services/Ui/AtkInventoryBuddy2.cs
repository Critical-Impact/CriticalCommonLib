using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkInventoryBuddy2 : AtkInventoryBuddy
    {
        public override WindowName WindowName { get; set; } = WindowName.InventoryBuddy2;

        public AtkInventoryBuddy2(IGameGui gameGui) : base(gameGui)
        {
        }
    }
}