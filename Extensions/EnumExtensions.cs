using System;
using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

namespace CriticalCommonLib.Extensions
{
    public static class EnumExtensions
    {
        public static IEnumerable<TEnum> GetFlags<TEnum>(this TEnum enumValue)
            where TEnum : Enum
        {
            return EnumUtil.GetFlags<TEnum>().Where(ev => enumValue.HasFlag(ev));
        }
        
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

        public static List<InventoryType> GetTypes(this InventoryCategory category)
        {
            switch (category)
            {
                case InventoryCategory.CharacterBags:
                    return new List<InventoryType>()
                        {InventoryType.Bag0, InventoryType.Bag1, InventoryType.Bag2, InventoryType.Bag3};
                case InventoryCategory.RetainerBags:
                    return new List<InventoryType>()
                        {InventoryType.RetainerBag0, InventoryType.RetainerBag1, InventoryType.RetainerBag2, InventoryType.RetainerBag3, InventoryType.RetainerBag4, InventoryType.RetainerBag5, InventoryType.RetainerBag6};
                case InventoryCategory.Armoire:
                    return new List<InventoryType>()
                        {InventoryType.Armoire};
                case InventoryCategory.Crystals:
                    return new List<InventoryType>()
                        {InventoryType.Crystal,InventoryType.RetainerCrystal, InventoryType.FreeCompanyCrystal};
                case InventoryCategory.Currency:
                    return new List<InventoryType>()
                        {InventoryType.Currency,InventoryType.FreeCompanyGil, InventoryType.RetainerGil, InventoryType.FreeCompanyCurrency};
                case InventoryCategory.CharacterEquipped:
                    return new List<InventoryType>()
                        {InventoryType.GearSet0};
                case InventoryCategory.CharacterArmoryChest:
                    return new List<InventoryType>()
                        {InventoryType.ArmoryBody, InventoryType.ArmoryEar , InventoryType.ArmoryFeet , InventoryType.ArmoryHand , InventoryType.ArmoryHead , InventoryType.ArmoryLegs , InventoryType.ArmoryLegs , InventoryType.ArmoryMain , InventoryType.ArmoryNeck , InventoryType.ArmoryOff , InventoryType.ArmoryRing , InventoryType.ArmoryWaist , InventoryType.ArmoryWrist};
                case InventoryCategory.GlamourChest:
                    return new List<InventoryType>()
                        {InventoryType.GlamourChest};
                case InventoryCategory.RetainerEquipped:
                    return new List<InventoryType>()
                        {InventoryType.RetainerEquippedGear};
                case InventoryCategory.RetainerMarket:
                    return new List<InventoryType>()
                        {InventoryType.RetainerMarket};
                case InventoryCategory.CharacterSaddleBags:
                    return new List<InventoryType>()
                        {InventoryType.SaddleBag0,InventoryType.SaddleBag1};
                case InventoryCategory.CharacterPremiumSaddleBags:
                    return new List<InventoryType>()
                        {InventoryType.PremiumSaddleBag0,InventoryType.PremiumSaddleBag1};
                case InventoryCategory.FreeCompanyBags:
                    return new List<InventoryType>()
                        {InventoryType.FreeCompanyBag0,InventoryType.FreeCompanyBag1,InventoryType.FreeCompanyBag2,InventoryType.FreeCompanyBag3,InventoryType.FreeCompanyBag4,InventoryType.FreeCompanyBag5,InventoryType.FreeCompanyBag6,InventoryType.FreeCompanyBag7,InventoryType.FreeCompanyBag8,InventoryType.FreeCompanyBag9,InventoryType.FreeCompanyBag10};
                case InventoryCategory.HousingInteriorItems:
                    return new List<InventoryType>()
                        {
                            InventoryType.HousingInteriorPlacedItems1, InventoryType.HousingInteriorPlacedItems2,
                            InventoryType.HousingInteriorPlacedItems3, InventoryType.HousingInteriorPlacedItems4,
                            InventoryType.HousingInteriorPlacedItems5, InventoryType.HousingInteriorPlacedItems6,
                            InventoryType.HousingInteriorPlacedItems7, InventoryType.HousingInteriorPlacedItems8,
                        };
                case InventoryCategory.HousingInteriorStoreroom:
                    return new List<InventoryType>()
                        {
                            InventoryType.HousingInteriorStoreroom1, InventoryType.HousingInteriorStoreroom2,
                            InventoryType.HousingInteriorStoreroom3, InventoryType.HousingInteriorStoreroom4,
                            InventoryType.HousingInteriorStoreroom5, InventoryType.HousingInteriorStoreroom6,
                            InventoryType.HousingInteriorStoreroom7, InventoryType.HousingInteriorStoreroom8,
                        };
                case InventoryCategory.HousingInteriorAppearance:
                    return new List<InventoryType>()
                        {
                            InventoryType.HousingInteriorAppearance
                        };
                case InventoryCategory.HousingExteriorStoreroom:
                    return new List<InventoryType>()
                        {
                            InventoryType.HousingExteriorStoreroom
                        };
                case InventoryCategory.HousingExteriorItems:
                    return new List<InventoryType>()
                        {
                            InventoryType.HousingExteriorPlacedItems
                        };
                case InventoryCategory.HousingExteriorAppearance:
                    return new List<InventoryType>()
                        {
                            InventoryType.HousingExteriorAppearance
                        };
            }

            return new List<InventoryType>();
        }

        public static bool IsRetainerCategory(this InventoryCategory category)
        {
            return category is InventoryCategory.RetainerBags or InventoryCategory.RetainerEquipped or InventoryCategory
                .RetainerMarket or InventoryCategory.Crystals or InventoryCategory.Currency;
        }

        public static bool IsFreeCompanyCategory(this InventoryCategory category)
        {
            return category is InventoryCategory.FreeCompanyBags or InventoryCategory.Crystals or InventoryCategory.Currency;
        }

        public static bool IsHousingCategory(this InventoryCategory category)
        {
            return category is InventoryCategory.HousingExteriorAppearance or InventoryCategory.HousingExteriorItems or InventoryCategory.HousingExteriorStoreroom or InventoryCategory.HousingInteriorAppearance or InventoryCategory.HousingInteriorItems or InventoryCategory.HousingInteriorStoreroom;
        }

        public static bool IsCharacterCategory(this InventoryCategory category)
        {
            return !IsRetainerCategory(category) && !IsFreeCompanyCategory(category) && !IsHousingCategory(category) || category == InventoryCategory.Crystals || category == InventoryCategory.Currency;
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
                case InventoryType.FreeCompanyCrystal :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.HousingInteriorStoreroom1 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom2 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom3 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom4 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom5 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom6 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom7 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingInteriorStoreroom8 :
                    return InventoryCategory.HousingInteriorStoreroom;
                case InventoryType.HousingExteriorAppearance :
                    return InventoryCategory.HousingExteriorAppearance;
                case InventoryType.HousingExteriorStoreroom :
                    return InventoryCategory.HousingExteriorStoreroom;
                case InventoryType.HousingExteriorPlacedItems :
                    return InventoryCategory.HousingExteriorItems;
                case InventoryType.HousingInteriorPlacedItems1 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems2 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems3 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems4 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems5 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems6 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems7 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorPlacedItems8 :
                    return InventoryCategory.HousingInteriorItems;
                case InventoryType.HousingInteriorAppearance :
                    return InventoryCategory.HousingInteriorAppearance;
                case InventoryType.RetainerGil :
                    return InventoryCategory.Currency;
                case InventoryType.Currency :
                    return InventoryCategory.Currency;
                case InventoryType.FreeCompanyGil :
                    return InventoryCategory.FreeCompanyBags;
                case InventoryType.FreeCompanyCurrency :
                    return InventoryCategory.FreeCompanyBags;
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
                case InventoryCategory.HousingExteriorAppearance:
                    return "Housing Exterior Appearance";
                case InventoryCategory.HousingExteriorItems:
                    return "Housing Exterior Items";
                case InventoryCategory.HousingExteriorStoreroom:
                    return "Housing Exterior Storeroom";
                case InventoryCategory.HousingInteriorAppearance:
                    return "Housing Interior Appearance";
                case InventoryCategory.HousingInteriorItems:
                    return "Housing Interior Items";
                case InventoryCategory.HousingInteriorStoreroom:
                    return "Housing Interior Storeroom";
                case InventoryCategory.RetainerEquipped:
                    return "Equipped";
            }

            return category.ToString();
        }
        
        public static string FormattedDetailedName(this InventoryCategory category)
        {
            switch (category)
            {
                case InventoryCategory.CharacterBags:
                    return "Character Bags";
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
                    return "Character Equipped";
                case InventoryCategory.Armoire:
                    return "Armoire";
                case InventoryCategory.RetainerBags:
                    return "Retainer Bags";
                case InventoryCategory.RetainerMarket:
                    return "Retainer Market";
                case InventoryCategory.Currency:
                    return "Currency";
                case InventoryCategory.Crystals:
                    return "Crystals";
                case InventoryCategory.HousingExteriorAppearance:
                    return "Housing Exterior Appearance";
                case InventoryCategory.HousingExteriorItems:
                    return "Housing Exterior Items";
                case InventoryCategory.HousingExteriorStoreroom:
                    return "Housing Exterior Storeroom";
                case InventoryCategory.HousingInteriorAppearance:
                    return "Housing Interior Appearance";
                case InventoryCategory.HousingInteriorItems:
                    return "Housing Interior Items";
                case InventoryCategory.HousingInteriorStoreroom:
                    return "Housing Interior Storeroom";
                case InventoryCategory.RetainerEquipped:
                    return "Retainer Equipped";
            }

            return category.ToString();
        }
        
        public static string FormattedName(this InventoryType type)
        {
            switch (type)
            {
                case InventoryType.Bag0:
                    return "Main Bags - 1";
                case InventoryType.Bag1:
                    return "Main Bags - 2";
                case InventoryType.Bag2:
                    return "Main Bags - 3";
                case InventoryType.Bag3:
                    return "Main Bags - 4";
                case InventoryType.GearSet0:
                    return "Gearset - 1";
                case InventoryType.GearSet1:
                    return "Gearset - 2";
                case InventoryType.Currency:
                    return "Currency";
                case InventoryType.Crystal:
                    return "Crystal";
                case InventoryType.Mail:
                    return "Mail";
                case InventoryType.KeyItem:
                    return "Key Item";
                case InventoryType.HandIn:
                    return "Hand-in";
                case InventoryType.DamagedGear:
                    return "Damaged Gear";
                case InventoryType.UNKNOWN_2008:
                    break;
                case InventoryType.Examine:
                    return "Examine";
                case InventoryType.Armoire:
                    return "Armoire";
                case InventoryType.GlamourChest:
                    return "Glamour Chest";
                case InventoryType.FreeCompanyCurrency:
                    return "Free Company Currency";
                case InventoryType.ArmoryOff:
                    return "Armoury - Off Hand";
                case InventoryType.ArmoryHead:
                    return "Armoury - Head";
                case InventoryType.ArmoryBody:
                    return "Armoury - Body";
                case InventoryType.ArmoryHand:
                    return "Armoury - Hand";
                case InventoryType.ArmoryWaist:
                    return "Armoury - Waist";
                case InventoryType.ArmoryLegs:
                    return "Armoury - Legs";
                case InventoryType.ArmoryFeet:
                    return "Armoury - Feet";
                case InventoryType.ArmoryEar:
                    return "Armoury - Ears";
                case InventoryType.ArmoryNeck:
                    return "Armoury - Neck";
                case InventoryType.ArmoryWrist:
                    return "Armoury - Wrist";
                case InventoryType.ArmoryRing:
                    return "Armoury - Rings";
                case InventoryType.ArmorySoulCrystal:
                    return "Armoury - Soul Crystals";
                case InventoryType.ArmoryMain:
                    return "Armoury - Main Hand";
                case InventoryType.SaddleBag0:
                    return "Saddlebag - 1";
                case InventoryType.SaddleBag1:
                    return "Saddlebag - 2";
                case InventoryType.PremiumSaddleBag0:
                    return "Premium Saddlebag - 1";
                case InventoryType.PremiumSaddleBag1:
                    return "Premium Saddlebag - 2";
                case InventoryType.RetainerBag0:
                    return "Retainer Bag - 1";
                case InventoryType.RetainerBag1:
                    return "Retainer Bag - 2";
                case InventoryType.RetainerBag2:
                    return "Retainer Bag - 3";
                case InventoryType.RetainerBag3:
                    return "Retainer Bag - 4";
                case InventoryType.RetainerBag4:
                    return "Retainer Bag - 5";
                case InventoryType.RetainerBag5:
                    return "Retainer Bag - 6";
                case InventoryType.RetainerBag6:
                    return "Retainer Bag - 7";
                case InventoryType.RetainerEquippedGear:
                    return "Retainer Equipped Gear";
                case InventoryType.RetainerGil:
                    return "Retainer Gil";
                case InventoryType.RetainerCrystal:
                    return "Retainer Crystal";
                case InventoryType.RetainerMarket:
                    return "Retainer Market";
                case InventoryType.FreeCompanyBag0:
                    return "Free Company Bag - 1";
                case InventoryType.FreeCompanyBag1:
                    return "Free Company Bag - 2";
                case InventoryType.FreeCompanyBag2:
                    return "Free Company Bag - 3";
                case InventoryType.FreeCompanyBag3:
                    return "Free Company Bag - 4";
                case InventoryType.FreeCompanyBag4:
                    return "Free Company Bag - 5";
                case InventoryType.FreeCompanyBag5:
                    return "Free Company Bag - 6";
                case InventoryType.FreeCompanyBag6:
                    return "Free Company Bag - 7";
                case InventoryType.FreeCompanyBag7:
                    return "Free Company Bag - 8";
                case InventoryType.FreeCompanyBag8:
                    return "Free Company Bag - 9";
                case InventoryType.FreeCompanyBag9:
                    return "Free Company Bag - 10";
                case InventoryType.FreeCompanyBag10:
                    return "Free Company Bag - 11";
                case InventoryType.FreeCompanyGil:
                    return "Free Company Bag - Gil";
                case InventoryType.FreeCompanyCrystal:
                    return "Free Company Bag - Crystal";
                case InventoryType.HousingInteriorAppearance:
                    return "House - Interior Appearance";
                case InventoryType.HousingInteriorPlacedItems1:
                    return "House - Interior Placed Items - 1";
                case InventoryType.HousingInteriorPlacedItems2:
                    return "House - Interior Placed Items - 2";
                case InventoryType.HousingInteriorPlacedItems3:
                    return "House - Interior Placed Items - 3";
                case InventoryType.HousingInteriorPlacedItems4:
                    return "House - Interior Placed Items - 4";
                case InventoryType.HousingInteriorPlacedItems5:
                    return "House - Interior Placed Items - 5";
                case InventoryType.HousingInteriorPlacedItems6:
                    return "House - Interior Placed Items - 6";
                case InventoryType.HousingInteriorPlacedItems7:
                    return "House - Interior Placed Items - 7";
                case InventoryType.HousingInteriorPlacedItems8:
                    return "House - Interior Placed Items - 8";
                case InventoryType.HousingInteriorStoreroom1:
                    return "House - Interior Storeroom - 1";
                case InventoryType.HousingInteriorStoreroom2:
                    return "House - Interior Storeroom - 2";
                case InventoryType.HousingInteriorStoreroom3:
                    return "House - Interior Storeroom - 3";
                case InventoryType.HousingInteriorStoreroom4:
                    return "House - Interior Storeroom - 4";
                case InventoryType.HousingInteriorStoreroom5:
                    return "House - Interior Storeroom - 5";
                case InventoryType.HousingInteriorStoreroom6:
                    return "House - Interior Storeroom - 6";
                case InventoryType.HousingInteriorStoreroom7:
                    return "House - Interior Storeroom - 7";
                case InventoryType.HousingInteriorStoreroom8:
                    return "House - Interior Storeroom - 8";
                case InventoryType.HousingExteriorAppearance:
                    return "House - Exterior Appearance";
                case InventoryType.HousingExteriorPlacedItems:
                    return "House - Exterior Placed Items";
                case InventoryType.HousingExteriorStoreroom:
                    return "House - Exterior Storeroom";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return type.ToString();
        }
        public static string FormattedName(this InventoryCategory? category)
        {
            if (category.HasValue)
            {
                return FormattedName(category.Value);
            }

            return "Unknown";
        }
        public static string FormattedName(this InventoryChangeReason reason)
        {
            switch (reason)
            {
                case InventoryChangeReason.Added:
                    return "Added";
                case InventoryChangeReason.Removed:
                    return "Removed";
                case InventoryChangeReason.Moved:
                    return "Moved";
                case InventoryChangeReason.ConditionChanged:
                    return "Condition Changed";
                case InventoryChangeReason.FlagsChanged:
                    return "NQ/HQ Changed";
                case InventoryChangeReason.GlamourChanged:
                    return "Glamour Changed";
                case InventoryChangeReason.MateriaChanged:
                    return "Materia Changed";
                case InventoryChangeReason.QuantityChanged:
                    return "Quantity Changed";
                case InventoryChangeReason.SpiritbondChanged:
                    return "Spiritbond Changed";
                case InventoryChangeReason.StainChanged:
                    return "Dye Changed";
                case InventoryChangeReason.ItemIdChanged:
                    return "Item Changed";
                case InventoryChangeReason.Transferred:
                    return "Transferred";
                case InventoryChangeReason.MarketPriceChanged:
                    return "Market Price Changed";
                case InventoryChangeReason.GearsetsChanged:
                    return "Gearsets Changed";
            }
            return "Unknown";
        }
        public static string FormattedName(this CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Character:
                    return "Character";
                case CharacterType.Housing:
                    return "Residence";
                case CharacterType.Retainer:
                    return "Retainer";
                case CharacterType.FreeCompanyChest:
                    return "Free Company Chest";
            }
            return "Unknown";
        }
        public static bool IsApplicable(this InventoryCategory inventoryCategory, CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Character:
                    return IsCharacterCategory(inventoryCategory);
                case CharacterType.Retainer:
                    return IsRetainerCategory(inventoryCategory);
                case CharacterType.FreeCompanyChest:
                    return IsFreeCompanyCategory(inventoryCategory);
                case CharacterType.Housing:
                    return IsHousingCategory(inventoryCategory);
            }

            return true;
        }
    }
}