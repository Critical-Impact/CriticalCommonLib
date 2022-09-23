using System;
using System.Collections.Generic;
using CriticalCommonLib.Models;
using CriticalCommonLib.UiModule;
using Dalamud.Logging;

namespace CriticalCommonLib.Services
{
    public class MemorySortScanner
    {
        public unsafe InventorySortOrder ParseItemOrder()
        {
            //Rework this later
            Dictionary<ulong, RetainerSortOrder> retainerInventories = new Dictionary<ulong, RetainerSortOrder>();
            Dictionary<string, List<(int slotIndex, int containerIndex)>> normalInventories = new Dictionary<string, List<(int slotIndex, int containerIndex)>>();

            List<(int slotIndex, int containerIndex)> inventories =
                new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->PlayerInventory->SlotPerContainer * 4; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->PlayerInventory->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->PlayerInventory->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("PlayerInventory", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.MainHand->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.MainHand->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.MainHand->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryMainHand", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Head->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Head->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Head->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryHead", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Body->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Body->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Body->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryBody", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Hands->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Hands->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Hands->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryHands", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Legs->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Legs->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Legs->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryLegs", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Feet->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Feet->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Feet->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryFeet", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.OffHand->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.OffHand->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.OffHand->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryOffHand", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Ears->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Ears->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Ears->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryEars", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Neck->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Neck->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Neck->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryNeck", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Wrists->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Wrists->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Wrists->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryWrists", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.Rings->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.Rings->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.Rings->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmouryRings", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->Armoury.SoulCrystal->SlotPerContainer; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->Armoury.SoulCrystal->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->Armoury.SoulCrystal->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("ArmourySoulCrystals", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->SaddleBagNormal->SlotPerContainer * 2; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->SaddleBagNormal->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->SaddleBagNormal->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("SaddleBag", inventories);

            inventories = new List<(int slotIndex, int containerIndex)>();
            for (int i = 0; i < ItemOrderModule.Instance()->SaddleBagPremium->SlotPerContainer * 2; i++)
            {
                var slotIndex = ItemOrderModule.Instance()->SaddleBagPremium->Slots[i]->SlotIndex;
                var containerIndex = ItemOrderModule.Instance()->SaddleBagPremium->Slots[i]->ContainerIndex;
                inventories.Add((slotIndex, containerIndex));
            }

            normalInventories.Add("SaddleBagPremium", inventories);

            var retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer1Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer1Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer2Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer2Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }


            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer3Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer3Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer4Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer4Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer5Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer5Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer6Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer6Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer7Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer7Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer8Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer8Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }

            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer9Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer9Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }


            retainerId = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer10Id;
            if (retainerId != 0)
            {
                inventories = new List<(int slotIndex, int containerIndex)>();
                var itemOrderContainer = ItemOrderModule.Instance()->Retainers->SortOrders->Retainer10Bag;
                for (int i = 0; i < itemOrderContainer->SlotPerContainer * 7; i++)
                {
                    var slotIndex = itemOrderContainer->Slots[i]->SlotIndex;
                    var containerIndex = itemOrderContainer->Slots[i]->ContainerIndex;
                    inventories.Add((slotIndex, containerIndex));
                }

                retainerInventories.Add(retainerId, new RetainerSortOrder(retainerId, inventories));
            }



            return new InventorySortOrder(retainerInventories, normalInventories);
        }
    }
}