using System;
using System.Collections.Generic;
using System.Numerics;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Services;
using Dalamud.Interface.Colors;
using InventoryTools.Structs;
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
        public bool FullStack
        {
            get
            {
                return Item == null ? false : (Quantity == Item.StackSize);
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
        public ItemUICategory ItemUICategory => ExcelCache.GetItemUICategory(Item.ItemUICategory.Row);
        
        [JsonIgnore]
        public ItemSearchCategory ItemSearchCategory => ExcelCache.GetItemSearchCategory(Item.ItemSearchCategory.Row);
        
        [JsonIgnore]
        public EquipSlotCategory EquipSlotCategory => ExcelCache.GetEquipSlotCategory(Item.EquipSlotCategory.Row);
        
        [JsonIgnore]
        public ItemSortCategory ItemSortCategory => ExcelCache.GetItemSortCategory(Item.ItemSortCategory.Row);
        
        [JsonIgnore]
        public EventItem EventItem => ExcelCache.GetEventItem(this.ItemId);
        
        [JsonIgnore]
        public Item Item => ExcelCache.GetItem(this.ItemId);

        public bool Equals(InventoryItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ItemId == other.ItemId && Flags == other.Flags;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InventoryItem) obj);
        }

        public override int GetHashCode()
        {
            int hash = 269;
            hash = (hash * 47) + (int) ItemId;
            hash = (hash * 47) + (int) Flags;
            return hash;
        }
    }
}