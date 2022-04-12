using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class SaddlebagUIAddon : UiAddon
    {
        private RadioButtonUiAddon? _radioButtonUiLeftAddon;
        private RadioButtonUiAddon? _radioButtonUiRightAddon;
        const uint LeftSaddlebagButtonId = 7;
        const uint RightSaddlebagButtonId = 8;
        private List<InventoryGridItem>? _sortedGridItemsLeft = null;
        private List<InventoryGridItem>? _sortedGridItemsRight = null;

        public SaddlebagUIAddon(AtkUnitBase* unitBase)
        {
            _unitBase = unitBase;
        }
            
        public void ClearColors()
        {
            for (var index = 0; index < InventoryItemsLeft.Count; index++)
            {
                var item = InventoryItemsLeft[index];
                item.ClearColor();
            }
            for (var index = 0; index < InventoryItemsRight.Count; index++)
            {
                var item = InventoryItemsRight[index];
                item.ClearColor();
            }

            LeftSaddlebagButton?.ClearColor();
            RightSaddlebagButton?.ClearColor();
        }

        public void SetItemLeftColor(int itemIndex, int red, int green, int blue)
        {
            if (itemIndex >= 0 && InventoryItemsLeft.Count > itemIndex)
            {
                InventoryItemsLeft[itemIndex]._resNode->AddBlue = (ushort) blue;
                InventoryItemsLeft[itemIndex]._resNode->AddRed = (ushort) red;
                InventoryItemsLeft[itemIndex]._resNode->AddGreen = (ushort) green;
            }
        }
        public void SetItemLeftColor(int itemIndex, Vector4 color)
        {
            if (itemIndex >= 0 && InventoryItemsLeft.Count > itemIndex)
            {
                InventoryItemsLeft[itemIndex].SetColor(color);
            }
        }
            
        public void SetItemLeftColors(HashSet<int> itemIndexes, Vector4 color, bool invert = false)
        {
            if (invert)
            {
                for (var index = 0; index < InventoryItemsLeft.Count; index++)
                {
                    if (!itemIndexes.Contains(index))
                    {
                        SetItemLeftColor(index, color);
                    }
                }
            }
            else
            {
                foreach (var itemIndex in itemIndexes)
                {
                    SetItemLeftColor(itemIndex, color);
                }
            }
        }
            
        public void SetItemRightColors(HashSet<int> itemIndexes, Vector4 color, bool invert = false)
        {
            if (invert)
            {
                for (var index = 0; index < InventoryItemsRight.Count; index++)
                {
                    if (!itemIndexes.Contains(index))
                    {
                        SetItemRightColor(index, color);
                    }
                }
            }
            else
            {
                foreach (var itemIndex in itemIndexes)
                {
                    SetItemRightColor(itemIndex, color);
                }
            }
        }

        public void SetItemRightColor(int itemIndex, int red, int green, int blue)
        {
            if (itemIndex >= 0 && InventoryItemsRight.Count > itemIndex)
            {
                InventoryItemsRight[itemIndex]._resNode->AddBlue = (ushort) blue;
                InventoryItemsRight[itemIndex]._resNode->AddRed = (ushort) red;
                InventoryItemsRight[itemIndex]._resNode->AddGreen = (ushort) green;
            }
        }
            
        public void SetItemRightColor(int itemIndex, Vector4 color)
        {
            if (itemIndex >= 0 && InventoryItemsRight.Count > itemIndex)
            {
                InventoryItemsRight[itemIndex].SetColor(color);
            }
        }

        public void SetLeftTabColor(int red, int green, int blue)
        {
            LeftSaddlebagButton?.SetColor(red, green, blue);
        }

        public void SetRightTabColor(int red, int green, int blue)
        {
            RightSaddlebagButton?.SetColor(red, green, blue);
        }

        public void SetLeftTabColor(Vector4 color)
        {
            LeftSaddlebagButton?.SetColor(color);
        }

        public void SetRightTabColor(Vector4 color)
        {
            RightSaddlebagButton?.SetColor(color);
        }
            
        public void SetTabColors(HashSet<int> tabIndexes, Vector4 color, bool invert = false)
        {
            if (invert)
            {
                if (!tabIndexes.Contains(0))
                {
                    SetLeftTabColor(color);
                }
                if (!tabIndexes.Contains(1))
                {
                    SetRightTabColor(color);
                }
            }
            else
            {
                if (tabIndexes.Contains(0))
                {
                    SetLeftTabColor(color);
                }
                if (tabIndexes.Contains(1))
                {
                    SetRightTabColor(color);
                }
            }
        }

        public List<InventoryGridItem> InventoryItemsLeft
        {
            get
            {
                if (_sortedGridItemsLeft == null)
                {
                    var nodes = GetNodesByComponentType(ComponentType.DragDrop);
                    var inventoryGridItems = new List<InventoryGridItem>();
                    for (var index = 0; index < nodes.Length; index++)
                    {
                        var node = nodes[index];
                        inventoryGridItems.Add(new InventoryGridItem(node));
                    }
                    var sortedList = inventoryGridItems.OrderBy(c => c._resNode->Y + c._resNode->ParentNode->Y).ThenBy(c => c._resNode->X + c._resNode->ParentNode->X).ToList();
                    var finalList = new List<InventoryGridItem>();
                    for (var i = 0; i < sortedList.Count; i++)
                    {
                        var item = sortedList[i];
                        if ((i / 5) % 2 == 0)
                        {
                            finalList.Add(item);
                        }
                    }

                    _sortedGridItemsLeft = finalList;
                }

                return _sortedGridItemsLeft;
            }
        }

        public List<InventoryGridItem> InventoryItemsRight
        {
            get
            {
                if (_sortedGridItemsRight == null)
                {
                    var nodes = GetNodesByComponentType(ComponentType.DragDrop);
                    var inventoryGridItems = new List<InventoryGridItem>();
                    for (var index = 0; index < nodes.Length; index++)
                    {
                        var node = nodes[index];
                        inventoryGridItems.Add(new InventoryGridItem(node));
                    }
                    var sortedList = inventoryGridItems.OrderBy(c => c._resNode->Y + c._resNode->ParentNode->Y).ThenBy(c => c._resNode->X + c._resNode->ParentNode->X).ToList();
                    var finalList = new List<InventoryGridItem>();
                    for (var i = 0; i < sortedList.Count; i++)
                    {
                        var item = sortedList[i];
                        if((i / 5) % 2 == 1)
                        {
                            finalList.Add(item);
                        }
                    }

                    _sortedGridItemsRight = finalList;
                }

                return _sortedGridItemsRight;
            }
        }
            
        public int SaddleBagSelected
        {
            get
            {
                //Premium saddle bag does not exist
                if (LeftSaddlebagButton == null)
                {
                    return 0;
                }

                if (LeftSaddlebagButton.IsSelected)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        public RadioButtonUiAddon? LeftSaddlebagButton
        {
            get
            {
                if (_radioButtonUiLeftAddon == null)
                {
                    var radioButtonAtk = GetNodeById(LeftSaddlebagButtonId);
                    if (radioButtonAtk != null)
                    {
                        _radioButtonUiLeftAddon = new RadioButtonUiAddon(radioButtonAtk);
                    }
                }

                return _radioButtonUiLeftAddon;
            }
        }

        public RadioButtonUiAddon? RightSaddlebagButton
        {
            get
            {
                if (_radioButtonUiRightAddon == null)
                {
                    var radioButtonAtk = GetNodeById(RightSaddlebagButtonId);
                    if (radioButtonAtk != null)
                    {
                        _radioButtonUiRightAddon = new RadioButtonUiAddon(radioButtonAtk);
                    }
                }

                return _radioButtonUiRightAddon;
            }
        }
    }
}