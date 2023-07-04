using System;
using System.Collections;
using System.Collections.Generic;
using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace CriticalCommonLib.Sheets;

public class LazyRelated<TRelatedRow, TSourceRow> : IEnumerable<LazyRow<TRelatedRow>> where TRelatedRow : ExcelRow where TSourceRow : ExcelRow 
{
    private readonly Func<ExcelSheet<TRelatedRow>, Dictionary<uint,List<uint>>> _generateAction;
    private TSourceRow Row { get; set; }
    private Language SheetLanguage { get; set; }

    private readonly GameData _gameData;

    private readonly string _relationName;
    
    public LazyRelated(TSourceRow row, GameData gameData, Language language, Func<ExcelSheet<TRelatedRow>, Dictionary<uint,List<uint>>> generateAction, string relationName)
    {
        _gameData = gameData;
        Row = row;
        SheetLanguage = language;
        _generateAction = generateAction;
        _relationName = relationName;
    }

    private List<LazyRow<TRelatedRow>>? _cachedRows = null;

    private List<LazyRow<TRelatedRow>> GetValues()
    {
        if (_cachedRows == null)
        {
            string cacheName = typeof(TSourceRow).Name +  typeof(TRelatedRow).Name + _relationName;
            if (!Service.ExcelCache.OneToManyCache.ContainsKey(cacheName))
            {
                var lookup = _generateAction.Invoke(Service.ExcelCache.GetSheet<TRelatedRow>());
                Service.ExcelCache.OneToManyCache[cacheName] = lookup;
            }
            var cache = Service.ExcelCache.OneToManyCache[cacheName];
            
            if (cache.ContainsKey(Row.RowId))
            {
                _cachedRows = new List<LazyRow<TRelatedRow>>(cache[Row.RowId].Count);
                foreach (var item in cache[Row.RowId])
                {
                    _cachedRows.Add(new LazyRow<TRelatedRow>(_gameData, item, SheetLanguage));
                }
            }
            else
            {
                _cachedRows = new List<LazyRow<TRelatedRow>>();
            }
        }

        return _cachedRows;
    }

    public IEnumerator<LazyRow<TRelatedRow>> GetEnumerator()
    {
        return GetValues().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}