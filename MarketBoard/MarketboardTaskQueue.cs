using CriticalCommonLib.Services;

namespace CriticalCommonLib.MarketBoard;

public class MarketboardTaskQueue : BackgroundTaskQueue
{
    public MarketboardTaskQueue(int capacity = 1) : base(capacity)
    {
        
    }
}