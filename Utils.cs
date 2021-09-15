using System;
using System.Drawing;
using System.Globalization;
using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace CriticalCommonLib
{
    public static class Utils
    {
        public static ByteColor ColorFromHex(string hexString, int alpha)
        {
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            var r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            var g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            var b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            return new ByteColor() {R = (byte) r, B = (byte) b, G = (byte) g, A = (byte) alpha};
        }
    }
}