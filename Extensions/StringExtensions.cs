using System.Globalization;

namespace CriticalCommonLib.Extensions;

public static class StringExtensions
{
    public static string ToTitleCase(this string npcNameSingular)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(npcNameSingular.ToLower()); 
    }
}