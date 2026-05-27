using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NetworkMonitor.Models;
using NetworkMonitor.Services;

namespace NetworkMonitor.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly StorageService _storage = new StorageService();
        public StorageService Storage => _storage;  //публичная копия
        private readonly ModuleRegistry _moduleRegistry;
        private readonly MonitoringScheduler _scheduler;
        public MonitoringScheduler Scheduler => _scheduler; //публичная копия
        private readonly DatabaseService _database;
        private readonly NotificationService _notifications;

        public TracerouteGroupingService TracerouteGroups { get; } = new TracerouteGroupingService();
        public ObservableCollection<NetworkNode> Nodes { get; set; }
        public ObservableCollection<NodeLink> Links { get; set; }
        public AppSettings Settings { get; set; }

        public ObservableCollection<ToastNotification> Toasts => _notifications.Toasts;

        private NetworkNode _selectedNode;
        public NetworkNode SelectedNode
        {
            get => _selectedNode;
            set { _selectedNode = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedNode)); }
        }

        public bool HasSelectedNode => _selectedNode != null;

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set { _isMonitoring = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonitoringLabel)); }
        }

        public string MonitoringLabel => IsMonitoring ? "\u23F9 \u0421\u0442\u043E\u043F" : "\u25B6 \u0421\u0442\u0430\u0440\u0442";

        public bool HasNodes => Nodes.Count > 0;

        private int _onlineCount;
        public int OnlineCount { get => _onlineCount; set { _onlineCount = value; OnPropertyChanged(); } }

        private int _offlineCount;
        public int OfflineCount { get => _offlineCount; set { _offlineCount = value; OnPropertyChanged(); } }

        private int _unstableCount;
        public int UnstableCount { get => _unstableCount; set { _unstableCount = value; OnPropertyChanged(); } }

        public ICommand ToggleMonitoringCommand { get; }
        public ICommand SaveCommand { get; }

        public DatabaseService Database => _database;
        public NotificationService Notifications => _notifications;

        public MainViewModel()
        {
            Settings = _storage.LoadSettings() ?? new AppSettings();
            Nodes = new ObservableCollection<NetworkNode>(_storage.LoadNodes() ?? new System.Collections.Generic.List<NetworkNode>());
            Links = new ObservableCollection<NodeLink>(_storage.LoadLinks() ?? new System.Collections.Generic.List<NodeLink>());

            _moduleRegistry = new ModuleRegistry(Settings);
            _scheduler = new MonitoringScheduler(_moduleRegistry)
            {
                DefaultIntervalSec = Settings.PingIntervalSeconds
            };
            _scheduler.GroupingService = TracerouteGroups;

            _database = new DatabaseService();
            _database.Initialize();

            _notifications = new NotificationService(Settings);

            EventBus.Instance.OnResult += OnMonitoringResult;
            EventBus.Instance.OnEvent += OnNodeEvent;

            ToggleMonitoringCommand = new RelayCommand(ToggleMonitoring);
            SaveCommand = new RelayCommand(Save);

            Nodes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasNodes));
        }

        private void ToggleMonitoring()
        {
            if (IsMonitoring)
            {
                _scheduler.Stop();
                IsMonitoring = false;
            }
            else
            {
                _scheduler.Start(Nodes);
                IsMonitoring = true;
            }
        }

        public void Save()
        {
            //_storage.SaveNodes(Nodes.ToList());
            _storage.SaveLinks(Links.ToList());
            _storage.SaveSettings(Settings); // оставил чисто для замочка
        }

        private void OnMonitoringResult(MonitoringResult result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var node = Nodes.FirstOrDefault(n => n.Id == result.NodeId);
                if (node == null) return;

                if (result.ModuleName == "ping")
                {
                    node.Status = result.Status;
                    node.LastPingMs = result.LatencyMs;
                    node.LastSeen = result.Timestamp;
                }

                UpdateCounters();
            });

            if (result.ModuleName == "ping")
                _database.SavePingResult(result);
            else
                _database.SaveModuleResult(result);

            if (result.ModuleName == "traceroute" && result.Hops?.Count > 0)
                TracerouteGroups.UpdateNodeHops(result.NodeId, result.Hops);
        }

        private void OnNodeEvent(NodeEvent nodeEvent)
        {
            _database.SaveEvent(nodeEvent);
            _notifications.OnNodeEvent(nodeEvent);
        }

        public void UpdateCounters()
        {
            OnlineCount = Nodes.Count(n => n.Status == NodeStatus.Online);
            OfflineCount = Nodes.Count(n => n.Status == NodeStatus.Offline);
            UnstableCount = Nodes.Count(n => n.Status == NodeStatus.Unstable);
        }

        public void UpdateSchedulerNodes()
        {
            if (IsMonitoring)
                _scheduler.UpdateNodes(Nodes);
        }

        public void UpdateSettings()
        {
            _moduleRegistry.UpdateSettings(Settings);
            _scheduler.DefaultIntervalSec = Settings.PingIntervalSeconds;
            _scheduler.ResetSchedule(); //Сброс расписания ping
            _notifications.UpdateSettings(Settings);
        }

        public void Shutdown()
        {
            EventBus.Instance.OnResult -= OnMonitoringResult;
            EventBus.Instance.OnEvent -= OnNodeEvent;
            _scheduler.Stop();
            _database.Dispose();
        }
    }
}
