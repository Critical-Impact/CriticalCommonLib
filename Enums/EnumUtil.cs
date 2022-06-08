using System;
using System.Collections.Generic;
using System.Linq;

namespace CriticalCommonLib.Enums
{
    public class EnumUtil
    {
        public static IEnumerable<TEnum> GetFlags<TEnum>()
            where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Where(v =>
                {
                    var x = Convert.ToInt64(v); // because enums can be Int64
                    return x != 0 && (x & (x - 1)) == 0;
                    // Checks whether x is a power of 2
                    // Example: when x = 16, the binary values are:
                    // x:         10000
                    // x-1:       01111
                    // x & (x-1): 00000
                });
        }
    }
}