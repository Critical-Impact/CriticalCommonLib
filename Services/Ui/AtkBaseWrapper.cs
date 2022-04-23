using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public class AtkBaseWrapper
    {
        public unsafe AtkBaseWrapper(AtkUnitBase* atkUnitBase)
        {
            AtkUnitBase = atkUnitBase;
        }
        public unsafe AtkUnitBase* AtkUnitBase;
    }
}