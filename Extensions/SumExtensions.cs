using System;
using System.Collections.Generic;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Sheets;

namespace CriticalCommonLib.Extensions
{
    public static class SumExtensions
    {
        public static T Sum<T>(this T a, T b) where T:ISummable<T>
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
                    result.QuantityRequired = (uint)Math.Ceiling((double)result.QuantityRequired / result.Yield) * result.Yield;
                    result.QuantityNeeded = (uint)Math.Ceiling((double)result.QuantityNeeded / result.Yield) * result.Yield;
                }
            }

            return result;
        }
        
        public static CompanyCraftSequenceEx.CompanyCraftMaterial Sum(this IEnumerable<CompanyCraftSequenceEx.CompanyCraftMaterial> source)
        {
            var result = new CompanyCraftSequenceEx.CompanyCraftMaterial();

            foreach (var item in source)
            {
                result = result.Add(item, result);
            }

            return result;
        }
    }
}