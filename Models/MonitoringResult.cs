using System;
using System.Collections.Generic;

namespace NetworkMonitor.Models
{
    public class MonitoringResult
    {
        public string NodeId { get; set; }
        public string ModuleName { get; set; }
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public double PacketLoss { get; set; }
        public NodeStatus Status { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string> Hops { get; set; }   // IP-адреса хопов из traceroute
    }
}
