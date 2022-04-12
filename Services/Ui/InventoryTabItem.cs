using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class InventoryTabItem
    {
        public AtkResNode* resNode;
        public int tabIndex;

        public InventoryTabItem(AtkResNode* resNode, int tabIndex)
        {
            this.resNode = resNode;
            this.tabIndex = tabIndex;
        }
    }
}