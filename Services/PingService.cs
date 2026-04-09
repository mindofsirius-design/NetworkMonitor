using System.Linq;
using System.Text;

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services
{
    public class PingService
    {
        private CancellationTokenSource _cts;
        private AppSettings _settings;

        public event Action<NetworkNode> NodeStatusChanged;

        public PingService(AppSettings settings)
        {
            _settings = settings;
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
        }

        public void Start(IEnumerable<NetworkNode> nodes)
        {
            Stop();
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoop(nodes, _cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task MonitorLoop(IEnumerable<NetworkNode> nodes, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var node in nodes)
                {
                    if (token.IsCancellationRequested) break;
                    await PingNodeAsync(node);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_settings.PingIntervalSeconds),
                    token
                ).ContinueWith(_ => { }); // подавляем исключение отмены
            }
        }

        public async Task PingNodeAsync(NetworkNode node)
        {
            var ping = new Ping();
            long totalMs = 0;
            int successCount = 0;

            for (int i = 0; i < _settings.PingRetries; i++)
            {
                try
                {
                    var reply = await ping.SendPingAsync(
                        node.IpAddress,
                        _settings.PingTimeoutMs
                    );

                    if (reply.Status == IPStatus.Success)
                    {
                        totalMs += reply.RoundtripTime;
                        successCount++;
                    }
                }
                catch
                {
                    // устройство недоступно
                }
            }

            // Определяем статус
            if (successCount == 0)
            {
                node.Status = NodeStatus.Offline;
                node.LastPingMs = -1;
            }
            else
            {
                long avgMs = totalMs / successCount;
                node.LastPingMs = avgMs;
                node.LastSeen = DateTime.Now;

                node.Status = avgMs > _settings.UnstableThresholdMs
                    ? NodeStatus.Unstable
                    : NodeStatus.Online;
            }

            NodeStatusChanged?.Invoke(node);
        }
    }
}
