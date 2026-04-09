using System;

namespace NetworkMonitor.Models
{
    public class NodeEvent
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public NodeStatus? OldStatus { get; set; }
        public NodeStatus? NewStatus { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
