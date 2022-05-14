using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Extensions
{
    public static class EnumExtensions
    {
        public static string FormattedName(this CharacterSex characterSex)
        {
            switch (characterSex)
            {
                case CharacterSex.Both:
                    return "Both";
                case CharacterSex.Either:
                    return "Either"; 
                case CharacterSex.Female:
                    return "Female"; 
                case CharacterSex.Male:
                    return "Male"; 
                case CharacterSex.FemaleOnly:
                    return "Female Only"; 
                case CharacterSex.MaleOnly:
                    return "Male Only"; 
                case CharacterSex.NotApplicable:
                    return "N/A"; 
            }

            return "Unknown";
        }
        public static string FormattedName(this CharacterRace characterRace)
        {
            switch (characterRace)
            {
                case CharacterRace.Any:
                    return "Any";
                case CharacterRace.Hyur:
                    return "Hyur"; 
                case CharacterRace.Elezen:
                    return "Elezen"; 
                case CharacterRace.Lalafell:
                    return "Lalafell"; 
                case CharacterRace.Miqote:
                    return "Miqote"; 
                case CharacterRace.Roegadyn:
                    return "Roegadyn"; 
                case CharacterRace.Viera:
                    return "Viera"; 
                case CharacterRace.AuRa:
                    return "Au Ra"; 
                case CharacterRace.None:
                    return "None"; 
            }

            return "N/A";
        }
        public static InventoryCategory ToInventoryCategory(this InventoryType type)
        {
            switch (type)
            {
                case InventoryType.Armoire:
                    return InventoryCategory.Armoire;
                case InventoryType.Bag0 :
                    return InventoryCategory.CharacterBags;
                case InventoryType.Bag1 :
                    return InventoryCategory.CharacterBags;
                case InventoryType.Bag2 :
                    return InventoryCategory.CharacterBags;
                case InventoryType.Bag3 :
                    return InventoryCategory.CharacterBags;
                case InventoryType.ArmoryBody :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryEar :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryFeet :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryHand :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryHead :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryLegs :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryMain :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryNeck :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryOff :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryRing :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryWaist :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmoryWrist :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.ArmorySoulCrystal :
                    return InventoryCategory.CharacterArmoryChest;
                case InventoryType.RetainerMarket :
                    return InventoryCategory.RetainerMarket;
                case InventoryType.RetainerEquippedGear :
                    return InventoryCategory.RetainerEquipped;
                case InventoryType.GlamourChest :
                    return InventoryCategory.GlamourChest;
                case InventoryType.RetainerBag0 :
                    return InventoryCategory.RetainerBags;
                case InventoryType.RetainerBag1 :
                    return InventoryCategory.RetainerBags;
                case InventoryType.RetainerBag2 :
                    return InventoryCategory.RetainerBags;
                case InventoryType.RetainerBag3 :
                    return InventoryCategory.RetainerBags;
                case InventoryType.RetainerBag4 :
                    return InventoryCategory.RetainerBags;
                case InventoryType.RetainerBag5 :
                    return InventoryCategory.RetainerBags;
                case InventoryType.SaddleBag0 :
                    return InventoryCategory.CharacterSaddleBags;
                case InventoryType.SaddleBag1 :
                    return InventoryCategory.CharacterSaddleBags;
                case InventoryType.PremiumSaddleBag0 :
                    return InventoryCategory.CharacterPremiumSaddleBags;
                case InventoryType.PremiumSaddleBag1 :
                    return InventoryCategory.CharacterPremiumSaddleBags;
                case InventoryType.FreeCompanyBag0 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag1 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag2 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag3 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag4 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag5 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag6 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag7 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag8 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag9 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyBag10 :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.RetainerGil :
                    return InventoryCategory.Currency;
                case InventoryType.Currency :
                    return InventoryCategory.Currency;
                case InventoryType.FreeCompanyGil :
                    return InventoryCategory.Currency;
                case InventoryType.Crystal :
                    return InventoryCategory.Crystals;
                case InventoryType.RetainerCrystal :
                    return InventoryCategory.Crystals;
            }
            return InventoryCategory.Other;
        }
        public static string FormattedName(this InventoryCategory category)
        {
            switch (category)
            {
                case InventoryCategory.CharacterBags:
                    return "Bags";
                case InventoryCategory.CharacterSaddleBags:
                    return "Saddle Bags";
                case InventoryCategory.CharacterPremiumSaddleBags:
                    return "Premium Saddle Bags";
                case InventoryCategory.FreeCompanyBags:
                    return "Free Company Bags";
                case InventoryCategory.CharacterArmoryChest:
                    return "Armoury Chest";
                case InventoryCategory.GlamourChest:
                    return "Glamour Chest";
                case InventoryCategory.CharacterEquipped:
                    return "Equipped";
                case InventoryCategory.Armoire:
                    return "Armoire";
                case InventoryCategory.RetainerBags:
                    return "Bags";
                case InventoryCategory.RetainerMarket:
                    return "Market";
                case InventoryCategory.Currency:
                    return "Currency";
                case InventoryCategory.Crystals:
                    return "Crystals";
            }

            return category.ToString();
        }
        public static string FormattedName(this InventoryCategory? category)
        {
            if (category.HasValue)
            {
                return FormattedName(category.Value);
            }

            return "Unknown";
        }
    }
}