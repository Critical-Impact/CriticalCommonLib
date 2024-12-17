using System;
using AllaganLib.GameSheets.Model;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;
using CriticalCommonLib.Interfaces;

using Dalamud.Game.Text.SeStringHandling;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Services;

public interface IChatUtilities
{
    bool LogsEnabled { get; set; }
    void PrintLog(string message);
    void Print(SeString message);
    void Print(string message);
    void Print(string left, string center, int color, string right);
    void PrintError(SeString message);
    void PrintError(string message);
    void PrintError(string left, string center, int color, string right);
    void PrintClipboardMessage(string objectType, string name, Exception? e = null);
    void PrintGeneralMessage(string objectType, string name);
    void PrintFullMapLink(ILocation location, string? textOverride = null);
    void PrintFullMapLink(MobSpawnPosition mobSpawnPosition, string text);
    void PrintGatheringMapLink(GatheringPointRow gatheringPoint);
    void PrintGatheringMapLink(FishingSpotRow fishingSpotRow, FishParameterRow fishParameterRow);
    void PrintGatheringMapLink(SpearfishingNotebookRow spearfishingNotebookRow, SpearfishingItemRow spearfishingItemRow);

    void LinkItem(ItemRow item);
}