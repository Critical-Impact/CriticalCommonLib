using System;
using System.Collections.Generic;
using AllaganLib.GameSheets.Sheets.Helpers;
using CriticalCommonLib.Crafting;


namespace CriticalCommonLib.Extensions
{
    public static class SumExtensions
    {
        public static T Sum<T>(this T a, T b) where T: Interfaces.ISummable<T>
        {
            return a.Add(a,b);
        }

        public static CraftItem Sum(this IEnumerable<CraftItem> source)
        {
            var result = new CraftItem();

            foreach (var item in source)
            {
                result = result.Add(item, result);
            }

            if (result.Recipe != null)
            {
                if (result.QuantityRequired % result.Yield != 0)
                {
                    result.QuantityRequired = (uint)Math.Ceiling((double)result.QuantityRequired / result.PreferenceYield) * result.PreferenceYield;
                    result.QuantityNeeded = (uint)Math.Ceiling((double)result.QuantityNeeded / result.PreferenceYield) * result.PreferenceYield;
                }
            }

            return result;
        }

        public static CompanyCraftMaterial Sum(this IEnumerable<CompanyCraftMaterial> source)
        {
            var result = new CompanyCraftMaterial();

            foreach (var item in source)
            {
                result = result.Add(item, result);
            }

            return result;
        }
    }
}