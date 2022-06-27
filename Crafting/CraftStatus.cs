namespace CriticalCommonLib.Crafting
{
    public enum CraftStatus
    {
        Normal    = 0,
        Poor      = 1,
        Good      = 2,
        Excellent = 3,
    }

    public static class StatusExtension
    {
        public static bool Improved(this CraftStatus status)
            => status == CraftStatus.Good || status == CraftStatus.Excellent;
    }
}