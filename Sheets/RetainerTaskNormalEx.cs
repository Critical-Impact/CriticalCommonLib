using System;
using System.Collections.Generic;
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
                return String.Join(", ", Quantity.Where(c => c != 0).Select(c => c.ToString()));
            }
        }
    }
}