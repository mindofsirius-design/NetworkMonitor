using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class PingModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "ping";

        public int TimeoutMs { get; set; } = 1000;
        public int Retries { get; set; } = 3;
        public int UnstableThresholdMs { get; set; } = 200;

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Ping?.Enabled ?? true;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var result = new MonitoringResult
            {
                NodeId = node.Id,
                ModuleName = Name,
                Timestamp = DateTime.UtcNow
            };

            int successCount = 0;
            long totalMs = 0;

            using (var pinger = new Ping())
            {
                for (int i = 0; i < Retries; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var reply = await pinger.SendPingAsync(node.IpAddress, TimeoutMs);
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            totalMs += reply.RoundtripTime;
                        }
                    }
                    catch (PingException ex)
                    {
                        Logger.Debug(ex, "Ping failed for {0}", node.IpAddress);
                    }
                }
            }

            if (successCount == 0)
            {
                result.Success = false;
                result.LatencyMs = -1;
                result.PacketLoss = 1.0;
                result.Status = NodeStatus.Offline;
                result.Details = "All pings failed";
            }
            else
            {
                long avgMs = totalMs / successCount;
                double loss = 1.0 - (double)successCount / Retries;

                result.Success = true;
                result.LatencyMs = avgMs;
                result.PacketLoss = loss;
                result.Status = avgMs > UnstableThresholdMs || loss > 0.3
                    ? NodeStatus.Unstable
                    : NodeStatus.Online;
                result.Details = $"avg={avgMs}ms, loss={loss:P0}";
            }

            Logger.Info("Ping {0} ({1}): {2}, {3}ms", node.Name, node.IpAddress, result.Status, result.LatencyMs);
            return result;
        }
    }
}
