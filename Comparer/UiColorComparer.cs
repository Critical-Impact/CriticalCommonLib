using System.Collections.Generic;
using Lumina.Excel.Sheets;


namespace CriticalCommonLib.Comparer
{
    public class UIColorComparer : IEqualityComparer<UIColor>
    {
        public bool Equals(UIColor? x, UIColor? y)
        {
            return x?.UIForeground == y?.UIForeground;
        }

        public bool Equals(UIColor x, UIColor y)
        {
            return x.UIForeground == y.UIForeground;
        }

        public int GetHashCode(UIColor obj)
        {
            return obj.UIForeground.GetHashCode();
        }
    }
}