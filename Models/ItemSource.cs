using System;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Models
{
    public struct ItemSource : IItemSource
    {
        //TODO: Turn me into an interface so you can have dutysource, itemsource, desynthsource, etc
        private string _name;
        private int _icon;
        private uint? _itemId;
        private uint? _count;

        public ItemSource(string name, uint icon, uint? itemId, uint? count = null)
        {
            _name = name;
            _icon = (int)icon;
            _itemId = itemId;
            _count = count;
        }

        public ItemEx? Item => ItemId != null ? Service.ExcelCache.GetItemExSheet().GetRow(ItemId.Value) : null;

        public int Icon => _icon;

        public string Name => _name;

        public uint? ItemId => _itemId;

        public bool HasItem => _itemId != null;

        public uint? Count => _count;

        public string FormattedName
        {
            get
            {
                var name = Name;
                if (Count != null)
                {
                    name += " - " + Count;
                }
                #if DEBUG
                if (ItemId != null)
                {
                    name += " - " + ItemId;
                }
                #endif
                return name;
            }
        }

        public bool CanOpen => true;
    }
}