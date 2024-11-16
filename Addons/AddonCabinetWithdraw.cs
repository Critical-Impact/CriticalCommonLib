using AllaganLib.GameSheets.Sheets;


namespace CriticalCommonLib.Addons;

using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;


[StructLayout(LayoutKind.Explicit, Size = 6272)]
public struct AddonCabinetWithdraw
{
    [FieldOffset(0)] public unsafe AtkUnitBase* AtkUnitBase;

    [FieldOffset(24640)]
    public unsafe AtkComponentRadioButton* ArtifactArmorRadioButton;
    [FieldOffset(24648)]
    public unsafe AtkComponentRadioButton* SeasonalGear1RadioButton;
    [FieldOffset(24656)]
    public unsafe AtkComponentRadioButton* SeasonalGear2RadioButton;
    [FieldOffset(24664)]
    public unsafe AtkComponentRadioButton* SeasonalGear3RadioButton;
    [FieldOffset(24672)]
    public unsafe AtkComponentRadioButton* SeasonalGear4RadioButton;
    [FieldOffset(24680)]
    public unsafe AtkComponentRadioButton* SeasonalGear5RadioButton;
    [FieldOffset(24688)]
    public unsafe AtkComponentRadioButton* AchievementsRadioButton;
    [FieldOffset(24696)]
    public unsafe AtkComponentRadioButton* ExclusiveExtrasRadioButton;
    [FieldOffset(24704)]
    public unsafe AtkComponentRadioButton* SearchRadioButton;

    public unsafe uint SelectedTab
    {
        get
        {
            if (ArtifactArmorRadioButton != null && ArtifactArmorRadioButton->IsSelected)
            {
                return 0;
            }

            if (SeasonalGear1RadioButton != null && SeasonalGear1RadioButton->IsSelected)
            {
                return 1;
            }

            if (SeasonalGear2RadioButton != null && SeasonalGear2RadioButton->IsSelected)
            {
                return 2;
            }

            if (SeasonalGear3RadioButton != null && SeasonalGear3RadioButton->IsSelected)
            {
                return 3;
            }

            if (SeasonalGear4RadioButton != null && SeasonalGear4RadioButton->IsSelected)
            {
                return 4;
            }

            if (SeasonalGear5RadioButton != null && SeasonalGear5RadioButton->IsSelected)
            {
                return 5;
            }

            if (AchievementsRadioButton != null && AchievementsRadioButton->IsSelected)
            {
                return 6;
            }

            if (ExclusiveExtrasRadioButton != null && ExclusiveExtrasRadioButton->IsSelected)
            {
                return 7;
            }

            if (SearchRadioButton != null && SearchRadioButton->IsSelected)
            {
                return 8;
            }

            return 0;
        }
    }

    public CabinetCategoryRow GetCabinetCategorySelected()
    {
        var selectedTab = SelectedTab;
        return Service.ExcelCache.GetCabinetCategorySheet().Single(c => c.Base.MenuOrder == selectedTab);
    }

}