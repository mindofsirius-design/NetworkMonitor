using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.ComponentModel;

namespace NetworkMonitor.Models
{
    public enum NodeStatus
    {
        Online,
        Offline,
        Unstable,
        Unknown
    }

    public class NetworkNode : INotifyPropertyChanged
    {
        private NodeStatus _status = NodeStatus.Unknown;
        private long _lastPing = -1;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string DeviceType { get; set; } = "Unknown";
        public string Description { get; set; }

        // Позиция на карте
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Позиция на холсте (для отображения)
        public double X { get; set; }
        public double Y { get; set; }

        public DateTime LastSeen { get; set; }

        public NodeStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
        }

        public long LastPingMs
        {
            get => _lastPing;
            set { _lastPing = value; OnPropertyChanged(); }
        }

        // Цвет по статусу
        public string StatusColor
        {
            get
            {
                switch (Status)
                {
                    case NodeStatus.Online: return "#4CAF50";
                    case NodeStatus.Offline: return "#F44336";
                    case NodeStatus.Unstable: return "#FFC107";
                    default: return "#9E9E9E";
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}
