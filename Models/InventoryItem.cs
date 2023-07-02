﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.GameStructs;
using CriticalCommonLib.Sheets;
using Dalamud.Interface.Colors;
using Lumina;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using LuminaSupplemental.Excel.Model;
using Newtonsoft.Json;

namespace CriticalCommonLib.Models
{
    public class InventoryItem : IEquatable<InventoryItem>, ICsv
    {
        public InventoryType Container;
        public short Slot;
        public uint ItemId;
        public uint Quantity;
        public ushort Spiritbond;
        public ushort Condition;
        public FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags Flags;
        public ushort Materia0;
        public ushort Materia1;
        public ushort Materia2;
        public ushort Materia3;
        public ushort Materia4;
        public byte MateriaLevel0;
        public byte MateriaLevel1;
        public byte MateriaLevel2;
        public byte MateriaLevel3;
        public byte MateriaLevel4;
        public byte Stain;
        public uint GlamourId;
        public InventoryType SortedContainer;
        public InventoryCategory SortedCategory;
        public int SortedSlotIndex;
        [JsonIgnore]
        public int GlamourIndex;
        public ulong RetainerId;
        [JsonIgnore]
        public uint TempQuantity = 0;
        public uint RetainerMarketPrice;
        //Cabinet category

        public uint[]? GearSets = Array.Empty<uint>();
        public string[]? GearSetNames = Array.Empty<string>();

        public static InventoryItem FromGlamourItem(GlamourItem glamourItem)
        {
            var glamourItemItemId = glamourItem.ItemId;
            var itemFlags = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None;
            if (glamourItemItemId >= 1_000_000)
            {
                glamourItemItemId -= 1_000_000;
                itemFlags = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HQ;
            }
            return new(InventoryType.GlamourChest, (short)glamourItem.Index, glamourItemItemId, 1, 0, 0,
                itemFlags, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, glamourItem.StainId, 0) ;
        }

        public static InventoryItem FromArmoireItem(uint itemId, short slotIndex)
        {
            return new (InventoryType.Armoire, slotIndex, itemId, 1, 0, 0,
                FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.None, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public static unsafe InventoryItem FromMemoryInventoryItem(FFXIVClientStructs.FFXIV.Client.Game.InventoryItem memoryInventoryItem)
        {
            return new(memoryInventoryItem.Container.Convert(), memoryInventoryItem.Slot, memoryInventoryItem.ItemID,
                memoryInventoryItem.Quantity, memoryInventoryItem.Spiritbond, memoryInventoryItem.Condition,
                memoryInventoryItem.Flags, memoryInventoryItem.Materia[0], memoryInventoryItem.Materia[1],
                memoryInventoryItem.Materia[2], memoryInventoryItem.Materia[3], memoryInventoryItem.Materia[4],
                memoryInventoryItem.MateriaGrade[0], memoryInventoryItem.MateriaGrade[1], memoryInventoryItem.MateriaGrade[2],
                memoryInventoryItem.MateriaGrade[3], memoryInventoryItem.MateriaGrade[4], memoryInventoryItem.Stain,
                memoryInventoryItem.GlamourID);
        }
        
        public static unsafe int HashCode(FFXIVClientStructs.FFXIV.Client.Game.InventoryItem memoryInventoryItem)
        {
            var hashCode = new HashCode();
            hashCode.Add((int)memoryInventoryItem.Container);
            hashCode.Add(memoryInventoryItem.Slot);
            hashCode.Add(memoryInventoryItem.ItemID);
            hashCode.Add(memoryInventoryItem.Quantity);
            hashCode.Add(memoryInventoryItem.Spiritbond);
            hashCode.Add(memoryInventoryItem.Condition);
            hashCode.Add((int)memoryInventoryItem.Flags);
            hashCode.Add(memoryInventoryItem.Materia[0]);
            hashCode.Add(memoryInventoryItem.Materia[1]);
            hashCode.Add(memoryInventoryItem.Materia[2]);
            hashCode.Add(memoryInventoryItem.Materia[3]);
            hashCode.Add(memoryInventoryItem.Materia[4]);
            hashCode.Add(memoryInventoryItem.MateriaGrade[0]);
            hashCode.Add(memoryInventoryItem.MateriaGrade[1]);
            hashCode.Add(memoryInventoryItem.MateriaGrade[2]);
            hashCode.Add(memoryInventoryItem.MateriaGrade[3]);
            hashCode.Add(memoryInventoryItem.MateriaGrade[4]);
            hashCode.Add(memoryInventoryItem.Stain);
            return hashCode.ToHashCode();
        }


        [JsonConstructor]
        public InventoryItem()
        {
            
        }

        public InventoryItem(InventoryItem inventoryItem)
        {
            Container = inventoryItem.Container;
            Slot = inventoryItem.Slot;
            ItemId = inventoryItem.ItemId;
            Quantity = inventoryItem.Quantity;
            Spiritbond = inventoryItem.Spiritbond;
            Condition = inventoryItem.Condition;
            Flags = inventoryItem.Flags;
            Materia0 = inventoryItem.Materia0;
            Materia1 = inventoryItem.Materia1;
            Materia2 = inventoryItem.Materia2;
            Materia3 = inventoryItem.Materia3;
            Materia4 = inventoryItem.Materia4;
            MateriaLevel0 = inventoryItem.MateriaLevel0;
            MateriaLevel1 = inventoryItem.MateriaLevel1;
            MateriaLevel2 = inventoryItem.MateriaLevel2;
            MateriaLevel3 = inventoryItem.MateriaLevel3;
            MateriaLevel4 = inventoryItem.MateriaLevel4;
            Stain = inventoryItem.Stain;
            GlamourId = inventoryItem.GlamourId;
            SortedContainer = inventoryItem.SortedContainer;
            SortedCategory = inventoryItem.SortedCategory;
            SortedSlotIndex = inventoryItem.SortedSlotIndex;
        }
        public InventoryItem(InventoryType container, short slot, uint itemId, uint quantity, ushort spiritbond, ushort condition, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags flags, ushort materia0, ushort materia1, ushort materia2, ushort materia3, ushort materia4, byte materiaLevel0, byte materiaLevel1, byte materiaLevel2, byte materiaLevel3, byte materiaLevel4, byte stain, uint glamourId)
        {
            Container = container;
            Slot = slot;
            ItemId = itemId;
            Quantity = quantity;
            Spiritbond = spiritbond;
            Condition = condition;
            Flags = flags;
            Materia0 = materia0;
            Materia1 = materia1;
            Materia2 = materia2;
            Materia3 = materia3;
            Materia4 = materia4;
            MateriaLevel0 = materiaLevel0;
            MateriaLevel1 = materiaLevel1;
            MateriaLevel2 = materiaLevel2;
            MateriaLevel3 = materiaLevel3;
            MateriaLevel4 = materiaLevel4;
            Stain = stain;
            GlamourId = glamourId;
        }
        [JsonIgnore]
        public bool IsHQ => (Flags & FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HQ) == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.HQ;
        [JsonIgnore]
        public bool IsCollectible => (Flags & FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.Collectable) == FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags.Collectable;
        [JsonIgnore]
        public bool IsEmpty => ItemId == 0;
        [JsonIgnore]
        public bool InRetainer => RetainerId.ToString().StartsWith("3");

        [JsonIgnore]
        public bool IsEquippedGear => Container is InventoryType.ArmoryBody or InventoryType.ArmoryEar or InventoryType.ArmoryFeet or InventoryType.ArmoryHand or InventoryType.ArmoryHead or InventoryType.ArmoryLegs or InventoryType.ArmoryLegs or InventoryType.ArmoryMain or InventoryType.ArmoryNeck or InventoryType.ArmoryOff or InventoryType.ArmoryRing or InventoryType.ArmoryWaist or InventoryType.ArmoryWrist or InventoryType.GearSet0 or InventoryType.RetainerEquippedGear;
        
        [JsonIgnore]
        public int ActualSpiritbond => Spiritbond / 100;

        [JsonIgnore]
        public string CabinetLocation
        {
            get
            {
                if (Container != InventoryType.Armoire || _cabFailed || ItemId == 0)
                {
                    if (_cabFailed)
                    {
                        return "Cabinet Lookup Failed";
                    }
                    else if (Container != InventoryType.Armoire)
                    {
                        return "";
                    }
                    return "Unknown Cabinet";
                }

                if (_cabCat == null)
                {
                    if (!Service.ExcelCache.ItemToCabinetCategory.ContainsKey(ItemId))
                    {
                        _cabFailed = true;
                        return "Unknown Cabinet";
                    }

                    var cabinetCategoryId = Service.ExcelCache.ItemToCabinetCategory[ItemId];
                    var cabinetCategory = Service.ExcelCache.GetCabinetCategorySheet().GetRow(cabinetCategoryId);
                    if (cabinetCategory == null)
                    {
                        _cabFailed = true;
                        return "Unknown Cabinet";
                    }

                    _cabCat = cabinetCategory.Category.Row;

                    return Service.ExcelCache.GetAddonName(cabinetCategory.Category.Row);
                }
                return Service.ExcelCache.GetAddonName(_cabCat.Value);

            }
        }

        private uint? _cabCat;
        private bool _cabFailed;
        
        [JsonIgnore]
        public Vector4 ItemColour
        {
            get
            {
                if (IsHQ)
                {
                    return ImGuiColors.TankBlue;
                }
                else if (IsCollectible)
                {
                    return ImGuiColors.DalamudOrange;
                }

                return ImGuiColors.HealerGreen;
            }
        }
        [JsonIgnore]
        public string ItemDescription
        {
            get
            {
                if (IsEmpty)
                {
                    return "Empty";
                }

                var _item = Item.NameString.ToString();
                if (IsHQ)
                {
                    _item += " (HQ)";
                }
                else if (IsCollectible)
                {
                    _item += " (Collectible)";
                }
                else
                {
                    _item += " (NQ)";
                }

                if (this.SortedCategory == InventoryCategory.Currency)
                {
                    _item += " - " + SortedContainerName;
                }
                else
                {
                    _item += " - " + SortedContainerName + " - " + (SortedSlotIndex + 1);
                }


                return _item;
            }
        }
        [JsonIgnore]
        public uint RemainingStack
        {
            get
            {
                return Item.StackSize - Quantity;
            }
        }
        [JsonIgnore]
        public uint RemainingTempStack
        {
            get
            {
                return Item.StackSize - TempQuantity;
            }
        }
        [JsonIgnore]
        public bool FullStack
        {
            get
            {
                return (Quantity == Item.StackSize);
            }
        }
        [JsonIgnore]
        public bool CanBeTraded
        {
            get
            {
                return !Item.IsUntradable && Item.ItemSearchCategory.Row != 0 && (Spiritbond * 100) == 0;
            }
        }
        
        [JsonIgnore]
        public string FormattedBagLocation
        {
            get
            {
                if (SortedContainer is InventoryType.GlamourChest or InventoryType.Currency or InventoryType.RetainerGil or InventoryType.FreeCompanyGil or InventoryType.Crystal or InventoryType.RetainerCrystal)
                {
                    return SortedContainerName;
                }
                return SortedContainerName + " - " + (SortedSlotIndex + 1);
            }
        }

        
        public static Dictionary<(InventoryType, int), Vector2> SlotIndexCache => _slotIndexCache ??= new Dictionary<(InventoryType, int), Vector2>();

        private static Dictionary<(InventoryType, int), Vector2>? _slotIndexCache;



        public Vector2 BagLocation(InventoryType bagType)
        {
            if (!SlotIndexCache.ContainsKey((bagType, SortedSlotIndex)))
            {
                if (bagType is InventoryType.Bag0 or InventoryType.Bag1 or InventoryType.Bag2 or InventoryType.Bag3
                    or InventoryType.RetainerBag0 or InventoryType.RetainerBag1 or InventoryType.RetainerBag2
                    or InventoryType.RetainerBag3 or InventoryType.RetainerBag4 or InventoryType.SaddleBag0
                    or InventoryType.SaddleBag1 or InventoryType.PremiumSaddleBag0 or InventoryType.PremiumSaddleBag1)
                {
                    var x = SortedSlotIndex % 5;
                    var y = SortedSlotIndex / 5;
                    SlotIndexCache[(bagType, SortedSlotIndex)] = new Vector2(x, y);

                }

                else if (bagType is InventoryType.ArmoryBody or InventoryType.ArmoryEar or InventoryType.ArmoryFeet
                         or InventoryType.ArmoryHand or InventoryType.ArmoryHead or InventoryType.ArmoryLegs
                         or InventoryType.ArmoryMain or InventoryType.ArmoryNeck or InventoryType.ArmoryOff
                         or InventoryType.ArmoryRing or InventoryType.ArmoryWrist or InventoryType.ArmorySoulCrystal
                         or InventoryType.FreeCompanyBag0 or InventoryType.FreeCompanyBag1 or InventoryType.FreeCompanyBag2
                         or InventoryType.FreeCompanyBag3 or InventoryType.FreeCompanyBag4)
                {
                    var x = SortedSlotIndex;
                    SlotIndexCache[(bagType, SortedSlotIndex)] = new Vector2(x, 0);

                }
                else if (bagType is InventoryType.GlamourChest)
                {
                    var x = GlamourIndex % 10;
                    var y = GlamourIndex / 10;
                    SlotIndexCache[(bagType, SortedSlotIndex)] = new Vector2(x, y);
                }
                else
                {
                    SlotIndexCache[(bagType, SortedSlotIndex)] = Vector2.Zero;
                }
            }

            return SlotIndexCache[(bagType, SortedSlotIndex)];
        }
        
        [JsonIgnore]
        public string FormattedType
        {
            get
            {
                return this.IsCollectible ? "Collectible" : (IsHQ ? "HQ" : "NQ");
            }
        }
        
        [JsonIgnore]
        public string FormattedName
        {
            get
            {
                if (ItemId == 0)
                {
                    return "Empty Slot";
                }
                return Item.NameString;
            }
        }
        
        [JsonIgnore]
        public string FormattedUiCategory
        {
            get
            {
                return ItemUICategory == null ? "" : ItemUICategory.Name.ToString().Replace("\u0002\u001F\u0001\u0003", "-");
            }
        }
        
        [JsonIgnore]
        public string FormattedSearchCategory
        {
            get
            {
                return ItemSearchCategory == null ? "" : ItemSearchCategory.Name.ToString().Replace("\u0002\u001F\u0001\u0003", "-");
            }
        }

        [JsonIgnore]
        public uint SellToVendorPrice
        {
            get
            {
                return IsHQ ? Item.PriceLow + 1 : Item.PriceLow;
            }
        }

        [JsonIgnore]
        public uint BuyFromVendorPrice
        {
            get
            {
                return IsHQ ? Item.PriceMid + 1 : Item.PriceMid;
            }
        }

        [JsonIgnore]
        public bool IsItemAvailableAtTimedNode
        {
            get
            {
                return Service.ExcelCache.IsItemAvailableAtTimedNode(Item.RowId);
            }
        }
        
        [JsonIgnore]
        public string SortedContainerName
        {
            get
            {
                if(SortedContainer is InventoryType.Bag0 or InventoryType.RetainerBag0)
                {
                    return "Bag 1";
                }
                if(SortedContainer is InventoryType.Bag1 or InventoryType.RetainerBag1)
                {
                    return "Bag 2";
                }
                if(SortedContainer is InventoryType.Bag2 or InventoryType.RetainerBag2)
                {
                    return "Bag 3";
                }
                if(SortedContainer is InventoryType.Bag3 or InventoryType.RetainerBag3)
                {
                    return "Bag 4";
                }
                if(SortedContainer is InventoryType.RetainerBag4)
                {
                    return "Bag 5";
                }
                if(SortedContainer is InventoryType.SaddleBag0)
                {
                    return "Saddlebag Left";
                }
                if(SortedContainer is InventoryType.SaddleBag1)
                {
                    return "Saddlebag Right";
                }
                if(SortedContainer is InventoryType.PremiumSaddleBag0)
                {
                    return "Premium Saddlebag Left";
                }
                if(SortedContainer is InventoryType.PremiumSaddleBag1)
                {
                    return "Premium Saddlebag Right";
                }
                if(SortedContainer is InventoryType.ArmoryBody)
                {
                    return "Armory - Body";
                }
                if(SortedContainer is InventoryType.ArmoryEar)
                {
                    return "Armory - Ear";
                }
                if(SortedContainer is InventoryType.ArmoryFeet)
                {
                    return "Armory - Feet";
                }
                if(SortedContainer is InventoryType.ArmoryHand)
                {
                    return "Armory - Hand";
                }
                if(SortedContainer is InventoryType.ArmoryHead)
                {
                    return "Armory - Head";
                }
                if(SortedContainer is InventoryType.ArmoryLegs)
                {
                    return "Armory - Legs";
                }
                if(SortedContainer is InventoryType.ArmoryMain)
                {
                    return "Armory - Main";
                }
                if(SortedContainer is InventoryType.ArmoryNeck)
                {
                    return "Armory - Neck";
                }
                if(SortedContainer is InventoryType.ArmoryOff)
                {
                    return "Armory - Offhand";
                }
                if(SortedContainer is InventoryType.ArmoryRing)
                {
                    return "Armory - Ring";
                }
                if(SortedContainer is InventoryType.ArmoryWaist)
                {
                    return "Armory - Waist";
                }
                if(SortedContainer is InventoryType.ArmoryWrist)
                {
                    return "Armory - Wrist";
                }
                if(SortedContainer is InventoryType.ArmorySoulCrystal)
                {
                    return "Armory - Soul Crystal";
                }
                if(SortedContainer is InventoryType.GearSet0)
                {
                    return "Equipped Gear";
                }
                if(SortedContainer is InventoryType.RetainerEquippedGear)
                {
                    return "Equipped Gear";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag0)
                {
                    return "Free Company Chest - 1";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag1)
                {
                    return "Free Company Chest - 2";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag2)
                {
                    return "Free Company Chest - 3";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag3)
                {
                    return "Free Company Chest - 4";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag4)
                {
                    return "Free Company Chest - 5";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag5)
                {
                    return "Free Company Chest - 6";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag6)
                {
                    return "Free Company Chest - 7";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag7)
                {
                    return "Free Company Chest - 8";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag8)
                {
                    return "Free Company Chest - 9";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag9)
                {
                    return "Free Company Chest - 10";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag10)
                {
                    return "Free Company Chest - 11";
                }
                if(SortedContainer is InventoryType.RetainerMarket)
                {
                    return "Market";
                }
                if(SortedContainer is InventoryType.GlamourChest)
                {
                    return "Glamour Chest";
                }
                if(SortedContainer is InventoryType.Armoire)
                {
                    return "Armoire - " + CabinetLocation;
                }
                if(SortedContainer is InventoryType.Currency)
                {
                    return "Currency";
                }
                if(SortedContainer is InventoryType.FreeCompanyGil)
                {
                    return "Free Company - Gil";
                }
                if(SortedContainer is InventoryType.RetainerGil)
                {
                    return "Currency";
                }
                if(SortedContainer is InventoryType.FreeCompanyCrystal)
                {
                    return "Free Company - Crystals";
                }
                if(SortedContainer is InventoryType.FreeCompanyCurrency)
                {
                    return "Free Company - Currency";
                }
                if(SortedContainer is InventoryType.Crystal or InventoryType.RetainerCrystal)
                {
                    return "Crystals";
                }
                if(SortedContainer is InventoryType.HousingExteriorAppearance)
                {
                    return "Housing Exterior Appearance";
                }
                if(SortedContainer is InventoryType.HousingInteriorAppearance)
                {
                    return "Housing Interior Appearance";
                }
                if(SortedContainer is InventoryType.HousingExteriorStoreroom)
                {
                    return "Housing Exterior Storeroom";
                }
                if(SortedContainer is InventoryType.HousingInteriorStoreroom1 or InventoryType.HousingInteriorStoreroom2 or InventoryType.HousingInteriorStoreroom2 or InventoryType.HousingInteriorStoreroom3 or InventoryType.HousingInteriorStoreroom4 or InventoryType.HousingInteriorStoreroom5 or InventoryType.HousingInteriorStoreroom6 or InventoryType.HousingInteriorStoreroom7 or InventoryType.HousingInteriorStoreroom8)
                {
                    return "Housing Interior Storeroom";
                }
                if(SortedContainer is InventoryType.HousingInteriorPlacedItems1 or InventoryType.HousingInteriorPlacedItems2 or InventoryType.HousingInteriorPlacedItems2 or InventoryType.HousingInteriorPlacedItems3 or InventoryType.HousingInteriorPlacedItems4 or InventoryType.HousingInteriorPlacedItems5 or InventoryType.HousingInteriorPlacedItems6 or InventoryType.HousingInteriorPlacedItems7 or InventoryType.HousingInteriorPlacedItems8)
                {
                    return "Housing Interior Placed Items";
                }
                if(SortedContainer is InventoryType.HousingExteriorPlacedItems)
                {
                    return "Housing Exterior Placed Items";
                }

                return SortedContainer.ToString();
            }
        }

        public IEnumerable<(ushort materiaId, byte level)> Materia() {
            if (Materia0 != 0) yield return (Materia0, MateriaLevel0); else yield break;
            if (Materia1 != 0) yield return (Materia1, MateriaLevel1); else yield break;
            if (Materia2 != 0) yield return (Materia2, MateriaLevel2); else yield break;
            if (Materia3 != 0) yield return (Materia3, MateriaLevel3); else yield break;
            if (Materia4 != 0) yield return (Materia4, MateriaLevel4);
        }

        [JsonIgnore] public bool InGearSet => (GearSets?.Length ?? 0) != 0;

        [JsonIgnore] 
        public ItemUICategory? ItemUICategory => Service.ExcelCache.GetItemUICategorySheet().GetRow(Item.ItemUICategory.Row);
        
        [JsonIgnore]
        public ItemSearchCategory? ItemSearchCategory => Service.ExcelCache.GetItemSearchCategorySheet().GetRow(Item.ItemSearchCategory.Row);
        
        [JsonIgnore]
        public EquipSlotCategory? EquipSlotCategory => Service.ExcelCache.GetEquipSlotCategorySheet().GetRow(Item.EquipSlotCategory.Row);
        
        [JsonIgnore]
        public ItemSortCategory? ItemSortCategory => Service.ExcelCache.GetItemSortCategorySheet().GetRow(Item.ItemSortCategory.Row);
        
        [JsonIgnore]
        public EventItem? EventItem => Service.ExcelCache.GetEventItem(this.ItemId);
        
        [JsonIgnore]
        public ItemEx Item => Service.ExcelCache.GetItemExSheet().GetRow(ItemId) ?? (Service.ExcelCache.GetItemExSheet().GetRow(1) ?? new ItemEx());

        [JsonIgnore]
        public Stain? StainEntry => Service.ExcelCache.GetStainSheet().GetRow(Stain);

        [JsonIgnore]
        public bool IsEventItem
        {
            get
            {
                return EventItem != null;
            }
        }
        [JsonIgnore]
        public ushort Icon {
            get {
                if (ItemId >= 2000000)
                {
                    return EventItem?.Icon ?? 0;
                }

                return Item.Icon;
            }
        }

        public bool Equals(InventoryItem? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ItemId == other.ItemId && Flags == other.Flags;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InventoryItem) obj);
        }

        public override int GetHashCode()
        {
            var flags = (int) Flags * 100000;
            return (int) ItemId + flags;
        }

        public int GenerateHashCode(bool ignoreFlags = false)
        {
            if (ignoreFlags)
            {
                return (int)ItemId;
            }

            return GetHashCode();
        }

        /// <summary>
        /// Determines of the two instances of InventoryItem are functionally the same
        /// </summary>
        /// <param name="otherItem"></param>
        /// <returns></returns>
        public bool IsSame(InventoryItem otherItem)
        {
            if (SortedContainer != otherItem.SortedContainer)
            {
                return false;
            }

            if (SortedSlotIndex != otherItem.SortedSlotIndex)
            {
                return false;
            }

            if (ItemId != otherItem.ItemId)
            {
                return false;
            }

            if (Quantity != otherItem.Quantity)
            {
                return false;
            }

            if (Spiritbond != otherItem.Spiritbond)
            {
                return false;
            }

            if ((ItemId != 0 || otherItem.ItemId != 0) && Condition != otherItem.Condition)
            {
                return false;
            }

            if (Flags != otherItem.Flags)
            {
                return false;
            }

            if (Materia0 != otherItem.Materia0)
            {
                return false;
            }

            if (Materia1 != otherItem.Materia1)
            {
                return false;
            }

            if (Materia2 != otherItem.Materia2)
            {
                return false;
            }

            if (Materia3 != otherItem.Materia3)
            {
                return false;
            }

            if (Materia4 != otherItem.Materia4)
            {
                return false;
            }

            if (MateriaLevel0 != otherItem.MateriaLevel0)
            {
                return false;
            }

            if (MateriaLevel1 != otherItem.MateriaLevel1)
            {
                return false;
            }

            if (MateriaLevel2 != otherItem.MateriaLevel2)
            {
                return false;
            }

            if (MateriaLevel3 != otherItem.MateriaLevel3)
            {
                return false;
            }

            if (MateriaLevel4 != otherItem.MateriaLevel4)
            {
                return false;
            }

            if (Stain != otherItem.Stain)
            {
                return false;
            }

            if (GlamourId != otherItem.GlamourId)
            {
                return false;
            }
            
            if (SortedContainer == InventoryType.RetainerMarket && RetainerMarketPrice != otherItem.RetainerMarketPrice)
            {
                return false;
            }

            if (GearSets != null && otherItem.GearSets != null && (GearSets.Length != 0 || otherItem.GearSets.Length != 0) && !GearSets.OrderBy(x => x).SequenceEqual(otherItem.GearSets.OrderBy(x => x)))
            {
                return false;
            }

            return true;
        }
        
        
        /// <summary>
        /// Determines of the two instances of InventoryItem are functionally the same, without comparing their locations
        /// </summary>
        /// <param name="otherItem"></param>
        /// <returns></returns>
        public InventoryChangeReason? IsSameItem(InventoryItem otherItem)
        {
            if (ItemId != otherItem.ItemId)
            {
                return InventoryChangeReason.ItemIdChanged;
            }

            if (Quantity != otherItem.Quantity)
            {
                return InventoryChangeReason.QuantityChanged;
            }

            if (Spiritbond != otherItem.Spiritbond)
            {
                return InventoryChangeReason.SpiritbondChanged;
            }

            if ((ItemId != 0 || otherItem.ItemId != 0) && Condition != otherItem.Condition)
            {
                return InventoryChangeReason.ConditionChanged;
            }

            if (Flags != otherItem.Flags)
            {
                return InventoryChangeReason.FlagsChanged;
            }

            if (Materia0 != otherItem.Materia0)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (Materia1 != otherItem.Materia1)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (Materia2 != otherItem.Materia2)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (Materia3 != otherItem.Materia3)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (Materia4 != otherItem.Materia4)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (MateriaLevel0 != otherItem.MateriaLevel0)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (MateriaLevel1 != otherItem.MateriaLevel1)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (MateriaLevel2 != otherItem.MateriaLevel2)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (MateriaLevel3 != otherItem.MateriaLevel3)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (MateriaLevel4 != otherItem.MateriaLevel4)
            {
                return InventoryChangeReason.MateriaChanged;
            }

            if (Stain != otherItem.Stain)
            {
                return InventoryChangeReason.StainChanged;
            }

            if (GlamourId != otherItem.GlamourId)
            {
                return InventoryChangeReason.GlamourChanged;
            }

            if (SortedContainer == InventoryType.RetainerMarket && RetainerMarketPrice != otherItem.RetainerMarketPrice)
            {
                return InventoryChangeReason.MarketPriceChanged;
            }

            if (GearSets != null && otherItem.GearSets != null && (GearSets.Length != 0 || otherItem.GearSets.Length != 0) && GearSets.Length == otherItem.GearSets.Length && !GearSets.OrderBy(x => x).SequenceEqual(otherItem.GearSets.OrderBy(x => x)))
            {
                return InventoryChangeReason.GearsetsChanged;
            }

            return null;
        }


        public string DebugName => Item.NameString + " in bag " + FormattedBagLocation + " in retainer " + RetainerId + " with quantity " + Quantity;


        /// <summary>
        /// Determines of the two instances of InventoryItem are in the same position
        /// </summary>
        /// <param name="otherItem"></param>
        /// <returns></returns>
        public bool IsSamePosition(InventoryItem otherItem)
        {
            if (SortedContainer != otherItem.SortedContainer)
            {
                return false;
            }

            if (SortedSlotIndex != otherItem.SortedSlotIndex)
            {
                return false;
            }

            return true;
        }

        public void FromCsv(string[] csvData)
        {
            if (Enum.TryParse<InventoryType>(csvData[0], out var container))
            {
                Container = container;
            }
            if(short.TryParse(csvData[1], out var slot))
            {
                Slot = slot;
            }
            if(uint.TryParse(csvData[2], out var itemId))
            {
                ItemId = itemId;
            }
            if(uint.TryParse(csvData[3], out var quantity))
            {
                Quantity = quantity;
            }
            if(ushort.TryParse(csvData[4], out var spiritbond))
            {
                Spiritbond = spiritbond;
            }
            if(ushort.TryParse(csvData[5], out var condition))
            {
                Condition = condition;
            }
            if (Enum.TryParse<FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags>(csvData[6], out var flags))
            {
                Flags = flags;
            }
            if(ushort.TryParse(csvData[7], out var materia0))
            {
                Materia0 = materia0;
            }
            if(ushort.TryParse(csvData[8], out var materia1))
            {
                Materia1 = materia1;
            }
            if(ushort.TryParse(csvData[9], out var materia2))
            {
                Materia2 = materia2;
            }
            if(ushort.TryParse(csvData[10], out var materia3))
            {
                Materia3 = materia3;
            }
            if(ushort.TryParse(csvData[11], out var materia4))
            {
                Materia4 = materia4;
            }
            if(byte.TryParse(csvData[12], out var materiaLevel0))
            {
                MateriaLevel0 = materiaLevel0;
            }
            if(byte.TryParse(csvData[13], out var materiaLevel1))
            {
                MateriaLevel1 = materiaLevel1;
            }
            if(byte.TryParse(csvData[14], out var materiaLevel2))
            {
                MateriaLevel2 = materiaLevel2;
            }
            if(byte.TryParse(csvData[15], out var materiaLevel3))
            {
                MateriaLevel3 = materiaLevel3;
            }
            if(byte.TryParse(csvData[16], out var materiaLevel4))
            {
                MateriaLevel4 = materiaLevel4;
            }
            if(byte.TryParse(csvData[17], out var stain))
            {
                Stain = stain;
            }
            if(uint.TryParse(csvData[18], out var glamourId))
            {
                GlamourId = glamourId;
            }
            if (Enum.TryParse<InventoryType>(csvData[19], out var inventoryType))
            {
                SortedContainer = inventoryType;
            }
            if (Enum.TryParse<InventoryCategory>(csvData[20], out var inventoryCategory))
            {
                SortedCategory = inventoryCategory;
            }
            if(int.TryParse(csvData[21], out var sortedSlotIndex))
            {
                SortedSlotIndex = sortedSlotIndex;
            }
            if(ulong.TryParse(csvData[22], out var retainerId))
            {
                RetainerId = retainerId;
            }
            if(uint.TryParse(csvData[23], out var retainerMarketPrice))
            {
                RetainerMarketPrice = retainerMarketPrice;
            }

            var gearSets = csvData[24].Split(";");
            GearSets = new uint[gearSets.Length];
            for (var index = 0; index < gearSets.Length; index++)
            {
                var gearSet = gearSets[index];
                if (uint.TryParse(gearSet, out var parsedGearSetId))
                {
                    GearSets[index] = parsedGearSetId;
                }
            }

            var gearSetNames = csvData[25].Split(";");
            GearSetNames = gearSetNames;
        }

        public string[] ToCsv()
        {
            List<string> csvData = new List<string>();
            csvData.Add(((int)this.Container).ToString());
            csvData.Add(Slot.ToString());
            csvData.Add(ItemId.ToString());
            csvData.Add(Quantity.ToString());
            csvData.Add(Spiritbond.ToString());
            csvData.Add(Condition.ToString());
            csvData.Add(((int)Flags).ToString());
            csvData.Add(Materia0.ToString());
            csvData.Add(Materia1.ToString());
            csvData.Add(Materia2.ToString());
            csvData.Add(Materia3.ToString());
            csvData.Add(Materia4.ToString());
            csvData.Add(MateriaLevel0.ToString());
            csvData.Add(MateriaLevel1.ToString());
            csvData.Add(MateriaLevel2.ToString());
            csvData.Add(MateriaLevel3.ToString());
            csvData.Add(MateriaLevel4.ToString());
            csvData.Add(Stain.ToString());
            csvData.Add(GlamourId.ToString());
            csvData.Add(((int)SortedContainer).ToString());
            csvData.Add(((int)SortedCategory).ToString());
            csvData.Add(SortedSlotIndex.ToString());
            csvData.Add(RetainerId.ToString());
            csvData.Add(RetainerMarketPrice.ToString());
            if (GearSets == null)
            {
                csvData.Add("");
            }
            else
            {
                csvData.Add(String.Join(";", GearSets.Select(c => c.ToString())));
            }
            if (GearSetNames == null)
            {
                csvData.Add("");
            }
            else
            {
                csvData.Add(String.Join(";", GearSetNames.Select(c => c.ToString())));
            }

            return csvData.ToArray();
        }

        public bool IncludeInCsv()
        {
            return ItemId != 0;
        }

        public virtual void PopulateData(GameData gameData, Language language)
        {
        }
    }
}