using System;
using CriticalCommonLib.EventHandlers;
using Dalamud.Plugin.Services;
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
        Culinarian,
        Miner,
        Botanist,
        Fisher
    }

    private readonly ExcelSheet<ClassJob> _classJobSheet;
    private readonly IUnlockState _unlockState;
    private readonly IPlayerState _playerState;

    public ClassJobService(ExcelSheet<ClassJob> classJobSheet, IUnlockState unlockState, IPlayerState playerState)
    {
        _classJobSheet = classJobSheet;
        _unlockState = unlockState;
        _playerState = playerState;
    }


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
            ClassJobList.Miner => 16,
            ClassJobList.Botanist => 17,
            ClassJobList.Fisher => 18,
            _ => 0
        };
    }

    public ClassJobList? GetClassJobFromIdx(byte classJobIdx)
    {
        return classJobIdx switch
        {
            8 => ClassJobList.Carpenter,
            9 => ClassJobList.Blacksmith,
            10 => ClassJobList.Armorer,
            11 => ClassJobList.Goldsmith,
            12 => ClassJobList.Leatherworker,
            13 => ClassJobList.Weaver,
            14 => ClassJobList.Alchemist,
            15 => ClassJobList.Culinarian,
            16 => ClassJobList.Miner,
            17 => ClassJobList.Botanist,
            18 => ClassJobList.Fisher,
            _ => null
        };
    }

    public sbyte GetExpArrayIdx(ClassJobList me)
    {
        return _classJobSheet.GetRow(GetClassJobIndex(me))!.ExpArrayIndex;
    }

    public short GetPlayerLevel(ClassJobList me)
    {
        var classJob = _classJobSheet.GetRow(GetClassJobIndex(me));
        return _playerState.GetClassJobLevel(classJob);
    }

    public unsafe ushort GetWksSyncedLevel(ClassJobList me)
    {
        var jobLevel = (ushort)GetPlayerLevel(me);

        var handler = CSCraftEventHandler.Instance();

        if (handler != null)
            for (var i = 0; i < 2; ++i)
                if (handler->WKSClassJobs[i] == GetClassJobIndex(me))
                    return Math.Min(jobLevel, handler->WKSClassLevels[i]);

        return jobLevel;
    }

    public short GetPlayerLevelByCraftTypeId(uint craftTypeId)
    {
        var job = craftTypeId switch
        {
            0 => ClassJobList.Carpenter,
            1 => ClassJobList.Blacksmith,
            2 => ClassJobList.Armorer,
            3 => ClassJobList.Goldsmith,
            4 => ClassJobList.Leatherworker,
            5 => ClassJobList.Weaver,
            6 => ClassJobList.Alchemist,
            7 => ClassJobList.Culinarian,
            _ => (ClassJobList?)null
        };
        return job.HasValue ? GetPlayerLevel(job.Value) : (short)0;
    }

    public bool IsSecretRecipeBookUnlocked(RowRef<SecretRecipeBook> secretRecipeBook)
    {
        if (secretRecipeBook.RowId == 0) return true;
        return _unlockState.IsSecretRecipeBookUnlocked(secretRecipeBook.Value);
    }

    public bool IsFolkloreBookUnlocked(RowRef<NotebookDivision> noteBookDivision)
    {
        if (noteBookDivision.RowId == 0) return true;
        return _unlockState.IsNotebookDivisionUnlocked(noteBookDivision.Value);
    }

}