using System;
using CriticalCommonLib.Interfaces;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Ipc;

using Dalamud.Plugin;

public class TeleporterIpc : ITeleporterIpc
{
    private bool _isAvailable;
    private long _timeSinceLastCheck;

    public bool IsAvailable
    {
        get
        {
            if (this._timeSinceLastCheck + 5000 > Environment.TickCount64)
            {
                return this._isAvailable;
            }

            try
            {
                this._consumerMessageSetting.InvokeFunc();
                this._isAvailable = true;
                this._timeSinceLastCheck = Environment.TickCount64;
            }
            catch
            {
                this._isAvailable = false;
            }

            return this._isAvailable;
        }
    }

    private ICallGateSubscriber<bool> _consumerMessageSetting = null!;
    private ICallGateSubscriber<uint, byte, bool> _consumerTeleport = null!;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IChatGui _chatGui;
    private readonly IPluginLog _pluginLog;

    private void Subscribe()
    {
        try
        {
            this._consumerTeleport = _pluginInterface.GetIpcSubscriber<uint, byte, bool>("Teleport");
            this._consumerMessageSetting = _pluginInterface.GetIpcSubscriber<bool>("Teleport.ChatMessage");
        }
        catch (Exception ex)
        {
            _pluginLog.Debug($"Failed to subscribe to Teleporter\nReason: {ex}");
        }
    }

    public TeleporterIpc(IDalamudPluginInterface pluginInterface, IChatGui chatGui, IPluginLog pluginLog)
    {
        _pluginInterface = pluginInterface;
        _chatGui = chatGui;
        _pluginLog = pluginLog;
        this.Subscribe();
    }

    public bool Teleport(uint aetheryteId)
    {
        try
        {
            return this._consumerTeleport.InvokeFunc(aetheryteId, 0);
        }
        catch
        {
            _chatGui.PrintError("Teleporter plugin is not responding");
            return false;
        }
    }
}