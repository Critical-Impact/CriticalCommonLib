using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models
{
    public class DutySource : IItemSource
    {
        private string _name;
        private int _icon;
        private uint _contentFinderConditionId;

        public DutySource(string name, uint icon, uint contentFinderConditionId)
        {
            _name = name;
            _icon = (int)icon;
            _contentFinderConditionId = contentFinderConditionId;
        }

        public ContentFinderCondition? ContentFinderCondition => Service.ExcelCache.GetContentFinderConditionExSheet().GetRow(ContentFinderConditionId);

        public int Icon => _icon;

        public string Name => _name;

        public uint? Count => 1;

        public uint ContentFinderConditionId => _contentFinderConditionId;

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
                name += " - " + ContentFinderConditionId;
#endif
                return name;
            }
        }

        public bool CanOpen => true;
    }
}