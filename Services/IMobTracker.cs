using System.Collections.Generic;
using LuminaSupplemental.Excel.Model;

namespace CriticalCommonLib.Services;

public interface IMobTracker
{
    void Enable();
    void Disable();
    void Dispose();
    
    bool Enabled { get; }

    bool SaveCsv(string filePath, List<MobSpawnPosition> positions);
    List<MobSpawnPosition> LoadCsv(string filePath, out bool success);
    void AddEntry(MobSpawnPosition spawnPosition);

    void SetEntries(List<MobSpawnPosition> spawnPositions);
    List<MobSpawnPosition> GetEntries();
    void ClearSavedData();
}