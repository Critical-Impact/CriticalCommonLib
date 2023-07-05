namespace CriticalCommonLib.Interfaces;

public interface ITeleporterIpc
{
    bool IsAvailable { get; }
    bool Teleport(uint aetheryteId);
}