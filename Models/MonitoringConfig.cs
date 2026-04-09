using System.Collections.Generic;

namespace NetworkMonitor.Models
{
    public class PingConfig
    {
        public bool Enabled { get; set; } = true;
        public int IntervalSec { get; set; } = 5;
    }

    public class TcpConfig
    {
        public bool Enabled { get; set; } = false;
        public List<int> Ports { get; set; } = new List<int> { 80, 443 };
    }

    public class SnmpConfig
    {
        public bool Enabled { get; set; } = false;
        public string Community { get; set; } = "public";
        public string Version { get; set; } = "2c";
    }

    public class TracerouteConfig
    {
        public bool Enabled { get; set; } = false;
    }

    public class MonitoringConfig
    {
        public PingConfig Ping { get; set; } = new PingConfig();
        public TcpConfig Tcp { get; set; } = new TcpConfig();
        public SnmpConfig Snmp { get; set; } = new SnmpConfig();
        public TracerouteConfig Traceroute { get; set; } = new TracerouteConfig();
    }
}
