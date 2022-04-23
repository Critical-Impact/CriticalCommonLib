using System;
using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Services;
using Dalamud.Interface.Colors;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CriticalCommonLib.Models
{
    public class InventoryItem : IEquatable<InventoryItem>
    {
        public InventoryType Container;
        public short Slot;
        public uint ItemId;
        public uint Quantity;
        public ushort Spiritbond;
        public ushort Condition;
        public ItemFlags Flags;
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
        public ulong RetainerId;
        public uint TempQuantity;
        public uint RetainerMarketPrice;

        public static unsafe InventoryItem FromMemoryInventoryItem(MemoryInventoryItem memoryInventoryItem)
        {
            return new(memoryInventoryItem.Container, memoryInventoryItem.Slot, memoryInventoryItem.ItemId,
                memoryInventoryItem.Quantity, memoryInventoryItem.Spiritbond, memoryInventoryItem.Condition,
                memoryInventoryItem.Flags, memoryInventoryItem.Materia0, memoryInventoryItem.Materia1,
                memoryInventoryItem.Materia2, memoryInventoryItem.Materia3, memoryInventoryItem.Materia4,
                memoryInventoryItem.MateriaLevel0, memoryInventoryItem.MateriaLevel1, memoryInventoryItem.MateriaLevel2,
                memoryInventoryItem.MateriaLevel3, memoryInventoryItem.MateriaLevel4, memoryInventoryItem.Stain,
                memoryInventoryItem.GlamourId);
        }

        public static unsafe InventoryItem FromNetworkItemInfo(ItemInfo itemInfo)
        {
            return new((InventoryType) itemInfo.containerId, (short)itemInfo.slot, itemInfo.catalogId,
                itemInfo.quantity, itemInfo.spiritBond, itemInfo.condition, (ItemFlags)itemInfo.hqFlag,
                itemInfo.materia1, itemInfo.materia2, itemInfo.materia3, itemInfo.materia4, itemInfo.materia5,
                itemInfo.buffer1, itemInfo.buffer2, itemInfo.buffer3, itemInfo.buffer4, itemInfo.buffer5, (byte)itemInfo.stain
                , itemInfo.glamourCatalogId);
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
        public InventoryItem(InventoryType container, short slot, uint itemId, uint quantity, ushort spiritbond, ushort condition, ItemFlags flags, ushort materia0, ushort materia1, ushort materia2, ushort materia3, ushort materia4, byte materiaLevel0, byte materiaLevel1, byte materiaLevel2, byte materiaLevel3, byte materiaLevel4, byte stain, uint glamourId)
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
        public bool IsHQ => (Flags & ItemFlags.HQ) == ItemFlags.HQ;
        [JsonIgnore]
        public bool IsCollectible => (Flags & ItemFlags.Collectible) == ItemFlags.Collectible;
        [JsonIgnore]
        public bool IsEmpty => ItemId == 0;
        [JsonIgnore]
        public bool InRetainer => RetainerId.ToString().StartsWith("3");

        public bool IsEquippedGear => Container is InventoryType.ArmoryBody or InventoryType.ArmoryEar or InventoryType.ArmoryFeet or InventoryType.ArmoryHand or InventoryType.ArmoryHead or InventoryType.ArmoryLegs or InventoryType.ArmoryLegs or InventoryType.ArmoryMain or InventoryType.ArmoryNeck or InventoryType.ArmoryOff or InventoryType.ArmoryRing or InventoryType.ArmoryWaist or InventoryType.ArmoryWrist or InventoryType.GearSet0 or InventoryType.RetainerEquippedGear;

        public int ActualSpiritbond => Spiritbond / 100;
        
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

                var _item = Item == null ? "Unknown" : Item.Name;
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

                _item += " - " + SortedContainerName + " - " + (SortedSlotIndex + 1);

                return _item;
            }
        }
        [JsonIgnore]
        public uint RemainingStack
        {
            get
            {
                return Item == null ? 0 : Item.StackSize - Quantity;
            }
        }
        [JsonIgnore]
        public uint RemainingTempStack
        {
            get
            {
                return Item == null ? 0 : Item.StackSize - TempQuantity;
            }
        }
        [JsonIgnore]
        public bool FullStack
        {
            get
            {
                return Item == null ? false : (Quantity == Item.StackSize);
            }
        }
        [JsonIgnore]
        public bool CanBeTraded
        {
            get
            {
                return Item == null ? false : !Item.IsUntradable && Item.ItemSearchCategory.Row != 0 && (Spiritbond * 100) == 0;
            }
        }
        
        [JsonIgnore]
        public string FormattedBagLocation
        {
            get
            {
                return SortedContainerName + " - " + (SortedSlotIndex + 1);
            }
        }

        public Vector2 BagLocation(InventoryType bagType)
        {
            if (bagType is InventoryType.Bag0 or InventoryType.Bag1 or InventoryType.Bag2 or InventoryType.Bag3 or InventoryType.RetainerBag0 or InventoryType.RetainerBag1 or InventoryType.RetainerBag2 or InventoryType.RetainerBag3 or InventoryType.RetainerBag4 or InventoryType.SaddleBag0 or InventoryType.SaddleBag1 or InventoryType.PremiumSaddleBag0 or InventoryType.PremiumSaddleBag1)
            {
                var x = SortedSlotIndex % 5;
                var y = SortedSlotIndex / 5;
                return new Vector2(x, y);
            }
            if (bagType is InventoryType.ArmoryBody or InventoryType.ArmoryEar or InventoryType.ArmoryFeet or InventoryType.ArmoryHand or InventoryType.ArmoryHead or InventoryType.ArmoryLegs or InventoryType.ArmoryMain or InventoryType.ArmoryNeck or InventoryType.ArmoryOff or InventoryType.ArmoryRing  or InventoryType.ArmoryWrist or InventoryType.ArmorySoulCrystal or InventoryType.FreeCompanyBag0 or InventoryType.FreeCompanyBag1 or InventoryType.FreeCompanyBag2 or InventoryType.FreeCompanyBag3 or InventoryType.FreeCompanyBag4)
            {
                var x = SortedSlotIndex;
                return new Vector2(x, 0);
            }

            return Vector2.Zero;
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
                return Item == null ? "Unknown" : Item.Name;
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
        public bool CanBeBought
        {
            get
            {
                if (Item == null)
                {
                    return false;
                }
                return ExcelCache.IsItemGilShopBuyable(Item.RowId);
            }
        }

        [JsonIgnore]
        public uint SellToVendorPrice
        {
            get
            {
                if (Item == null)
                {
                    return 0;
                }
                return IsHQ ? Item.PriceLow + 1 : Item.PriceLow;
            }
        }

        [JsonIgnore]
        public uint BuyFromVendorPrice
        {
            get
            {
                if (Item == null)
                {
                    return 0;
                }
                return IsHQ ? Item.PriceMid + 1 : Item.PriceMid;
            }
        }

        [JsonIgnore]
        public bool IsItemAvailableAtTimedNode
        {
            get
            {
                if (Item == null)
                {
                    return false;
                }
                return ExcelCache.IsItemAvailableAtTimedNode(Item.RowId);
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
                    return "Free Company Bag - 1";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag1)
                {
                    return "Free Company Bag - 2";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag2)
                {
                    return "Free Company Bag - 3";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag3)
                {
                    return "Free Company Bag - 4";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag4)
                {
                    return "Free Company Bag - 5";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag5)
                {
                    return "Free Company Bag - 6";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag6)
                {
                    return "Free Company Bag - 7";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag7)
                {
                    return "Free Company Bag - 8";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag8)
                {
                    return "Free Company Bag - 9";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag9)
                {
                    return "Free Company Bag - 10";
                }
                if(SortedContainer is InventoryType.FreeCompanyBag10)
                {
                    return "Free Company Bag - 11";
                }
                if(SortedContainer is InventoryType.RetainerMarket)
                {
                    return "Market";
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

        [JsonIgnore] 
        public ItemUICategory? ItemUICategory => Item == null ? null : ExcelCache.GetItemUICategory(Item.ItemUICategory.Row);
        
        [JsonIgnore]
        public ItemSearchCategory? ItemSearchCategory => Item == null ? null : ExcelCache.GetItemSearchCategory(Item.ItemSearchCategory.Row);
        
        [JsonIgnore]
        public EquipSlotCategory? EquipSlotCategory => Item == null ? null : ExcelCache.GetEquipSlotCategory(Item.EquipSlotCategory.Row);
        
        [JsonIgnore]
        public ItemSortCategory? ItemSortCategory => Item == null ? null : ExcelCache.GetItemSortCategory(Item.ItemSortCategory.Row);
        
        [JsonIgnore]
        public EventItem? EventItem => ExcelCache.GetEventItem(this.ItemId);
        
        [JsonIgnore]
        public Item? Item => ExcelCache.GetItem(this.ItemId);

        [JsonIgnore]
        public bool IsEventItem
        {
            get
            {
                return EventItem != null;
            }
        }
        
        public ushort Icon {
            get {
                if (ItemId >= 2000000)
                {
                    return EventItem?.Icon ?? 0;
                }

                return Item?.Icon ?? 0;
            }
        }

        public bool CanTryOn
        {
            get
            {
                if (Item == null)
                {
                    return false;
                }
                if (Item.EquipSlotCategory?.Value == null) return false;
                if (Item.EquipSlotCategory.Row > 0 && Item.EquipSlotCategory.Row != 6 && Item.EquipSlotCategory.Row != 17 && (Item.EquipSlotCategory.Value.OffHand <=0 || Item.ItemUICategory.Row == 11))
                {
                    return true;
                }

                return false;
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
    }
}