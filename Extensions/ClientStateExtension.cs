using System;
using Dalamud.Game.ClientState;

namespace CriticalCommonLib.Extensions
{
    public static class ClientStateExtension
    {
        public static string GetCharacterName(this ClientState clientState)
        {
            return clientState.LocalPlayer != null ? clientState.LocalPlayer.Name.ToString() : "";
        }
    }
}