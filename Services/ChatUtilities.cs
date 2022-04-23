using System;
using System.Diagnostics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;

namespace CriticalCommonLib.Services
{

    internal static class SeStringBuilderExtension
    {
        public static SeStringBuilder AddColoredText(this SeStringBuilder builder, string text, int colorId)
            => builder.AddUiForeground((ushort) colorId)
                .AddText(text)
                .AddUiForegroundOff();

        public static SeStringBuilder AddFullItemLink(this SeStringBuilder builder, uint itemId, string itemName)
            => builder.AddUiForeground(0x0225)
                .AddUiGlow(0x0226)
                .AddItemLink(itemId, false)
                .AddUiForeground(0x01F4)
                .AddUiGlow(0x01F5)
                .AddText($"{(char) SeIconChar.LinkMarker}")
                .AddUiGlowOff()
                .AddUiForegroundOff()
                .AddText(itemName)
                .Add(RawPayload.LinkTerminator)
                .AddUiGlowOff()
                .AddUiForegroundOff();
    }

    public static class ChatUtilities
    {
        public const int SeColorNames     = 504;
        public const int SeColorCommands  = 31;
        public const int SeColorArguments = 546;
        public const int SeColorAlarm     = 518;
        
        public delegate SeStringBuilder ReplacePlaceholder(SeStringBuilder builder, string placeholder);

        public static void Print(SeString message)
        {
            var entry = new XivChatEntry()
            {
                Message = message,
                Name = SeString.Empty,
                Type = XivChatType.Echo,
            };
            Service.Chat.PrintChat(entry);
        }

        public static void PrintError(SeString message)
        {
            var entry = new XivChatEntry()
            {
                Message = message,
                Name = SeString.Empty,
                Type = XivChatType.ErrorMessage,
            };
            Service.Chat.PrintChat(entry);
        }

        public static void Print(string message)
            => Print((SeString) message);

        public static void PrintError(string message)
            => PrintError((SeString) message);

        public static void Print(string left, string center, int color, string right)
        {
            SeStringBuilder builder = new();
            builder.AddText(left).AddColoredText(center, color).AddText(right);
            Print(builder.BuiltString);
        }

        public static void PrintError(string left, string center, int color, string right)
        {
            SeStringBuilder builder = new();
            builder.AddText(left).AddColoredText(center, color).AddText(right);
            PrintError(builder.BuiltString);
        }

        public static void PrintClipboardMessage(string objectType, string name, Exception? e = null)
        {
            if (e != null)
            {
                name = name.Length > 0 ? name : "<Unnamed>";
                PluginLog.Error($"Could not save {objectType}{name} to Clipboard:\n{e}");
                PrintError($"Could not save {objectType}", name, SeColorNames, " to Clipboard.");
            }
            else
            {
                Print(objectType, name.Length > 0 ? name : "<Unnamed>", SeColorNames,
                    " saved to Clipboard.");
            }
        }



        // Split a format string with '{text}' placeholders into a SeString with Payloads, 
        // and replace all placeholders by the returned payloads.
        private static SeString Format(string format, ReplacePlaceholder func)
        {
            SeStringBuilder builder = new();
            var lastPayload = 0;
            var openBracket = -1;
            for (var i = 0; i < format.Length; ++i)
            {
                if (format[i] == '{')
                {
                    openBracket = i;
                }
                else if (openBracket != -1 && format[i] == '}')
                {
                    builder.AddText(format.Substring(lastPayload, openBracket - lastPayload));
                    var placeholder = format.Substring(openBracket, i - openBracket + 1);
                    Debug.Assert(placeholder.StartsWith('{') && placeholder.EndsWith('}'));
                    func(builder, placeholder);
                    lastPayload = i + 1;
                    openBracket = -1;
                }
            }

            if (lastPayload != format.Length)
                builder.AddText(format[lastPayload..]);
            return builder.BuiltString;
        }
    }
}