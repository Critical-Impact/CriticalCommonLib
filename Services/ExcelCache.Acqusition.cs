using System.Collections.Generic;
using System.Linq;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Services
{
    public partial class ExcelCache
    {
        //Special Shop Lookups
        private Dictionary<uint, HashSet<uint>> CostByItemIdLookup { get; set; } = new();
        private Dictionary<uint, HashSet<uint>> ResultByItemIdLookup { get; set; } = new();
        private Dictionary<uint, HashSet<uint>> CostResultLookup { get; set; } = new();
        private Dictionary<uint, HashSet<uint>> ResultCostLookup { get; set; } = new();
        private bool _specialShopLookupCalculated = false;

        public HashSet<uint> GetCurrencies(uint minimumEntries = 0)
        {
            CalculateSpecialShopLookup();
            return CostByItemIdLookup.Where(c => minimumEntries == 0 || c.Value.Count >= minimumEntries).Select(c => c.Key).ToHashSet();
        }

        public bool BoughtWithCurrency(uint currencyId, uint itemId)
        {
            CalculateSpecialShopLookup();
            if (CostResultLookup.ContainsKey(currencyId))
            {
                if (CostResultLookup[currencyId].Contains(itemId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool BoughtWithCurrency(uint itemId)
        {
            CalculateSpecialShopLookup();
            if (ResultCostLookup.ContainsKey(itemId))
            {
                if (ResultCostLookup[itemId].Contains(itemId))
                {
                    return true;
                }
            }

            return false;
        }

        public bool SpentAtSpecialShop(uint itemId)
        {
            CalculateSpecialShopLookup();
            return CostByItemIdLookup.ContainsKey(itemId);
        }

        public bool BoughtAtSpecialShop(uint itemId)
        {
            CalculateSpecialShopLookup();
            return ResultByItemIdLookup.ContainsKey(itemId);
        }

        public HashSet<uint>? GetSpecialShopsByCostItemId(uint costItemId)
        {
            CalculateSpecialShopLookup();
            if (CostByItemIdLookup.ContainsKey(costItemId))
            {
                return CostByItemIdLookup[costItemId];
            }

            return null;
        }

        public HashSet<uint>? GetSpecialShopsByResultItemId(uint resultItemId)
        {
            CalculateSpecialShopLookup();
            if (ResultByItemIdLookup.ContainsKey(resultItemId))
            {
                return ResultByItemIdLookup[resultItemId];
            }

            return null;
        }

        public HashSet<uint>? GetCurrenciesByResultItemId(uint resultItemId)
        {
            CalculateSpecialShopLookup();
            if (ResultCostLookup.ContainsKey(resultItemId))
            {
                return ResultCostLookup[resultItemId];
            }

            return null;
        }
        

        private void CalculateSpecialShopLookup()
        {
            if (_specialShopLookupCalculated)
            {
                return;
            }

            var sheet = GetSheet<SpecialShopEx>();
            foreach (var specialShop in sheet)
            {
                //Use Currency Types appear to
                foreach (var entry in specialShop.Entries)
                {
                    for (var index = 0; index < entry.Cost.Length; index++)
                    {
                        var cost = entry.Cost[index];
                        if (cost.Item.Row != 0)
                        {
                            if (index >= 0 && index < entry.Result.Length)
                            {
                                var result = entry.Result[index];
                                if (result.Item.Row != 0)
                                {
                                    if (!ResultByItemIdLookup.ContainsKey(result.Item.Row))
                                    {
                                        ResultByItemIdLookup.Add(result.Item.Row, new HashSet<uint>());
                                    }

                                    if (!ResultByItemIdLookup[result.Item.Row].Contains(specialShop.RowId))
                                    {
                                        ResultByItemIdLookup[result.Item.Row].Add(specialShop.RowId);
                                    }

                                    if (!CostResultLookup.ContainsKey(cost.Item.Row))
                                    {
                                        CostResultLookup.Add(cost.Item.Row, new HashSet<uint>());
                                    }

                                    if (!CostResultLookup[cost.Item.Row].Contains(result.Item.Row))
                                    {
                                        CostResultLookup[cost.Item.Row].Add(result.Item.Row);
                                    }

                                    if (!ResultCostLookup.ContainsKey(result.Item.Row))
                                    {
                                        ResultCostLookup.Add(result.Item.Row, new HashSet<uint>());
                                    }

                                    if (!ResultCostLookup[result.Item.Row].Contains(cost.Item.Row))
                                    {
                                        ResultCostLookup[result.Item.Row].Add(cost.Item.Row);
                                    }
                                }
                            }

                            if (!CostByItemIdLookup.ContainsKey(cost.Item.Row))
                            {
                                CostByItemIdLookup.Add(cost.Item.Row, new HashSet<uint>());
                            }

                            if (!CostByItemIdLookup[cost.Item.Row].Contains(specialShop.RowId))
                            {
                                CostByItemIdLookup[cost.Item.Row].Add(specialShop.RowId);
                            }
                        }

                    }
                }
            }

            _specialShopLookupCalculated = true;
        }
    }
}