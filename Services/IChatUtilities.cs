using System;
using CriticalCommonLib.Interfaces;
using CriticalCommonLib.Sheets;
using Dalamud.Game.Text.SeStringHandling;

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
    void LinkItem(ItemEx item);
}