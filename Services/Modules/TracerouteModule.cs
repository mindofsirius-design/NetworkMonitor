using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class TracerouteModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "traceroute";
        public int MaxHops { get; set; } = 30;
        public int TimeoutMs { get; set; } = 3000;

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Traceroute?.Enabled ?? false;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var hops = new List<string>();
            bool reachedTarget = false;

            await Task.Run(() =>
            {
                var buffer = new byte[32];
                using (var pinger = new Ping())
                {
                    for (int ttl = 1; ttl <= MaxHops; ttl++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var options = new PingOptions(ttl, true);
                        try
                        {
                            var reply = pinger.Send(node.IpAddress, TimeoutMs, buffer, options);

                            if (reply.Status == IPStatus.TtlExpired)
                            {
                                hops.Add($"{ttl}: {reply.Address} ({reply.RoundtripTime}ms)");
                            }
                            else if (reply.Status == IPStatus.Success)
                            {
                                hops.Add($"{ttl}: {reply.Address} ({reply.RoundtripTime}ms)");
                                reachedTarget = true;
                                break;
                            }
                            else
                            {
                                hops.Add($"{ttl}: * ({reply.Status})");
                            }
                        }
                        catch (PingException)
                        {
                            hops.Add($"{ttl}: * (timeout)");
                        }
                    }
                }
            }, ct);

            var sb = new StringBuilder();
            foreach (var hop in hops)
                sb.AppendLine(hop);

            Logger.Info("Traceroute {0}: {1} hops, reached={2}", node.Name, hops.Count, reachedTarget);

            return new MonitoringResult
            {
                NodeId = node.Id,
                ModuleName = Name,
                Success = reachedTarget,
                Status = reachedTarget ? NodeStatus.Online : NodeStatus.Offline,
                Details = sb.ToString().TrimEnd(),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
