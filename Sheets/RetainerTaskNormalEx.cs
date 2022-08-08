using System.Collections.Generic;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class RetainerTaskNormalEx : RetainerTaskNormal
    {
        public LazyRow<RetainerTaskEx> RetainerTaskEx { get; set; } = null!;

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
        }

        public bool IsGatheringVenture => RetainerTaskEx.Value?.ClassJobCategoryEx?.Value?.IsGathering ?? false;
        public bool IsFishingVenture => RetainerTaskEx.Value?.ClassJobCategoryEx?.Value?.FSH ?? false;
        public bool IsMiningVenture => RetainerTaskEx.Value?.ClassJobCategoryEx?.Value?.MIN ?? false;
        public bool IsBotanistVenture => RetainerTaskEx.Value?.ClassJobCategoryEx?.Value?.BTN ?? false;
        public bool IsCombatVenture => RetainerTaskEx.Value?.ClassJobCategoryEx?.Value?.IsCombat ?? false;

        public string TaskName
        {
            get
            {
                if (RetainerTaskEx.Row != 0 && RetainerTaskEx.Value != null)
                {
                    var classJobName = RetainerTaskEx.Value.ClassJobCategory?.Value?.Name.ToString();
                    var level = RetainerTaskEx.Value.RetainerLevel;
                    return classJobName + " - " + level;
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
                var quantities = new List<string>();
                if (Quantity0 != 0)
                {
                    quantities.Add(Quantity0.ToString());
                }
                if (Quantity1 != 0)
                {
                    quantities.Add(Quantity1.ToString());
                }
                if (Quantity2 != 0)
                {
                    quantities.Add(Quantity2.ToString());
                }
                if (Quantity3 != 0)
                {
                    quantities.Add(Quantity3.ToString());
                }
                if (Quantity4 != 0)
                {
                    quantities.Add(Quantity4.ToString());
                }

                return string.Join(", ", quantities);
            }
        }
    }
}