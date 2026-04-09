using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services.Modules
{
    public interface IMonitoringModule
    {
        string Name { get; }
        bool IsEnabled(NetworkNode node);
        Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct);
    }
}
