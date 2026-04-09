using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class SnmpModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "snmp";
        public int TimeoutMs { get; set; } = 5000;

        private static readonly ObjectIdentifier SysDescr = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");
        private static readonly ObjectIdentifier SysUpTime = new ObjectIdentifier("1.3.6.1.2.1.1.3.0");
        private static readonly ObjectIdentifier SysName = new ObjectIdentifier("1.3.6.1.2.1.1.5.0");

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Snmp?.Enabled ?? false;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var config = node.Monitoring?.Snmp ?? new SnmpConfig();

            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var version = config.Version == "1" ? VersionCode.V1 : VersionCode.V2;
                    var endpoint = new IPEndPoint(IPAddress.Parse(node.IpAddress), 161);
                    var community = new OctetString(config.Community);

                    var variables = new List<Variable>
                    {
                        new Variable(SysDescr),
                        new Variable(SysUpTime),
                        new Variable(SysName)
                    };

                    var results = Messenger.Get(
                        version,
                        endpoint,
                        community,
                        variables,
                        TimeoutMs);

                    var details = new List<string>();
                    foreach (var v in results)
                    {
                        details.Add($"{v.Id}={v.Data}");
                    }

                    Logger.Info("SNMP {0}: OK, {1} OIDs", node.Name, results.Count);

                    return new MonitoringResult
                    {
                        NodeId = node.Id,
                        ModuleName = Name,
                        Success = true,
                        Status = NodeStatus.Online,
                        Details = string.Join("; ", details),
                        Timestamp = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "SNMP {0} failed", node.Name);
                    return new MonitoringResult
                    {
                        NodeId = node.Id,
                        ModuleName = Name,
                        Success = false,
                        Status = NodeStatus.Offline,
                        Details = ex.Message,
                        Timestamp = DateTime.UtcNow
                    };
                }
            }, ct);
        }
    }
}
