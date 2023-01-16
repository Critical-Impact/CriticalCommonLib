using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Sheets;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using Dalamud.Utility;
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
        public uint CabCat
        {
            get
            {
                if (_cabCat == null)
                {
                    //TODO: Turn me into a dictionary
                    var armoireCategory = Service.ExcelCache.GetCabinetSheet().FirstOrDefault(c => c.Item.Row == ItemId);
                    if (armoireCategory == null)
                    {
                        _cabFailed = true;
                        return 0;
                    }
                    _cabCat = armoireCategory.Category.Value!.Category.Row;
                    return _cabCat.Value;
                }
                else
                {
                    return _cabCat.Value;
                }
            }
        }
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
                        return "Container Aint Armoire";
                    }
                    return "Unknown Cabinet";
                }

                if (_cabCat == null)
                {
                    //TODO: Turn me into a dictionary
                    var armoireCategory = Service.ExcelCache.GetCabinetSheet().FirstOrDefault(c => c.Item.Row == ItemId);
                    if (armoireCategory == null)
                    {
                        _cabFailed = true;
                        return "Unknown Cabinet";
                    }
                    _cabCat = armoireCategory.Category.Value!.Category.Row;
                    return Service.ExcelCache.GetAddonName(_cabCat.Value);
                }
                else
                {
                    return Service.ExcelCache.GetAddonName(_cabCat.Value);
                }

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
            if (bagType is InventoryType.GlamourChest)
            {
                var x = GlamourIndex % 10;
                var y = GlamourIndex / 10;
                return new Vector2(x, y);
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
                if(SortedContainer is InventoryType.Crystal or InventoryType.RetainerCrystal)
                {
                    return "Crystals";
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
    }
}