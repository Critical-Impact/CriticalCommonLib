using CriticalCommonLib.Crafting;
using CriticalCommonLib.Services;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace CriticalCommonLib
{
    public class Service
    {
        public static ExcelCache ExcelCache { get; set; } = null!;

        public void Dispose()
        {
            //Required so we aren't left with a static that points to excel cache
            ExcelCache = null!;
        }
    }
}
