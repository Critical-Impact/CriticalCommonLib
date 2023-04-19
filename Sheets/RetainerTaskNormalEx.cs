using System;
using System.Linq;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class RetainerTaskNormalEx : RetainerTaskNormal
    {
        public LazyRow<RetainerTaskEx> RetainerTaskEx { get; set; } = null!;
        public LazyRow<ItemEx> ItemEx { get; set; } = null!;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            if (Service.ExcelCache.RetainerTaskToRetainerNormalLookup.ContainsKey(RowId))
            {
                RetainerTaskEx = new LazyRow<RetainerTaskEx>(gameData, Service.ExcelCache.RetainerTaskToRetainerNormalLookup[RowId],
                    language);
            }
            else
            {
                RetainerTaskEx = new LazyRow<RetainerTaskEx>(gameData, 0, language);
            }

            ItemEx = new LazyRow<ItemEx>(gameData, Item.Row, language);
        }

        public string TaskName
        {
            get
            {
                if (RetainerTaskEx.Row != 0 && RetainerTaskEx.Value != null)
                {
                    var classJobName = RetainerTaskEx.Value.ClassJobCategory?.Value?.Name.ToString();
                    var level = RetainerTaskEx.Value.RetainerLevel;
                    return classJobName + " - Lv " + level;
                }

                return "Unknown";
            }
        }

        public ushort TaskTime
        {
            get
            {
                if (RetainerTaskEx.Row != 0 && RetainerTaskEx.Value != null)
                {
                    return RetainerTaskEx.Value.MaxTimemin;
                }

                return 0;
            }
        }

        public string Quantities
        {
            get
            {
                return String.Join(", ", Quantity.Where(c => c != 0).Select(c => c.ToString()));
            }
        }
    }
}