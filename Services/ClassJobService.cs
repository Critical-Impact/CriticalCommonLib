using System;
using CriticalCommonLib.EventHandlers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace CriticalCommonLib.Services;

public class ClassJobService
{
    public enum ClassJobList
    {
        Carpenter,
        Blacksmith,
        Armorer,
        Goldsmith,
        Leatherworker,
        Weaver,
        Alchemist,
        Culinarian
    }

    private readonly ExcelSheet<ClassJob> _classJobSheet;

    public byte GetClassJobIndex(ClassJobList me)
    {
        return me switch
        {
            ClassJobList.Carpenter => 8,
            ClassJobList.Blacksmith => 9,
            ClassJobList.Armorer => 10,
            ClassJobList.Goldsmith => 11,
            ClassJobList.Leatherworker => 12,
            ClassJobList.Weaver => 13,
            ClassJobList.Alchemist => 14,
            ClassJobList.Culinarian => 15,
            _ => 0
        };
    }

    public ClassJobList? GetClassJobFromIdx(byte classJobIdx) =>
        classJobIdx switch
        {
            8 => ClassJobList.Carpenter,
            9 => ClassJobList.Blacksmith,
            10 => ClassJobList.Armorer,
            11 => ClassJobList.Goldsmith,
            12 => ClassJobList.Leatherworker,
            13 => ClassJobList.Weaver,
            14 => ClassJobList.Alchemist,
            15 => ClassJobList.Culinarian,
            _ => null
        };

    public ClassJobService(ExcelSheet<ClassJob> classJobSheet)
    {
        _classJobSheet = classJobSheet;
    }
    public sbyte GetExpArrayIdx(ClassJobList me) =>
        _classJobSheet.GetRow(this.GetClassJobIndex(me))!.ExpArrayIndex;

    public unsafe short GetPlayerLevel(ClassJobList me) => PlayerState.Instance()->ClassJobLevels[this.GetExpArrayIdx(me)];

    public unsafe ushort GetWksSyncedLevel(ClassJobList me)
    {
        var jobLevel = (ushort)this.GetPlayerLevel(me);

        var handler = CSCraftEventHandler.Instance();

        if (handler != null)
        {
            for (var i = 0; i < 2; ++i)
            {
                if (handler->WKSClassJobs[i] == this.GetClassJobIndex(me))
                    return Math.Min(jobLevel, handler->WKSClassLevels[i]);
            }


        }
        return jobLevel;


    }
}