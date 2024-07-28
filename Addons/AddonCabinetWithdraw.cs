namespace CriticalCommonLib.Addons;

using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

[StructLayout(LayoutKind.Explicit, Size = 0x1880)]
public struct AddonCabinetWithdraw
{
    [FieldOffset(0x0)] public unsafe AtkUnitBase* AtkUnitBase;

    [FieldOffset(0x3AB8)]
    public unsafe AtkComponentRadioButton* ArtifactArmorRadioButton;
    [FieldOffset(0x3AC0)]
    public unsafe AtkComponentRadioButton* SeasonalGear1RadioButton;
    [FieldOffset(0x3AC8)]
    public unsafe AtkComponentRadioButton* SeasonalGear2RadioButton;
    [FieldOffset(0x3AD0)]
    public unsafe AtkComponentRadioButton* SeasonalGear3RadioButton;
    [FieldOffset(0x3AD8)]
    public unsafe AtkComponentRadioButton* SeasonalGear4RadioButton;
    [FieldOffset(0x3AE0)]
    public unsafe AtkComponentRadioButton* SeasonalGear5RadioButton;
    [FieldOffset(0x3AE8)]
    public unsafe AtkComponentRadioButton* AchievementsRadioButton;
    [FieldOffset(0x3AF0)]
    public unsafe AtkComponentRadioButton* ExclusiveExtrasRadioButton;
    [FieldOffset(0x3AF8)]
    public unsafe AtkComponentRadioButton* SearchRadioButton;

    public unsafe uint SelectedTab
    {
        get
        {
            if (ArtifactArmorRadioButton->IsSelected)
            {
                return 0;
            }

            if (SeasonalGear1RadioButton->IsSelected)
            {
                return 1;
            }

            if (SeasonalGear2RadioButton->IsSelected)
            {
                return 2;
            }

            if (SeasonalGear3RadioButton->IsSelected)
            {
                return 3;
            }

            if (SeasonalGear4RadioButton->IsSelected)
            {
                return 4;
            }

            if (SeasonalGear5RadioButton->IsSelected)
            {
                return 5;
            }

            if (AchievementsRadioButton->IsSelected)
            {
                return 6;
            }

            if (ExclusiveExtrasRadioButton->IsSelected)
            {
                return 7;
            }

            if (SearchRadioButton->IsSelected)
            {
                return 8;
            }

            return 0;
        }
    }

    public CabinetCategory GetCabinetCategorySelected()
    {
        var selectedTab = SelectedTab;
        return Service.ExcelCache.GetCabinetCategorySheet().Single(c => c.MenuOrder == selectedTab);
    }

}