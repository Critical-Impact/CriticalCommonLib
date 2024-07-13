using CriticalCommonLib.Services;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace CriticalCommonLib
{
    public class Service
    {
        [PluginService] public static IChatGui Chat { get; set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ICommandManager Commands { get; set; } = null!;
        [PluginService] public static ICondition Condition { get; set; } = null!;
        [PluginService] public static IDataManager Data { get; set; } = null!;
        [PluginService] public static IFramework Framework { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;
        [PluginService] public static IKeyState KeyState { get; set; } = null!;
        [PluginService] public static IObjectTable Objects { get; set; } = null!;
        [PluginService] public static ITargetManager Targets { get; set; } = null!;
        [PluginService] public static IToastGui Toasts { get; set; } = null!;
        [PluginService] public static IGameNetwork Network { get; set; } = null!;
        [PluginService] public static ITextureProvider TextureProvider { get; set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
        [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
        [PluginService] public static IContextMenu ContextMenu { get; set; } = null!;
        [PluginService] public static IPluginLog Log { get; set; } = null!;
        [PluginService] public static ITitleScreenMenu TitleScreenMenu { get; set; } = null!;
        public static ExcelCache ExcelCache { get; set; } = null!;

        public void Dispose()
        {
            //Required so we aren't left with a static that points to excel cache
            ExcelCache = null!;
        }
    }
}
