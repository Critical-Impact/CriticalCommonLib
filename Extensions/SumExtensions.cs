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