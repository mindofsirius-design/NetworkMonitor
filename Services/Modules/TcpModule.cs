using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class TcpModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "tcp";
        public int TimeoutMs { get; set; } = 5000;

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Tcp?.Enabled ?? false;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var ports = node.Monitoring?.Tcp?.Ports ?? new List<int>();
            var openPorts = new List<int>();
            var closedPorts = new List<int>();

            foreach (int port in ports)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync(node.IpAddress, port);
                        var delayTask = Task.Delay(TimeoutMs, ct);
                        var completed = await Task.WhenAny(connectTask, delayTask);

                        if (completed == connectTask && client.Connected)
                            openPorts.Add(port);
                        else
                            closedPorts.Add(port);
                    }
                }
                catch
                {
                    closedPorts.Add(port);
                }
            }

            bool allOpen = closedPorts.Count == 0 && openPorts.Count > 0;
            var sb = new StringBuilder();
            if (openPorts.Count > 0) sb.AppendFormat("Open: {0}", string.Join(", ", openPorts));
            if (closedPorts.Count > 0)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.AppendFormat("Closed: {0}", string.Join(", ", closedPorts));
            }

            var result = new MonitoringResult
            {
                NodeId = node.Id,
                ModuleName = Name,
                Success = allOpen,
                Status = allOpen ? NodeStatus.Online : (openPorts.Count > 0 ? NodeStatus.Unstable : NodeStatus.Offline),
                Details = sb.ToString(),
                Timestamp = DateTime.UtcNow
            };

            Logger.Info("TCP {0}: {1}", node.Name, result.Details);
            return result;
        }
    }
}
