using System.Collections.Generic;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Interfaces;

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

            return result;
        }
    }
}