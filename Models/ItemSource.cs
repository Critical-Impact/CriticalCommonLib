namespace CriticalCommonLib.Models
{
    public struct ItemSource
    {
        //TODO: Turn me into an interface so you can have dutysource, itemsource, desynthsource, etc
        private string _name;
        private int _icon;
        private uint? _itemId;

        public ItemSource(string name, uint icon, uint? itemId)
        {
            _name = name;
            _icon = (int)icon;
            _itemId = itemId;
        }

        public int Icon => _icon;

        public string Name => _name;

        public uint? ItemId => _itemId;

        public bool HasItem => _itemId != null;
    }
}