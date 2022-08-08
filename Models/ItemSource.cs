namespace CriticalCommonLib.Models
{
    public struct ItemSource
    {
        private string _name;
        private int _icon;
        private uint? _itemId;

        public ItemSource(string name, int icon, uint? itemId)
        {
            _name = name;
            _icon = icon;
            _itemId = itemId;
        }

        public int Icon => _icon;

        public string Name => _name;

        public uint? ItemId => _itemId;

        public bool HasItem => _itemId != null;
    }
}