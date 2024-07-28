using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace CriticalCommonLib.Sheets
{
    public interface ILazySubRow
    {
        /// <summary>
        /// The backing value/row that was passed through when creating the reference
        /// </summary>
        public uint Row { get; }
        public uint? SubRow { get; }
        
        /// <summary>
        /// Checks whether something has loaded successfully.
        /// </summary>
        /// <remarks>
        /// If something fails to load, this will still be false regardless.
        /// </remarks>
        public bool IsValueCreated { get; }
        
        public Language Language { get; }
        
        public ExcelRow? RawRow { get; }
    }

    /// <summary>
    /// Allows for sheet definitions to contain entries which will lazily load the referenced sheet row
    /// </summary>
    /// <typeparam name="T">The row type to load</typeparam>
    public class LazySubRow< T > : ILazySubRow where T : ExcelRow
    {
        private readonly GameData _gameData;
        private readonly uint _row;
        private readonly uint? _subRow;
        private readonly Language _language;

        private T? _value;

        /// <summary>
        /// The backing value/row that was passed through when creating the reference
        /// </summary>
        public uint Row => _row;

        /// <summary>
        /// The backing value/row that was passed through when creating the reference
        /// </summary>
        public uint? SubRow => _subRow;

        public Language Language => _language;

        /// <summary>
        /// Construct a new LazySubRow instance
        /// </summary>
        /// <param name="gameData">The Lumina instance to load from</param>
        /// <param name="row">The row id to load if/when the value is fetched</param>
        /// <param name="language">The requested language to use when resolving row references</param>
        public LazySubRow( GameData gameData, uint row, uint? subRow, Language language = Language.None )
        {
            _gameData = gameData;
            _row = row;
            _subRow = subRow;
            _language = language;
        }

        /// <summary>
        /// Construct a new LazySubRow instance
        /// </summary>
        /// <param name="gameData">The Lumina instance to load from</param>
        /// <param name="row">The row id to load if/when the value is fetched</param>
        /// <param name="language">The language to load the row in</param>
        public LazySubRow( GameData gameData, int row, int? subrow, Language language = Language.None ) : this( gameData, (uint)row, (uint?)subrow, language )
        {
        }

        /// <summary>
        /// Lazily load the referenced sheet/row, otherwise return the existing row.
        /// </summary>
        public T? Value
        {
            get
            {
                if( IsValueCreated )
                {
                    return _value;
                }

                if (_subRow != null)
                {
                    Service.ExcelCache.GetSheet<T>(); //TODO: Not have to rely on a hack to track which sheets need to be unloaded
                    _value = _gameData.GetExcelSheet<T>(_language)?.GetRow(_row, _subRow.Value);
                }
                else
                {
                    Service.ExcelCache.GetSheet<T>();
                    _value = _gameData.GetExcelSheet<T>(_language)?.GetRow(_row);
                }

                return _value;
            }
        }

        /// <summary>
        /// Provides access to the raw row without any fuckery, useful for serialisation and etc.
        /// </summary>
        public ExcelRow? RawRow => Value;
        
        /// <summary>
        /// Checks whether something has loaded successfully.
        /// </summary>
        /// <remarks>
        /// If something fails to load, this will still be false regardless.
        /// </remarks>
        public bool IsValueCreated => _value != null;

        public override string ToString()
        {
            return $"{typeof( T ).FullName}#{_row},#{(_subRow.HasValue ? _subRow.Value : "")}";
        }
    }
}