using CriticalCommonLib.Sheets;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models
{
    public class SubmarineSource : IItemSource
    {
        private string _name;
        private int _icon;
        private uint _submarineExplorationExId;

        public SubmarineSource(string name, uint icon, uint submarineExplorationExId)
        {
            _name = name;
            _icon = (int)icon;
            _submarineExplorationExId = submarineExplorationExId;
        }

        public SubmarineExplorationEx? SubmarineExplorationEx => Service.ExcelCache.GetSubmarineExplorationExSheet().GetRow(SubmarineExplorationExId);

        public int Icon => _icon;

        public string Name => _name;

        public uint? Count => 1;

        public uint SubmarineExplorationExId => _submarineExplorationExId;

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
                name += " - " + SubmarineExplorationExId;
#endif
                return name;
            }
        }

        public bool CanOpen => true;
    }
}