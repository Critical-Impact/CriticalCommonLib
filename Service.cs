using System;
using CriticalCommonLib.Services;
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

namespace CriticalCommonLib
{
    public class Service
    {
        [PluginService] public static DalamudPluginInterface Interface { get; private set; } = null!;
        [PluginService] public static ChatGui Chat { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static CommandManager Commands { get; private set; } = null!;
        [PluginService] public static Condition Condition { get; private set; } = null!;
        [PluginService] public static DataManager Data { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static GameGui Gui { get; private set; } = null!;
        [PluginService] public static KeyState KeyState { get; private set; } = null!;
        [PluginService] public static LibcFunction LibcFunction { get; private set; } = null!;
        [PluginService] public static ObjectTable Objects { get; private set; } = null!;
        [PluginService] public static SigScanner Scanner { get; private set; } = null!;
        [PluginService] public static TargetManager Targets { get; private set; } = null!;
        [PluginService] public static ToastGui Toasts { get; private set; } = null!;
        [PluginService] public static GameNetwork Network { get; private set; } = null!;
        public static FrameworkService FrameworkService { get; set; } = null!;
        public static ExcelCache ExcelCache { get; set; } = null!;

        public static void Dereference()
        {
            Interface = null;
            Chat = null;
            ClientState = null;
            Commands = null;
            Condition = null;
            Data = null;
            Framework = null;
            Gui = null;
            KeyState = null;
            LibcFunction = null;
            Objects = null;
            Scanner = null;
            Targets = null;
            Toasts = null;
            Network = null;
            FrameworkService = null;
            ExcelCache.Dispose();
            ExcelCache = null;
        }
    }
}
