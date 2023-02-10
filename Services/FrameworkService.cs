using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;

namespace CriticalCommonLib.Services;

public class FrameworkService : IFrameworkService
{
    public Framework _framework;
    public FrameworkService(Framework framework)
    {
        _framework = framework;
        _framework.Update += OnFrameworkOnUpdate;    
    }

    private void OnFrameworkOnUpdate(Framework framework)
    {
        Update?.Invoke(this);
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkOnUpdate;
    }

    public event IFrameworkService.OnUpdateDelegate? Update;

    public Task RunOnFrameworkThread(Action action)
    {
        return _framework.RunOnFrameworkThread(action);
    }

    public Task RunOnTick(Action action, TimeSpan delay = default(TimeSpan), int delayTicks = 0,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return _framework.RunOnTick(action, delay, delayTicks, cancellationToken);
    }
}