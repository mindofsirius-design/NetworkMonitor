using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
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

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly StorageService _storage = new StorageService();
        private readonly PingService _pingService;

        private NetworkNode _selectedNode;
        private bool _isMonitoring;

        public ObservableCollection<NetworkNode> Nodes { get; set; }
        public ObservableCollection<NodeLink> Links { get; set; }
        public AppSettings Settings { get; set; }

        public NetworkNode SelectedNode
        {
            get => _selectedNode;
            set { _selectedNode = value; OnPropertyChanged(); }
        }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set { _isMonitoring = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonitoringLabel)); }
        }

        public string MonitoringLabel => IsMonitoring ? "⏹ Стоп" : "▶ Старт";

        public bool HasNodes => Nodes.Count > 0;

        public ICommand ToggleMonitoringCommand { get; }
        public ICommand SaveCommand { get; }

        public MainViewModel()
        {
            Settings = _storage.LoadSettings();
            Nodes = new ObservableCollection<NetworkNode>(_storage.LoadNodes());
            Links = new ObservableCollection<NodeLink>(_storage.LoadLinks());

            Nodes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasNodes));

            _pingService = new PingService(Settings);
            _pingService.NodeStatusChanged += node =>
            {
                // Обновление UI из другого потока
                App.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(Nodes)));
            };

            ToggleMonitoringCommand = new RelayCommand(() =>
            {
                if (IsMonitoring) { _pingService.Stop(); IsMonitoring = false; }
                else { _pingService.Start(Nodes); IsMonitoring = true; }
            });

            SaveCommand = new RelayCommand(() =>
            {
                _storage.SaveNodes(Nodes);
                _storage.SaveLinks(Links);
                _storage.SaveSettings(Settings);
            });
        }
    }
}
