using System.Linq;
using CriticalCommonLib.Sheets;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Models.ItemSources
{
    public class GrandCompanySupplySource : IItemSource
    {
        private string _name;
        private int _icon;
        private uint _quantity;
        private uint _dailySupplyItemId;

        public GrandCompanySupplySource(uint itemId, uint dailySupplyItemId)
        {
            _icon = 60313;
            _name = "Grand Company Supply";
            _dailySupplyItemId = dailySupplyItemId;
            
            var supplyRow = DailySupplyItem;
            var supplyData = supplyRow?.UnkData0.First(c => c.Item == itemId);
            if (supplyData != null)
            {
                _quantity = supplyData.Quantity;
            }
        }

        public DailySupplyItem? DailySupplyItem => Service.ExcelCache.GetSheet<DailySupplyItem>().GetRow(DailySupplyItemId);

        public int Icon => _icon;

        public string Name => _name;

        public uint? Count => _quantity;

        public uint DailySupplyItemId => _dailySupplyItemId;

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
                name += " - " + DailySupplyItemId;
#endif
                return name;
            }
        }

        public bool CanOpen => false;
    }
}