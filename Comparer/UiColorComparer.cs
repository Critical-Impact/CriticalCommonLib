using System.Collections.Generic;
using Lumina.Excel.Sheets;


namespace CriticalCommonLib.Comparer
{
    public class UIColorComparer : IEqualityComparer<UIColor>
    {
        public bool Equals(UIColor? x, UIColor? y)
        {
            return x?.Dark == y?.Dark;
        }

        public bool Equals(UIColor x, UIColor y)
        {
            return x.Dark == y.Dark;
        }

        public int GetHashCode(UIColor obj)
        {
            return obj.Dark.GetHashCode();
        }
    }
}