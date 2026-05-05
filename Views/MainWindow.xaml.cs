using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, cert, chain, errors) => true;

            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            DetailView.SetDatabase(_vm.Database);
            DetailView.DeleteRequested += (s, node) =>
            {
                _vm.Nodes.Remove(node);
                _vm.SelectedNode = null;
                _vm.UpdateSchedulerNodes();
                RefreshMarkers(_vm.SelectedNode?.Id);
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
                RefreshMarkers(_vm.SelectedNode?.Id);
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
            // GMap.NET 2.x: вместо GMaps.Instance.Mode используется статическое свойство
            GMap.NET.GMaps.Instance.Mode = AccessMode.ServerAndCache;

            //MainMap.MapProvider = OpenStreetMapProvider.Instance;
            //MainMap.MapProvider = GMap.NET.MapProviders.BingMapProvider.Instance;
            MainMap.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;

            MainMap.Position = new PointLatLng(_vm.Settings.MapLat, _vm.Settings.MapLon);
            MainMap.Zoom = _vm.Settings.MapZoom;

            MainMap.MouseLeftButtonDown += MainMap_MouseLeftButtonDown;
            RefreshMarkers(_vm.SelectedNode?.Id);
        }

        private void RefreshMarkers(string selectedNodeId = null)
        {
            MainMap.Markers.Clear();

            // Связи рисуем через кастомные маркеры-линии
            foreach (var link in _vm.Links)
            {
                var source = _vm.Nodes.FirstOrDefault(n => n.Id == link.SourceNodeId);
                var target = _vm.Nodes.FirstOrDefault(n => n.Id == link.TargetNodeId);
                if (source == null || target == null) continue;

                var worstStatus = (NodeStatus)Math.Max((int)source.Status, (int)target.Status);
                Color lineColor;
                switch (worstStatus)
                {
                    case NodeStatus.Online: lineColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                    case NodeStatus.Unstable: lineColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                    case NodeStatus.Offline: lineColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                    default: lineColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                }

                // Используем GMapRoute с одним аргументом — список точек
                var points = new List<PointLatLng>
        {
            new PointLatLng(source.Latitude, source.Longitude),
            new PointLatLng(target.Latitude, target.Longitude)
        };

                var route = new GMapRoute(points);
                route.Shape = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2,
                    Opacity = 0.7
                };
                MainMap.Markers.Add(route);
            }

            // Узлы
            foreach (var node in _vm.Nodes)
            {
                bool isSelected = node.Id == selectedNodeId;
                double size = isSelected ? 44 : 36;
                double iconSize = isSelected ? 22 : 18;

                Color markerColor;
                switch (node.Status)
                {
                    case NodeStatus.Online: markerColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                    case NodeStatus.Unstable: markerColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                    case NodeStatus.Offline: markerColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                    default: markerColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                }

                var iconKind = GetIconKind(node.DeviceType);
                var grid = new Grid { Width = size, Height = size };
                grid.Children.Add(new Ellipse
                {
                    Fill = new SolidColorBrush(markerColor),
                    Stroke = isSelected ? Brushes.Yellow : Brushes.White,
                    StrokeThickness = isSelected ? 3 : 2,
                    Width = size,
                    Height = size
                });
                grid.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = iconKind,
                    Width = iconSize,
                    Height = iconSize,
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
                    ev.Handled = true;
                };

                var marker = new GMapMarker(new PointLatLng(node.Latitude, node.Longitude))
                {
                    Shape = grid,
                    Offset = new Point(-size / 2, -size / 2)
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
            var center = MainMap.Position; // GMap.NET.PointLatLng
            var dialog = new AddNodeDialog(center.Lat, center.Lng)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
            {
                //_viewModel.Nodes.Add(dialog.ResultNode);
                //_viewModel.UpdateSchedulerNodes();
                //RefreshMapMarkers();

                _vm.Nodes.Add(dialog.ResultNode);
                _vm.UpdateSchedulerNodes();
                RefreshMarkers(_vm.SelectedNode?.Id);
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
        private void SaveMapPositionButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.Settings.DefaultMapZoom = MainMap.Zoom;
            _vm.Settings.DefaultMapLat = MainMap.Position.Lat;
            _vm.Settings.DefaultMapLon = MainMap.Position.Lng;
            _vm.Save();
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
        private void MainMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Если клик пришёл именно на карту, а не всплыл от маркера
            if (e.OriginalSource is GMapControl || e.OriginalSource is Image ||
                e.OriginalSource is System.Windows.Shapes.Path)
            {
                _vm.SelectedNode = null;
                NodeListView.SelectedItem = null;
                RefreshMarkers(null);

                // Возврат на позицию по умолчанию
                MainMap.Position = new PointLatLng(_vm.Settings.DefaultMapLat, _vm.Settings.DefaultMapLon);
                MainMap.Zoom = _vm.Settings.DefaultMapZoom;
            }
        }
        private void NodeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.SelectedNode != null)
            {
                if (_vm.Settings.CenterMapOnSelect)
                    MainMap.Position = new PointLatLng(_vm.SelectedNode.Latitude, _vm.SelectedNode.Longitude);

                if (_vm.Settings.ZoomOnSelect)
                    MainMap.Zoom = 12;
            }
            RefreshMarkers(_vm.SelectedNode?.Id);
            
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

        private void DetailView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}