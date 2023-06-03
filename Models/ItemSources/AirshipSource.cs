using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Models.ItemSources
{
    public class AirshipSource : IItemSource
    {
        private string _name;
        private int _icon;
        private uint _airshipExplorationPointExId;

        public AirshipSource(string name, uint icon, uint airshipExplorationPointExId)
        {
            _name = name;
            _icon = (int)icon;
            _airshipExplorationPointExId = airshipExplorationPointExId;
        }

        public AirshipExplorationPointEx? AirshipExplorationPointEx => Service.ExcelCache.GetAirshipExplorationPointExSheet().GetRow(AirshipExplorationPointExId);

        public int Icon => _icon;

        public string Name => _name;

        public uint? Count => 1;

        public uint AirshipExplorationPointExId => _airshipExplorationPointExId;

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
                name += " - " + AirshipExplorationPointExId;
#endif
                return name;
            }
        }

        public bool CanOpen => true;
    }
}