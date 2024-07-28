using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace CriticalCommonLib.Services;

public class HostedInventoryHistory : InventoryHistory, IHostedService
{
    public HostedInventoryHistory(IInventoryMonitor monitor) : base(monitor)
    {
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}