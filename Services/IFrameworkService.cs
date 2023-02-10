using System;
using System.Threading;
using System.Threading.Tasks;

namespace CriticalCommonLib.Services;

public interface IFrameworkService
{
    public void Dispose();

    public delegate void OnUpdateDelegate(IFrameworkService framework);
    
    public event OnUpdateDelegate Update;
    public Task RunOnFrameworkThread(Action action);

    public Task RunOnTick(Action action, TimeSpan delay = default(TimeSpan), int delayTicks = 0,
        CancellationToken cancellationToken = default(CancellationToken));
}