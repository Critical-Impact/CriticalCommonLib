using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Comparer
{
    public class UIColorComparer : IEqualityComparer<UIColor>
    {
        public bool Equals(UIColor? x, UIColor? y)
        {
            return x?.UIForeground == y?.UIForeground; // based on variable i
        }

        public int GetHashCode(UIColor obj)
        {
            return obj.UIForeground.GetHashCode(); // hashcode of variable to compare
        }
    }
}