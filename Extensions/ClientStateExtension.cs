using Dalamud.Plugin.Services;

namespace CriticalCommonLib.Extensions
{
    public static class ClientStateExtension
    {
        public static string GetCharacterName(this IClientState clientState)
        {
            return clientState.LocalPlayer != null ? clientState.LocalPlayer.Name.ToString() : "";
        }
    }
}