namespace CriticalCommonLib.Addons;

public struct SubmarinePartMenuItem
{
    public uint ItemId;
    public uint QtyPerSet;
    public uint SetsSubmitted;
    public uint SetsRequired;

    public uint QtyRemaining => this.QtyPerSet * (this.SetsRequired - this.SetsSubmitted);
}