using System;
using System.Collections.Generic;
using System.Linq;

namespace CriticalCommonLib.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<T> OrderBySequence<T, TId>(
        this IEnumerable<T> source,
        IEnumerable<TId> order,
        Func<T, TId> idSelector)
    {
        
        return source.OrderBy(x =>
            {
                var index = order.ToList().IndexOf(idSelector.Invoke(x));

                if (index == -1)
                    index = Int32.MaxValue;

                return index;
            })
            .ToList();
    }
}