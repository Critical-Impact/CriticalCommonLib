using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class InventoryGrid : GameWindow
    {
        private List<InventoryGridItem> _sortedItems;
        private List<InventoryTabItem> _inventoryTabs;

        public InventoryGrid(List<InventoryGridItem> sortedItems, List<InventoryTabItem> tabItems)
        {
            _sortedItems = sortedItems;
            _inventoryTabs = tabItems.OrderBy(c => c.tabIndex).ToList();
        }

        public void ClearColors()
        {
            foreach (var item in _sortedItems)
            {
                item._resNode->AddBlue = 0;
                item._resNode->AddRed = 0;
                item._resNode->AddGreen = 0;
                item._resNode->Color.A = 255;
            }
            foreach (var item in _inventoryTabs)
            {
                item.resNode->AddBlue = 0;
                item.resNode->AddRed = 0;
                item.resNode->AddGreen = 0;
                item.resNode->Color.A = 255;
            }
        }

        public void SetColor(int itemIndex, int red, int green, int blue)
        {
            if (itemIndex >= 0 && _sortedItems.Count > itemIndex)
            {
                _sortedItems[itemIndex]._resNode->AddBlue = (ushort) blue;
                _sortedItems[itemIndex]._resNode->AddRed = (ushort) red;
                _sortedItems[itemIndex]._resNode->AddGreen = (ushort) green;
            }
        }

        public void SetTabColor(int tabIndex, int red, int green, int blue)
        {
            if (tabIndex >= 0 && _inventoryTabs.Count > tabIndex)
            {
                _inventoryTabs[tabIndex].resNode->AddBlue = (ushort) blue;
                _inventoryTabs[tabIndex].resNode->AddRed = (ushort) red;
                _inventoryTabs[tabIndex].resNode->AddGreen = (ushort) green;
            }
        }

        public void SetColor(int itemIndex, Vector4 color)
        {
            if (itemIndex >= 0 && _sortedItems.Count > itemIndex)
            {
                _sortedItems[itemIndex]._resNode->Color.A = (byte) (color.W * 255.0f);
                _sortedItems[itemIndex]._resNode->AddBlue = (ushort) (color.Z * 255.0f);
                _sortedItems[itemIndex]._resNode->AddRed = (ushort) (color.X * 255.0f);
                _sortedItems[itemIndex]._resNode->AddGreen = (ushort) (color.Y * 255.0f);
            }
        }

        public void SetColors(HashSet<int> itemIndexes, Vector4 color, bool invert = false)
        {
            if (invert)
            {
                for (var index = 0; index < _sortedItems.Count; index++)
                {
                    if (!itemIndexes.Contains(index))
                    {
                        SetColor(index, color);
                    }
                }
            }
            else
            {
                foreach (var itemIndex in itemIndexes)
                {
                    SetColor(itemIndex, color);
                }
            }
        }

        public void SetTabColor(int tabIndex, Vector4 color)
        {
            if (tabIndex >= 0 && _inventoryTabs.Count > tabIndex)
            {
                _inventoryTabs[tabIndex].resNode->AddBlue = (ushort) (color.Z * 255.0f);
                _inventoryTabs[tabIndex].resNode->AddRed = (ushort) (color.X * 255.0f);
                _inventoryTabs[tabIndex].resNode->AddGreen = (ushort) (color.Y * 255.0f);
                _inventoryTabs[tabIndex].resNode->Color.A = (byte) (color.W * 255.0f);
            }
        }

        public void SetTabColors(HashSet<int> tabIndexes, Vector4 color, bool invert = false)
        {
            if (invert)
            {
                for (var index = 0; index < _inventoryTabs.Count; index++)
                {
                    if (!tabIndexes.Contains(index))
                    {
                        SetTabColor(index, color);
                    }
                }
            }
            else
            {
                foreach (var tabIndex in tabIndexes)
                {
                    SetTabColor(tabIndex, color);
                }
            }
        }
    }
}