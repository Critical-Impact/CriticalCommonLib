using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Interfaces;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class CompanyCraftSequenceEx : CompanyCraftSequence
{
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language);
        ActiveCompanyCraftParts = CompanyCraftPart.Where(c => c.Row != 0).ToArray();
    }

    public class CompanyCraftMaterial : ISummable<CompanyCraftMaterial>
    {
        public CompanyCraftMaterial(uint itemId, uint quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public CompanyCraftMaterial()
        {
            
        }

        public uint ItemId { get; }
        public uint Quantity { get; }
        public CompanyCraftMaterial Add(CompanyCraftMaterial a, CompanyCraftMaterial b)
        {
            return new CompanyCraftMaterial(a.ItemId, a.Quantity + b.Quantity);
        }
    }

    public LazyRow<CompanyCraftPart>[] ActiveCompanyCraftParts { get; private set; } = null!;
    private Dictionary<uint, List<CompanyCraftMaterial>>? _partsRequired;
    private List<CompanyCraftMaterial>? _allPartsRequired;

    public List<CompanyCraftMaterial> MaterialsRequired(uint? phase)
    {
        if (_partsRequired == null || _allPartsRequired == null)
        {
            _partsRequired = new Dictionary<uint, List<CompanyCraftMaterial>>();
            _allPartsRequired = new List<CompanyCraftMaterial>();
            var totalIndex = 0u;
            foreach (var lazyPart in ActiveCompanyCraftParts)
            {
                var part = lazyPart.Value;
                if (part != null)
                {
                    for (var index = 0u; index < part.CompanyCraftProcess.Length; index++)
                    {
                        var lazyProcess = part.CompanyCraftProcess[index];
                        var process = lazyProcess.Value;
                        if (process != null)
                        {
                            foreach (var supplyItem in process.UnkData0)
                            {
                                var actualItem = Service.ExcelCache.GetCompanyCraftSupplyItemSheet()
                                    .GetRow(supplyItem.SupplyItem);
                                if (actualItem != null)
                                {
                                    if (actualItem.ItemEx.Row != 0 && actualItem.ItemEx.Value != null)
                                    {
                                        var material = new CompanyCraftMaterial(actualItem.Item.Row, (uint)supplyItem.SetQuantity * supplyItem.SetsRequired);
                                        _partsRequired.TryAdd(totalIndex, new List<CompanyCraftMaterial>());
                                        _partsRequired[totalIndex].Add(material);
                                        _allPartsRequired.Add(material);
                                    }
                                }
                            }
                        }

                        
                    }
                }
                totalIndex++;
            }
            _allPartsRequired = _allPartsRequired.GroupBy(c => c.ItemId).Select(c => c.Sum()).ToList();
        }

        if (phase == null)
        {
            return _allPartsRequired;
        }

        if (_partsRequired.ContainsKey(phase.Value))
        {
            return _partsRequired[phase.Value];
        }

        return new();
    }
}