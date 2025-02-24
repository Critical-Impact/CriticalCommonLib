using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using AllaganLib.GameSheets.Model;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Helpers;
using AllaganLib.GameSheets.Sheets.Rows;
using AllaganLib.Shared.Extensions;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Models;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using LuminaSupplemental.Excel.Model;
using MapType = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType;


namespace CriticalCommonLib.Services
{
    public class ChatUtilities : IChatUtilities
    {
        private readonly IChatGui _chatGui;
        private readonly IKeyState _keyState;
        private readonly IGameGui _gameGui;
        private readonly IPluginLog _pluginLog;

        public const int SeColorNames     = 504;
        public const int SeColorCommands  = 31;
        public const int SeColorArguments = 546;
        public const int SeColorAlarm     = 518;

        public bool LogsEnabled { get; set; } = false;

        public ChatUtilities(IChatGui chatGui, IKeyState keyState, IGameGui gameGui, IPluginLog pluginLog)
        {
            _chatGui = chatGui;
            _keyState = keyState;
            _gameGui = gameGui;
            _pluginLog = pluginLog;
        }

        public void PrintLog(string message)
        {
            if (LogsEnabled)
            {
                Print(message);
            }
        }

        public delegate SeStringBuilder ReplacePlaceholder(SeStringBuilder builder, string placeholder);

        public void Print(SeString message)
        {
            var entry = new XivChatEntry()
            {
                Message = message,
                Name = SeString.Empty,
                Type = XivChatType.Echo,
            };
            _chatGui.Print(entry);
        }

        public void PrintError(SeString message)
        {
            var entry = new XivChatEntry()
            {
                Message = message,
                Name = SeString.Empty,
                Type = XivChatType.ErrorMessage,
            };
            _chatGui.Print(entry);
        }

        public void Print(string message)
            => Print((SeString) message);

        public void PrintError(string message)
            => PrintError((SeString) message);

        public void Print(string left, string center, int color, string right)
        {
            SeStringBuilder builder = new();
            AddColoredText(builder.AddText(left), center, color).AddText(right);
            Print(builder.BuiltString);
        }

        public void PrintError(string left, string center, int color, string right)
        {
            SeStringBuilder builder = new();
            AddColoredText(builder.AddText(left), center, color).AddText(right);
            PrintError(builder.BuiltString);
        }

        public void PrintClipboardMessage(string objectType, string name, Exception? e = null)
        {
            if (e != null)
            {
                name = name.Length > 0 ? name : "<Unnamed>";
                _pluginLog.Error($"Could not save {objectType}{name} to Clipboard:\n{e}");
                PrintError($"Could not save {objectType}", name, SeColorNames, " to Clipboard.");
            }
            else
            {
                Print(objectType, name.Length > 0 ? name : "<Unnamed>", SeColorNames,
                    " saved to Clipboard.");
            }
        }

        public void PrintGeneralMessage(string objectType, string name)
        {

            Print(objectType, name.Length > 0 ? name : "<Unnamed>", SeColorAlarm,
                "");
        }

        public void PrintFullMapLink(ILocation location, string? textOverride = null)
        {
            if (location.Map.ValueNullable != null && location.Map.ValueNullable.Value.TerritoryType.ValueNullable != null)
            {
                var name = location.ToString();
                if (name != null)
                {
                    var link = AddFullMapLink(new SeStringBuilder(), textOverride ?? name, location.Map.Value.TerritoryType.Value, location.Map.Value,
                        (float)(location.MapX),
                        (float)(location.MapY), true).BuiltString;
                    Print(link);
                }
            }
        }

        public unsafe void PrintGatheringMapLink(GatheringPointRow gatheringPoint)
        {
            var instance = AgentMap.Instance();
            instance->TempMapMarkerCount = 0;
            instance->AddGatheringTempMarker((int)gatheringPoint.GatherMarkerX, (int)gatheringPoint.GatherMarkerY, gatheringPoint.GatheringPointBase.ExportedGatheringPoint.Base.Radius, (uint)gatheringPoint.GatheringPointBase.ExportedGatheringPoint.Icon, 4u, $"Lv. {gatheringPoint.GatheringPointBase.Base.GatheringLevel} {gatheringPoint.GatheringPointNameRow.Base.Singular.ExtractText().ToTitleCase()}");
            instance->OpenMap(gatheringPoint.Map.RowId, gatheringPoint.Map.Value.TerritoryType.RowId, null,MapType.GatheringLog);
        }

        public unsafe void PrintGatheringMapLink(FishingSpotRow fishingSpotRow, FishParameterRow fishParameterRow)
        {
            var instance = AgentMap.Instance();
            instance->TempMapMarkerCount = 0;
            instance->AddGatheringTempMarker(fishingSpotRow.GatherMarkerX, fishingSpotRow.GatherMarkerY, fishingSpotRow.Base.Radius / 7, Icons.FishingIcon, 4u, $"Lv. {fishingSpotRow.Base.GatheringLevel} {fishParameterRow.Base.FishingRecordType.Value.Addon.Value.Text.ExtractText()}");
            instance->OpenMap(fishingSpotRow.Map.RowId, fishingSpotRow.Map.Value.TerritoryType.RowId, null,MapType.GatheringLog);
        }

        public unsafe void PrintGatheringMapLink(SpearfishingNotebookRow spearfishingNotebookRow, SpearfishingItemRow spearfishingItemRow)
        {
            var instance = AgentMap.Instance();
            instance->TempMapMarkerCount = 0;
            instance->AddGatheringTempMarker(spearfishingNotebookRow.GatherMarkerX, spearfishingNotebookRow.GatherMarkerY, spearfishingNotebookRow.Base.Radius / 7, Icons.Spearfishing, 4u, $"Lv. {spearfishingNotebookRow.Base.GatheringLevel} {spearfishingItemRow.FishRecordType}");
            instance->OpenMap(spearfishingNotebookRow.Map.RowId, spearfishingNotebookRow.Map.Value.TerritoryType.RowId, null,MapType.GatheringLog);
        }

        public void PrintFullMapLink(MobSpawnPosition mobSpawnPosition, string text)
        {
            if (mobSpawnPosition.TerritoryType.ValueNullable?.Map.ValueNullable != null)
            {
                var link = AddFullMapLink(new SeStringBuilder(), text, mobSpawnPosition.TerritoryType.Value, mobSpawnPosition.TerritoryType.Value.Map.Value,
                    mobSpawnPosition.Position.X,
                    mobSpawnPosition.Position.Y, true).BuiltString;
                Print(link);
            }
        }

        public void LinkItem(ItemRow item) {
            if (item.RowId == HardcodedItems.FreeCompanyCreditItemId)
            {
                return;
            }
            var payloadList = new List<Payload> {
                new UIForegroundPayload((ushort) (0x223 + item.Base.Rarity * 2)),
                new UIGlowPayload((ushort) (0x224 + item.Base.Rarity * 2)),
                new ItemPayload(item.RowId, item.Base.CanBeHq && _keyState[0x11]),
                new UIForegroundPayload(500),
                new UIGlowPayload(501),
                new TextPayload($"{(char) SeIconChar.LinkMarker}"),
                new UIForegroundPayload(0),
                new UIGlowPayload(0),
                new TextPayload(item.Base.Name.ExtractText() + (item.Base.CanBeHq && _keyState[0x11] ? $" {(char)SeIconChar.HighQuality}" : "")),
                new RawPayload(new byte[] {0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03}),
                new RawPayload(new byte[] {0x02, 0x13, 0x02, 0xEC, 0x03})
            };

            var payload = new SeString(payloadList);

            _chatGui.Print(new XivChatEntry {
                Message = payload
            });
        }


        // Split a format string with '{text}' placeholders into a SeString with Payloads,
        // and replace all placeholders by the returned payloads.
        private SeString Format(string format, ReplacePlaceholder func)
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

        public static SeStringBuilder AddColoredText(SeStringBuilder builder, string text, int colorId)
            => builder.AddUiForeground((ushort) colorId)
                .AddText(text)
                .AddUiForegroundOff();

        public SeStringBuilder AddFullItemLink(SeStringBuilder builder, uint itemId, string itemName)
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
        public SeStringBuilder AddFullMapLink(SeStringBuilder builder, string name, TerritoryType territory, Map? map, float xCoord, float yCoord,
            bool openMapLink = false, bool withCoordinates = true, float fudgeFactor = 0.05f)
        {
            var mapPayload = new MapLinkPayload(territory.RowId, map?.RowId ?? territory.Map.RowId, xCoord, yCoord, fudgeFactor);
            if (openMapLink)
                _gameGui.OpenMapWithMapLink(mapPayload);
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
    }
}