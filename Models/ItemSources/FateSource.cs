using System.Linq;
using CriticalCommonLib.Sheets;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models.ItemSources
{
    public class FateSource : IItemSource
    {
        private string _name;
        private int _icon;
        private uint _fateId;

        public FateSource(uint fateId)
        {
            _fateId = fateId;
            
            var fate = Fate;
            _name = fate?.Name.AsReadOnly().ToString() ?? "Unknown";
            _icon = (int)(fate?.IconMap ?? 0);
        }

        public Fate? Fate => Service.ExcelCache.GetSheet<Fate>().GetRow(FateId);

        public int Icon => _icon;

        public string Name => _name;

        public uint? Count => null;

        public uint FateId => _fateId;

        public string FormattedName
        {
            get
            {
                var name = Name;
#if DEBUG
                name += " - Fate Id: " + _fateId;
#endif
                return name;
            }
        }

        public bool CanOpen => false;
    }
}