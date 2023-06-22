using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class CompanyCraftSequenceEx : CompanyCraftSequence
{
    public class CompanyCraftMaterial
    {
        public CompanyCraftMaterial(uint itemId, uint quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public uint ItemId { get; }
        public uint Quantity { get; }
    }
    private Dictionary<uint, List<CompanyCraftMaterial>>? _partsRequired;
    private List<CompanyCraftMaterial>? _allPartsRequired;

    public List<CompanyCraftMaterial> MaterialsRequired(uint? phase)
    {
        if (_partsRequired == null || _allPartsRequired == null)
        {
            _partsRequired = new Dictionary<uint, List<CompanyCraftMaterial>>();
            _allPartsRequired = new List<CompanyCraftMaterial>();
            foreach (var lazyPart in this.CompanyCraftPart)
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
                                        _partsRequired.TryAdd(index, new List<CompanyCraftMaterial>());
                                        _partsRequired[index].Add(material);
                                        _allPartsRequired.Add(material);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (phase == null)
        {
            return _allPartsRequired;
        }
        return _partsRequired[phase.Value];
    }
}