using System;
using System.Collections.Generic;
using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace CriticalCommonLib.Sheets;

public class LazyOneToOneRow<TRelatedRow, TSourceRow> where TRelatedRow : ExcelRow where TSourceRow : ExcelRow
{
    private readonly Func<ExcelSheet<TRelatedRow>, Dictionary<uint,uint>> _generateAction;
    private TSourceRow Row { get; set; }
    private Language SheetLanguage { get; set; }

    private readonly GameData _gameData;

    private readonly string _relationName;
    
    public LazyOneToOneRow(TSourceRow row, GameData gameData, Language language, Func<ExcelSheet<TRelatedRow>, Dictionary<uint,uint>> generateAction, string relationName)
    {
        _gameData = gameData;
        Row = row;
        SheetLanguage = language;
        _generateAction = generateAction;
        _relationName = relationName;
    }

    private LazyRow<TRelatedRow>? _cachedRow = null;

    public LazyRow<TRelatedRow> GetValue()
    {
        if (_cachedRow == null)
        {
            string cacheName = typeof(TSourceRow).Name +  typeof(TRelatedRow).Name + _relationName;
            if (!Service.ExcelCache.OneToManyCache.ContainsKey(cacheName))
            {
                var lookup = _generateAction.Invoke(Service.ExcelCache.GetSheet<TRelatedRow>());
                Service.ExcelCache.OneToOneCache[cacheName] = lookup;
            }
            var cache = Service.ExcelCache.OneToOneCache[cacheName];
            uint item = 0;
            if (cache.ContainsKey(Row.RowId))
            {
                item = cache[Row.RowId];
            }
            _cachedRow = new LazyRow<TRelatedRow>(_gameData, item, SheetLanguage);
        }

        return _cachedRow;
    }
}