using System;
using System.Collections.Generic;

namespace CriticalCommonLib.Extensions;

public static class RandomExtension
{
    private static Random rng = new Random();  

    public static void Shuffle<T>(this IList<T> list)  
    {  
        int n = list.Count;  
        for (int i = 0; i < (n - 1); i++)
        {
            int r = i + rng.Next(n - i);
            (list[r], list[i]) = (list[i], list[r]);
        }
    }
}