using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Sheets;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;

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

        public static bool LogsEnabled { get; set; } = false;

        public static void PrintLog(string message)
        {
            if (LogsEnabled)
            {
                Print(message);
            }
        }
        
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

        public static void PrintGeneralMessage(string objectType, string name)
        {

            Print(objectType, name.Length > 0 ? name : "<Unnamed>", SeColorAlarm,
                "");
        }

        public static void PrintFullMapLink(ILocation location, string? textOverride = null)
        {
            if (location.MapEx.Value != null && location.MapEx.Value.TerritoryType.Value != null)
            {
                var name = location.ToString();
                if (name != null)
                {
                    var link = new SeStringBuilder().AddFullMapLink(textOverride ?? name, location.MapEx.Value.TerritoryType.Value, location.MapEx.Value,
                        (float)(location.MapX),
                        (float)(location.MapY), true).BuiltString;
                    Print(link);
                }
            }
        }

        public static SeStringBuilder AddFullMapLink(this SeStringBuilder builder, string name, TerritoryType territory, MapEx? mapEx, float xCoord, float yCoord,
            bool openMapLink = false, bool withCoordinates = true, float fudgeFactor = 0.05f)
        {
            var mapPayload = new MapLinkPayload(territory.RowId, mapEx?.RowId ?? territory.Map.Row, xCoord, yCoord, fudgeFactor);
            if (openMapLink)
                Service.Gui.OpenMapWithMapLink(mapPayload);
            if (withCoordinates)
                name = $"{name} ({xCoord.ToString("00.0", CultureInfo.InvariantCulture)}, {yCoord.ToString("00.0", CultureInfo.InvariantCulture)})";
            return builder.AddUiForeground(0x0225)
                .AddUiGlow(0x0226)
                .Add(mapPayload)
                .AddUiForeground(500)
                .AddUiGlow(501)
                .AddText($"{(char)SeIconChar.LinkMarker}")
                .AddUiGlowOff()
                .AddUiForegroundOff()
                .AddText(name)
                .Add(RawPayload.LinkTerminator)
                .AddUiGlowOff()
                .AddUiForegroundOff();
        }
        
        public static void LinkItem(ItemEx item) {
            var payloadList = new List<Payload> {
                new UIForegroundPayload((ushort) (0x223 + item.Rarity * 2)),
                new UIGlowPayload((ushort) (0x224 + item.Rarity * 2)),
                new ItemPayload(item.RowId, item.CanBeHq && Service.KeyState[0x11]),
                new UIForegroundPayload(500),
                new UIGlowPayload(501),
                new TextPayload($"{(char) SeIconChar.LinkMarker}"),
                new UIForegroundPayload(0),
                new UIGlowPayload(0),
                new TextPayload(item.Name + (item.CanBeHq && Service.KeyState[0x11] ? $" {(char)SeIconChar.HighQuality}" : "")),
                new RawPayload(new byte[] {0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03}),
                new RawPayload(new byte[] {0x02, 0x13, 0x02, 0xEC, 0x03})
            };

            var payload = new SeString(payloadList);

            Service.Chat.PrintChat(new XivChatEntry {
                Message = payload
            });
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