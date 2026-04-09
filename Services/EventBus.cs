using System;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services
{
    public class EventBus
    {
        private static readonly Lazy<EventBus> _instance = new Lazy<EventBus>(() => new EventBus());
        public static EventBus Instance => _instance.Value;

        public event Action<MonitoringResult> OnResult;
        public event Action<NodeEvent> OnEvent;

        public void PublishResult(MonitoringResult result)
        {
            OnResult?.Invoke(result);
        }

        public void PublishEvent(NodeEvent nodeEvent)
        {
            OnEvent?.Invoke(nodeEvent);
        }
    }
}
