using System.Linq;
using CriticalCommonLib.Sheets;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models
{
    public class VentureSource : IItemSource
    {
        private string _name;
        private int _icon;
        private uint? _itemId;
        private uint? _count;
        private RetainerTaskEx _retainerTaskEx;

        public VentureSource(RetainerTaskEx retainerTaskEx)
        {
            _name = retainerTaskEx.NameString;
            _icon = Service.ExcelCache.GetItemExSheet().GetRow(21072)!.Icon;
            _itemId = 21072;
            //TODO: Support multiple return quantities
            _count = retainerTaskEx.Quantity;
            _retainerTaskEx = retainerTaskEx;
        }

        public RetainerTaskEx RetainerTask => _retainerTaskEx;

        public ItemEx? Item => ItemId != null ? Service.ExcelCache.GetItemExSheet().GetRow(ItemId.Value) : null;

        public int Icon => _icon;

        public string Name => _name;

        public uint? ItemId => _itemId;

        public bool HasItem => _itemId != null;

        public uint? Count => _count;

        private string? _formattedName;

        public string FormattedName
        {
            get
            {
                if (_formattedName != null)
                {
                    return _formattedName;
                }
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
                _formattedName = name;
                return _formattedName;
            }
        }

        public bool CanOpen => true;
    }
}