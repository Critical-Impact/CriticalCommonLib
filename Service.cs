using CriticalCommonLib.Services;
using CriticalCommonLib.Time;
using DalaMock.Interfaces;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace CriticalCommonLib
{
    public class Service : IServiceContainer
    {
        public static IPluginInterfaceService Interface { get; set; } = null!;
        [PluginService] public static IChatGui Chat { get; set; } = null!;
        [PluginService] public static IClientState ClientState { get; set; } = null!;
        [PluginService] public static ICommandManager Commands { get; set; } = null!;
        [PluginService] public static ICondition Condition { get; set; } = null!;
        [PluginService] public static IDataManager Data { get; set; } = null!;
        [PluginService] public static IFramework Framework { get; set; } = null!;
        [PluginService] public static IGameGui GameGui { get; set; } = null!;
        [PluginService] public static IKeyState KeyState { get; set; } = null!;
        [PluginService] public static ILibcFunction LibcFunction { get; set; } = null!;
        [PluginService] public static IObjectTable Objects { get; set; } = null!;
        [PluginService] public static ITargetManager Targets { get; set; } = null!;
        [PluginService] public static IToastGui Toasts { get; set; } = null!;
        [PluginService] public static IGameNetwork Network { get; set; } = null!;
        [PluginService] public static ITextureProvider TextureProvider { get; set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
        [PluginService] public static IPluginLog Log { get; set; } = null!;
        public static ExcelCache ExcelCache { get; set; } = null!;
        public static ISeTime SeTime { get; set; } = null!;

        public static void Dereference()
        {
            Interface = null!;
            Chat = null!;
            ClientState = null!;
            Commands = null!;
            Condition = null!;
            Data = null!;
            Framework = null!;
            GameGui = null!;
            KeyState = null!;
            LibcFunction = null!;
            Objects = null!;
            Targets = null!;
            Toasts = null!;
            Network = null!;
            ExcelCache.Dispose();
            ExcelCache = null!;
            SeTime = null!;
        }

        public IPluginInterfaceService PluginInterfaceService
        {
            get
            {
                return Interface;
            }
            set
            {
                Interface = value;
            }
        }
    }
}
