using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets
{
    public class HWDGathererInspectionEx : HWDGathererInspection
    {
        private Dictionary<uint, (uint, uint)>? _inspectionResults;
        

        public Dictionary<uint, (uint, uint)> GenerateInspectionResults(Dictionary<uint, (uint, uint)> existingList)
        {
            for (var index = 0; index < ItemReceived.Length; index++)
            {
                var result = ItemReceived[index].Row;
                if (result != 0 && !existingList.ContainsKey(result))
                {
                    uint requirement = (uint)(ItemRequired[index].Value?.Item ?? 0);
                    if (requirement != 0)
                    {
                        var amountRequired = AmountRequired[index];
                        existingList.Add(result, (requirement, amountRequired));
                    }
                }
            }

            return existingList;
        }

        public Dictionary<uint, (uint, uint)> InspectionResults
        {
            get
            {
                if (_inspectionResults == null)
                    _inspectionResults = GenerateInspectionResults(new Dictionary<uint, (uint, uint)>());
                return _inspectionResults;
            }
        }
    }
}