using System;
using System.Collections.Generic;
using System.Linq;
using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace CriticalCommonLib.Extensions
{
    public static class ExcelSheetExtensions
    {
        public static Dictionary<K, List<V>> ToListDictionary<K, V>(this ExcelSheet<V> source, Func<V, K> keySelector) where V : ExcelRow where K : notnull {
            var dict = new Dictionary<K, List<V>>();
            foreach (var item in source) {
                var k = keySelector(item);
                if (dict.TryGetValue(k, out var list))
                    list.Add(item);
                else
                    dict.Add(k, new List<V>{ item });
            }
            return dict;
        }
        public static Dictionary<uint, V> ToCache<V>(this ExcelSheet<V> source) where V : ExcelRow {
            var dict = new Dictionary<uint, V>();
            foreach (var item in source.ToList()) {
                if (!dict.TryGetValue(item.RowId, out _))
                    dict.Add(item.RowId, item);
            }
            return dict;
        }

        public static Dictionary<uint, HashSet<uint>> ToColumnLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, uint> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, HashSet<uint>>();
            foreach (var item in sourceSheet) {
                var source = sourceSelector(item);
                if (source!= 0 || !ignoreSourceZeroes)
                {
                    var lookup = lookupSelector(item);
                    if (lookup != 0 || !ignoreLookupZeroes)
                    {
                        if (dict.TryGetValue(source, out var list))
                            list.Add(lookup);
                        else
                            dict.Add(source, new HashSet<uint> { lookup });
                    }
                }
            }
            return dict;
        }

        public static Dictionary<uint, HashSet<uint>> ToColumnLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, uint> sourceSelector, Func<V, uint[]> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, HashSet<uint>>();
            foreach (var item in sourceSheet) {
                var source = sourceSelector(item);
                if (source!= 0 || !ignoreSourceZeroes)
                {
                    var lookup = lookupSelector(item);
                    foreach (var lookupItem in lookup)
                    {
                        if (lookupItem != 0 || !ignoreLookupZeroes)
                        {
                            if (dict.TryGetValue(source, out var list))
                                list.Add(lookupItem);
                            else
                                dict.Add(source, new HashSet<uint> { lookupItem });
                        }
                    }
                }
            }
            return dict;
        }

        public static Dictionary<uint, HashSet<uint>> ToColumnLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, uint[]> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, HashSet<uint>>();
            foreach (var item in sourceSheet) {
                var sources = sourceSelector(item);
                foreach (var source in sources)
                {
                    if (source != 0 || !ignoreSourceZeroes)
                    {
                        var lookup = lookupSelector(item);
                        if (lookup != 0 || !ignoreLookupZeroes)
                        {
                            if (dict.TryGetValue(source, out var list))
                                list.Add(lookup);
                            else
                                dict.Add(source, new HashSet<uint> { lookup });
                        }
                    }
                }
            }
            return dict;
        }

        public static Dictionary<uint, HashSet<uint>> ToColumnLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, int[]> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, HashSet<uint>>();
            foreach (var item in sourceSheet) {
                var sources = sourceSelector(item);
                foreach (var source in sources)
                {
                    if (source != 0 || !ignoreSourceZeroes)
                    {
                        var lookup = lookupSelector(item);
                        if (lookup != 0 || !ignoreLookupZeroes)
                        {
                            if (dict.TryGetValue((uint)source, out var list))
                                list.Add(lookup);
                            else
                                dict.Add((uint)source, new HashSet<uint> { lookup });
                        }
                    }
                }
            }
            return dict;
        }

        public static Dictionary<(uint,uint), HashSet<uint>> ToColumnTupleLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, (uint,uint)> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<(uint,uint), HashSet<uint>>();
            foreach (var item in sourceSheet) {
                var source = sourceSelector(item);
                if (source!= (0,0) || !ignoreSourceZeroes)
                {
                    var lookup = lookupSelector(item);
                    if (lookup != 0 || !ignoreLookupZeroes)
                    {
                        if (dict.TryGetValue(source, out var list))
                            list.Add(lookup);
                        else
                            dict.Add(source, new HashSet<uint> { lookup });
                    }
                }
            }
            return dict;
        }

        public static Dictionary<uint, HashSet<(uint,uint)>> ToColumnLookupTuple<V>(this ExcelSheet<V> sourceSheet, Func<V, uint> sourceSelector, Func<V, (uint,uint)> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, HashSet<(uint,uint)>>();
            foreach (var item in sourceSheet) {
                var source = sourceSelector(item);
                if (source!= 0 || !ignoreSourceZeroes)
                {
                    var lookup = lookupSelector(item);
                    if (lookup != (0,0) || !ignoreLookupZeroes)
                    {
                        if (dict.TryGetValue(source, out var list))
                            list.Add(lookup);
                        else
                            dict.Add(source, new HashSet<(uint,uint)> { lookup });
                    }
                }
            }
            return dict;
        }

        public static Dictionary<uint, uint> ToSingleLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, uint> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, uint>();
            foreach (var item in sourceSheet) {
                var source = sourceSelector(item);
                if (source != 0 || !ignoreSourceZeroes)
                {
                    var lookup = lookupSelector(item);
                    if (lookup != 0 || !ignoreLookupZeroes)
                    {
                        if (!dict.TryGetValue(source, out _))
                            dict.Add(source, lookup);
                    }

                }
            }
            return dict;
        }
        
        public static Dictionary<uint, uint> ToSingleLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, IEnumerable<uint>> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<uint, uint>();
            foreach (var item in sourceSheet) {
                var sources = sourceSelector(item);
                foreach (var source in sources)
                {
                    if (source != 0 || !ignoreSourceZeroes)
                    {
                        var lookup = lookupSelector(item);
                        if (lookup != 0 || !ignoreLookupZeroes)
                        {
                            if (!dict.TryGetValue(source, out _))
                                dict.Add(source, lookup);
                        }

                    }
                }
            }
            return dict;
        }
        
        public static Dictionary<(uint,uint), uint> ToSingleTupleLookup<V>(this ExcelSheet<V> sourceSheet, Func<V, (uint,uint)> sourceSelector, Func<V, uint> lookupSelector, bool ignoreSourceZeroes = true, bool ignoreLookupZeroes = true) where V : ExcelRow{
            var dict = new Dictionary<(uint,uint), uint>();
            foreach (var item in sourceSheet) {
                var source = sourceSelector(item);
                if (source != (0,0) || !ignoreSourceZeroes)
                {
                    var lookup = lookupSelector(item);
                    if (lookup != 0 || !ignoreLookupZeroes)
                    {
                        if (!dict.TryGetValue(source, out _))
                            dict.Add(source, lookup);
                    }

                }
            }
            return dict;
        }

    }
}