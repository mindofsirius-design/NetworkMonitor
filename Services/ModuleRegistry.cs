using System.Collections.Generic;
using System.Linq;
using NetworkMonitor.Models;
using NetworkMonitor.Services.Modules;

namespace NetworkMonitor.Services
{
    public class ModuleRegistry
    {
        private readonly List<IMonitoringModule> _modules = new List<IMonitoringModule>();

        public ModuleRegistry(AppSettings settings)
        {
            _modules.Add(new PingModule
            {
                TimeoutMs = settings.PingTimeoutMs,
                Retries = settings.PingRetries,
                UnstableThresholdMs = settings.UnstableThresholdMs
            });
            _modules.Add(new TcpModule());
            _modules.Add(new SnmpModule());
            _modules.Add(new TracerouteModule());
        }

        public IEnumerable<IMonitoringModule> GetAllModules() => _modules.AsReadOnly();

        public IEnumerable<IMonitoringModule> GetEnabledModules(NetworkNode node)
        {
            return _modules.Where(m => m.IsEnabled(node));
        }

        public void UpdateSettings(AppSettings settings)
        {
            var ping = _modules.OfType<PingModule>().FirstOrDefault();
            if (ping != null)
            {
                ping.TimeoutMs = settings.PingTimeoutMs;
                ping.Retries = settings.PingRetries;
                ping.UnstableThresholdMs = settings.UnstableThresholdMs;
            }
        }
    }
}
