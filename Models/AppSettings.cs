using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Models
{
    public class AppSettings
    {
        public int PingIntervalSeconds { get; set; } = 30;
        public int PingTimeoutMs { get; set; } = 1000;
        public int PingRetries { get; set; } = 3;
        public int UnstableThresholdMs { get; set; } = 200;
        public bool DarkTheme { get; set; } = false;
        public bool SoundAlerts { get; set; } = true;
        public string Language { get; set; } = "ru";
        public double MapZoom { get; set; } = 10;
        public double MapLat { get; set; } = 55.75;
        public double MapLon { get; set; } = 37.61;
    }
}