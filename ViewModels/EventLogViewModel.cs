using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetworkMonitor.Models;
using NetworkMonitor.Services;

namespace NetworkMonitor.ViewModels
{
    public class EventLogViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly ObservableCollection<NetworkNode> _nodes;

        public ObservableCollection<NodeEvent> Events { get; } = new ObservableCollection<NodeEvent>();
        public List<string> NodeNames { get; } = new List<string>();
        public List<string> EventTypes { get; } = new List<string> { "Все", "Смена статуса", "Оповещение", "Ошибка модуля" };

        private string _selectedNodeName = "Все";
        public string SelectedNodeName
        {
            get => _selectedNodeName;
            set { _selectedNodeName = value; OnPropertyChanged(); Refresh(); }
        }

        private string _selectedEventType = "Все";
        public string SelectedEventType
        {
            get => _selectedEventType;
            set { _selectedEventType = value; OnPropertyChanged(); Refresh(); }
        }

        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); Refresh(); }
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); Refresh(); }
        }

        public EventLogViewModel(DatabaseService database, ObservableCollection<NetworkNode> nodes)
        {
            _database = database;
            _nodes = nodes;

            NodeNames.Add("Все");
            NodeNames.AddRange(nodes.Select(n => n.Name));

            Refresh();
        }

        public void Refresh()
        {
            Events.Clear();

            var allEvents = _database.GetAllEvents(500);

            var filtered = allEvents.AsEnumerable();

            if (_selectedNodeName != "Все")
            {
                var node = _nodes.FirstOrDefault(n => n.Name == _selectedNodeName);
                if (node != null)
                    filtered = filtered.Where(e => e.NodeId == node.Id);
            }

            if (_selectedEventType != "Все")
                filtered = filtered.Where(e => e.EventType == _selectedEventType);

            if (_dateFrom.HasValue)
                filtered = filtered.Where(e => e.Timestamp >= _dateFrom.Value);

            if (_dateTo.HasValue)
                filtered = filtered.Where(e => e.Timestamp <= _dateTo.Value.AddDays(1));

            foreach (var ev in filtered)
                Events.Add(ev);
        }
    }
}
