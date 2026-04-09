using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using NetworkMonitor.ViewModels;

namespace NetworkMonitor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _toastTimer;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            DetailView.SetDatabase(_vm.Database);
            DetailView.DeleteRequested += (s, node) =>
            {
                _vm.Nodes.Remove(node);
                _vm.SelectedNode = null;
                _vm.UpdateSchedulerNodes();
                RefreshMarkers();
            };

            DetailView.PingRequested += async (s, node) =>
            {
                var pingModule = new Services.Modules.PingModule
                {
                    TimeoutMs = _vm.Settings.PingTimeoutMs,
                    Retries = _vm.Settings.PingRetries,
                    UnstableThresholdMs = _vm.Settings.UnstableThresholdMs
                };
                var result = await pingModule.CheckAsync(node, System.Threading.CancellationToken.None);
                Services.EventBus.Instance.PublishResult(result);
            };

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _refreshTimer.Tick += (s, e) =>
            {
                RefreshMarkers();
                _vm.UpdateCounters();
                if (_vm.SelectedNode != null)
                    DetailView.RefreshChart(_vm.SelectedNode);
            };
            _refreshTimer.Start();

            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _toastTimer.Tick += (s, e) =>
            {
                var expired = _vm.Notifications.Toasts
                    .Where(t => (DateTime.Now - t.CreatedAt).TotalSeconds > 5)
                    .ToList();
                foreach (var t in expired)
                    _vm.Notifications.Toasts.Remove(t);
            };
            _toastTimer.Start();
        }

        private void MainMap_Loaded(object sender, RoutedEventArgs e)
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            MainMap.MapProvider = OpenStreetMapProvider.Instance;
            MainMap.Position = new PointLatLng(_vm.Settings.MapLat, _vm.Settings.MapLon);
            MainMap.Zoom = _vm.Settings.MapZoom;
            MainMap.OnPositionChanged += MainMap_OnPositionChanged;
            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;

            MiniMap.MapProvider = OpenStreetMapProvider.Instance;
            MiniMap.Position = MainMap.Position;
            MiniMap.Zoom = Math.Max(1, MainMap.Zoom - 4);

            RefreshMarkers();
        }

        private void MainMap_OnPositionChanged(PointLatLng point)
        {
            MiniMap.Position = point;
        }

        private void MainMap_OnMapZoomChanged()
        {
            MiniMap.Zoom = Math.Max(1, MainMap.Zoom - 4);
        }

        private void RefreshMarkers()
        {
            MainMap.Markers.Clear();

            foreach (var link in _vm.Links)
            {
                var source = _vm.Nodes.FirstOrDefault(n => n.Id == link.SourceNodeId);
                var target = _vm.Nodes.FirstOrDefault(n => n.Id == link.TargetNodeId);
                if (source == null || target == null) continue;

                var points = new List<PointLatLng>
                {
                    new PointLatLng(source.Latitude, source.Longitude),
                    new PointLatLng(target.Latitude, target.Longitude)
                };

                var route = new GMapRoute(points);
                MainMap.Markers.Add(route);
                route.RegenerateShape(MainMap);

                if (route.Shape is System.Windows.Shapes.Path path)
                {
                    var worstStatus = (NodeStatus)Math.Max((int)source.Status, (int)target.Status);
                    Color lineColor;
                    switch (worstStatus)
                    {
                        case NodeStatus.Online: lineColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                        case NodeStatus.Unstable: lineColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                        case NodeStatus.Offline: lineColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                        default: lineColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                    }
                    path.Stroke = new SolidColorBrush(lineColor);
                    path.StrokeThickness = 2;
                    path.Opacity = 0.7;
                }
            }

            foreach (var node in _vm.Nodes)
            {
                Color markerColor;
                switch (node.Status)
                {
                    case NodeStatus.Online: markerColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                    case NodeStatus.Unstable: markerColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                    case NodeStatus.Offline: markerColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                    default: markerColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                }

                var iconKind = GetIconKind(node.DeviceType);

                var grid = new Grid { Width = 36, Height = 36 };
                grid.Children.Add(new Ellipse
                {
                    Fill = new SolidColorBrush(markerColor),
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Width = 36,
                    Height = 36
                });
                grid.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = iconKind,
                    Width = 18,
                    Height = 18,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });

                grid.ToolTip = $"{node.Name}\n{node.IpAddress}\n{node.Status} | {node.LastPingMs}ms";

                var capturedNode = node;
                grid.MouseLeftButtonDown += (s, ev) =>
                {
                    _vm.SelectedNode = capturedNode;
                    NodeListView.SelectedItem = capturedNode;
                };

                var marker = new GMapMarker(new PointLatLng(node.Latitude, node.Longitude))
                {
                    Shape = grid,
                    Offset = new Point(-18, -18)
                };
                MainMap.Markers.Add(marker);
            }
        }

        private MaterialDesignThemes.Wpf.PackIconKind GetIconKind(string deviceType)
        {
            switch (deviceType?.ToLower())
            {
                case "router": return MaterialDesignThemes.Wpf.PackIconKind.Router;
                case "server": return MaterialDesignThemes.Wpf.PackIconKind.Server;
                case "switch": return MaterialDesignThemes.Wpf.PackIconKind.LanConnect;
                case "camera": return MaterialDesignThemes.Wpf.PackIconKind.Camera;
                case "pc": return MaterialDesignThemes.Wpf.PackIconKind.Monitor;
                default: return MaterialDesignThemes.Wpf.PackIconKind.Devices;
            }
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddNodeDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultNode != null)
            {
                _vm.Nodes.Add(dialog.ResultNode);
                _vm.UpdateSchedulerNodes();
                RefreshMarkers();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(_vm.Settings, _vm.Database) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _vm.UpdateSettings();
                _vm.Save();
            }
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsVm = new StatisticsViewModel(_vm.Database, _vm.Nodes);
            var window = new StatisticsView(statsVm) { Owner = this };
            window.Show();
        }

        private void EventLogButton_Click(object sender, RoutedEventArgs e)
        {
            var logVm = new EventLogViewModel(_vm.Database, _vm.Nodes);
            var window = new EventLogView(logVm) { Owner = this };
            window.Show();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(_vm.Nodes);
            var filter = SearchBox.Text?.Trim().ToLower();
            if (string.IsNullOrEmpty(filter))
                view.Filter = null;
            else
                view.Filter = obj => obj is NetworkNode n &&
                    ((n.Name?.ToLower().Contains(filter) ?? false) ||
                     (n.IpAddress?.ToLower().Contains(filter) ?? false));
        }

        private void NodeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.SelectedNode != null)
                DetailView.RefreshChart(_vm.SelectedNode);
        }

        private void Toast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToastNotification toast)
                _vm.Notifications.RemoveToast(toast);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _refreshTimer.Stop();
            _toastTimer.Stop();
            _vm.Save();
            _vm.Shutdown();
        }
    }
}
