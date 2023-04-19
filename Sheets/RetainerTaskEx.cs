using System;
using System.Collections.Generic;
using System.Linq;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public enum RetainerTaskType
    {
        HighlandExploration = 1,
        WoodlandExploration = 2,
        WatersideExploration = 3,
        FieldExploration = 4,
        QuickExploration = 5,
        
        Hunting = 6,
        Mining = 7,
        Botanist = 8,
        Fishing = 9,
        
        Unknown = 99
    }
    public class RetainerTaskEx : RetainerTask
    {
        public LazyRow<ClassJobCategoryEx> ClassJobCategoryEx { get; set; } = null!;
        public LazyRow<RetainerTaskNormalEx> RetainerTaskNormalEx { get; set; } = null!;
        public LazyRow<RetainerTaskRandomEx> RetainerTaskRandomEx { get; set; } = null!;

        public override void PopulateData(RowParser parser, GameData gameData, Language language)
        {
            base.PopulateData(parser, gameData, language);
            ClassJobCategoryEx = new LazyRow<ClassJobCategoryEx>(gameData, ClassJobCategory.Row, language);
            RetainerTaskNormalEx = new LazyRow<RetainerTaskNormalEx>(gameData, Task < 30000 ? Task : 0, language);
            RetainerTaskRandomEx = new LazyRow<RetainerTaskRandomEx>(gameData, Task >= 30000 ? Task : 0, language);
        }
        
        public bool IsGatheringVenture => ClassJobCategoryEx?.Value?.IsGathering ?? false;
        public bool IsFishingVenture => ClassJobCategoryEx?.Value?.FSH ?? false;
        public bool IsMiningVenture => ClassJobCategoryEx?.Value?.MIN ?? false;
        public bool IsBotanistVenture => ClassJobCategoryEx?.Value?.BTN ?? false;
        public bool IsCombatVenture => ClassJobCategoryEx?.Value?.IsCombat ?? false;

        public uint Quantity
        {
            get
            {
                if (RetainerTaskNormalEx.Row != 0)
                {
                    return RetainerTaskNormalEx.Value!.Quantity.LastOrDefault();
                }
                else if (RetainerTaskRandomEx.Row != 0)
                {
                    return 1;
                }

                return 0;
            }
        }

        public string Quantities
        {
            get
            {
                if (RetainerTaskNormalEx.Row != 0)
                {
                    return RetainerTaskNormalEx.Value!.Quantities;
                }
                else if (RetainerTaskRandomEx.Row != 0)
                {
                    return "1";
                }

                return "0";
            }
        }

        public RetainerTaskType RetainerTaskType
        {
            get
            {
                if (IsRandom)
                {
                    if (IsFishingVenture)
                    {
                        return RetainerTaskType.WatersideExploration;
                    }
                    else if (IsMiningVenture)
                    {
                        return RetainerTaskType.HighlandExploration;
                    }
                    else if (IsBotanistVenture)
                    {
                        return RetainerTaskType.WoodlandExploration;
                    }
                    else if (IsCombatVenture)
                    {
                        return RetainerTaskType.FieldExploration;
                    }
                    else
                    {
                        return RetainerTaskType.QuickExploration;
                    }
                }
                else if (RetainerTaskNormalEx.Row != 0)
                {
                    if (IsFishingVenture)
                    {
                        return RetainerTaskType.Fishing;
                    }
                    else if (IsMiningVenture)
                    {
                        return RetainerTaskType.Mining;
                    }
                    else if (IsBotanistVenture)
                    {
                        return RetainerTaskType.Botanist;
                    }
                    else if (IsCombatVenture)
                    {
                        return RetainerTaskType.Hunting;
                    }
                }

                return RetainerTaskType.Unknown;
            }
        }

        private List<ItemEx>? _drops;
        public List<ItemEx> Drops
        {
            get
            {
                if (_drops != null)
                {
                    return _drops;
                }
                var drops = new List<ItemEx>();
                if (RetainerTaskRandomEx.Row != 0)
                {
                    var ventureItems = Service.ExcelCache.GetRetainerVentureItems(RetainerTaskRandomEx.Row);
                    if (ventureItems != null)
                    {
                        drops = ventureItems.Select(c => Service.ExcelCache.GetItemExSheet().GetRow(c.Item.Row)).Where(c => c != null).Select(c => c!).ToList();
                    }
                }
                else if (RetainerTaskNormalEx.Row != 0)
                {
                    if (RetainerTaskNormalEx.Value != null)
                    {
                        drops.Add(Service.ExcelCache.GetItemExSheet().GetRow(RetainerTaskNormalEx.Value.Item.Row)!);
                    }
                }

                _drops = drops;

                return _drops;
            }
        }

        private string? _formattedName;
        public string FormattedName
        {
            get
            {
                if (_formattedName == null)
                {
                    if (RetainerTaskNormalEx.Row != 0 && RetainerTaskNormalEx.Value != null)
                    {
                        _formattedName = RetainerTaskNormalEx.Value.TaskName;
                    }
                    else if (RetainerTaskRandomEx.Row != 0 && RetainerTaskRandomEx.Value != null)
                    {
                        _formattedName = RetainerTaskRandomEx.Value.FormattedName;
                    }
                    else
                    {
                        _formattedName = "Unknown Task";
                    }
                    
                }

                return _formattedName;
            }
        }

        public string ExperienceString
        {
            get
            {
                if (Experience > 0)
                {
                    return Experience.ToString();
                }

                return "N/A";
            }
        }

        public string DurationString
        {
            get
            {
                var time = TimeSpan.FromMinutes(MaxTimemin);
                return $"{(int)time.TotalHours}h";
            }
        }

        private string? _nameString;
        public string NameString
        {
            get
            {
                if (_nameString == null)
                {
                    if (RetainerTaskNormalEx.Row != 0 && RetainerTaskNormalEx.Value != null)
                    {
                        _nameString = RetainerTaskNormalEx.Value?.ItemEx.Value?.NameString ?? "Unknown";
                    }
                    else if (RetainerTaskRandomEx.Row != 0 && RetainerTaskRandomEx.Value != null)
                    {
                        _nameString = RetainerTaskRandomEx.Value?.NameString ?? "Unknown";
                    }
                    else
                    {
                        _nameString = "Unknown Task";
                    }
                }

                return _nameString;
            }
        }
    }
}