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
using MaterialDesignThemes.Wpf;
using System.Runtime.InteropServices;

namespace NetworkMonitor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _toastTimer;
        private Point _crosshairPos; // позиция перекрестия в пикселях
        private bool _crosshairInitialized = false;

        //Для изменения шапки программы
        [DllImport("dwmapi.dll", PreserveSig = true)]   
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public MainWindow()
        {
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, cert, chain, errors) => true;

            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            ApplyMapLock(_vm.Settings.MapLocked);

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
            UpdateToolbarForeground(_vm.Settings.DarkTheme);
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
            MainMap.ShowCenter = false;
            MainMap.MouseLeftButtonDown += MainMap_MouseLeftButtonDown;
            MainMap.PreviewMouseWheel += (s, ev) =>
            {
                if (_vm.Settings.MapLocked)
                    ev.Handled = true;
            };
            RefreshMarkers(_vm.SelectedNode?.Id);
            UpdateCrosshairPosition();
        }

        private void ResetMapPositionButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;
            MainMap.Position = new GMap.NET.PointLatLng(vm.Settings.DefaultMapLat, vm.Settings.DefaultMapLon);
            MainMap.Zoom = vm.Settings.DefaultMapZoom;
        }

        private void ApplyMapLock(bool locked)
        {
            MainMap.CanDragMap = !locked;
            MainMap.MouseWheelZoomEnabled = !locked;
            LockIcon.Kind = locked
                ? MaterialDesignThemes.Wpf.PackIconKind.Lock
                : MaterialDesignThemes.Wpf.PackIconKind.LockOpenVariant;
            LockBadge.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
            CrosshairCanvas.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LockMapButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.Settings.MapLocked = !_vm.Settings.MapLocked;
            ApplyMapLock(_vm.Settings.MapLocked);
        }

        private void MainMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // Если карта не заблокирована — перемещаем перекрестие
            if (!vm.Settings.MapLocked)
            {
                _crosshairPos = e.GetPosition(MainMap);
                UpdateCrosshairVisual();
                e.Handled = false; // не мешаем drag карты
                return;
            }
        }

        private void MainMap_MouseMove(object sender, MouseEventArgs e) { }

        private void MainMap_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCrosshairVisual();
        }

        private void UpdateCrosshairPosition()
        {
            if (!_crosshairInitialized && MainMap.ActualWidth > 0)
            {
                _crosshairPos = new Point(MainMap.ActualWidth / 2, MainMap.ActualHeight / 2);
                _crosshairInitialized = true;
                UpdateCrosshairVisual();
            }
        }

        private void UpdateCrosshairVisual()
        {
            double w = MainMap.ActualWidth;
            double h = MainMap.ActualHeight;
            if (w == 0 || h == 0) return;

            CrossH.X1 = 0; CrossH.Y1 = _crosshairPos.Y;
            CrossH.X2 = w; CrossH.Y2 = _crosshairPos.Y;

            CrossV.X1 = _crosshairPos.X; CrossV.Y1 = 0;
            CrossV.X2 = _crosshairPos.X; CrossV.Y2 = h;

            Canvas.SetLeft(CrossDot, _crosshairPos.X - 4);
            Canvas.SetTop(CrossDot, _crosshairPos.Y - 4);
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
                double baseSize = _vm.Settings.MarkerSize;
                double size = isSelected ? baseSize * 1.22 : baseSize;
                double iconSize = size * 0.5;
                double opacity = isSelected? 1 : _vm.Settings.MarkerOpacity;

                Color markerColor;
                switch (node.Status)
                {
                    case NodeStatus.Online: markerColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                    case NodeStatus.Unstable: markerColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                    case NodeStatus.Offline: markerColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                    default: markerColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                }

                var iconKind = GetIconKind(node.DeviceType);
                var grid = new Grid { Width = size, Height = size, Opacity = opacity };
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
            var vm = DataContext as MainViewModel;
            if (vm.Settings.MapLocked)
            {
                vm.Notifications.ShowToast("Карта заблокирована. Разблокируйте для добавления узлов.", ToastType.Warning);
                return;
            }

            var latLng = MainMap.FromLocalToLatLng((int)_crosshairPos.X, (int)_crosshairPos.Y);

            var dialog = new AddNodeDialog(latLng.Lat, latLng.Lng) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                //_viewModel.Nodes.Add(dialog.ResultNode);
                //_viewModel.UpdateSchedulerNodes();
                //RefreshMapMarkers();

                _vm.Nodes.Add(dialog.ResultNode); //Добавляется новый узел в коллекцию
                _vm.UpdateSchedulerNodes();
                RefreshMarkers(_vm.SelectedNode?.Id);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(_vm.Settings, _vm.Database, this) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _vm.UpdateSettings();
                _vm.Save();
                RefreshMarkers();
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
            var result = MessageBox.Show(
                "Сохранить текущее положение карты как позицию по умолчанию?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            vm.Settings.DefaultMapLat = MainMap.Position.Lat;
            vm.Settings.DefaultMapLon = MainMap.Position.Lng;
            vm.Settings.DefaultMapZoom = MainMap.Zoom;
            vm.Save();
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
            _vm.Settings.MapLat = MainMap.Position.Lat;
            _vm.Settings.MapLon = MainMap.Position.Lng;
            _vm.Settings.MapZoom = MainMap.Zoom;
            _refreshTimer.Stop();
            _toastTimer.Stop();
            _vm.Save();
            _vm.Shutdown();
        }

        public void UpdateToolbarForeground(bool isDark)
        {
            var brush = new SolidColorBrush(isDark ? Colors.Black : Colors.White);

            //Для надписи лого
            var logoText = HeaderPanel.FindName("LogoText") as TextBlock;
            if (logoText != null)
                logoText.Foreground = brush;

            //Для текста внутри кнопок
            foreach (var btn in HeaderPanel.Children.OfType<Button>())
                btn.Foreground = brush;

            //Для всех иконок
            foreach (var icon in HeaderPanel.Children.OfType<PackIcon>())
                icon.Foreground = brush;

            ApplyDarkTitleBar(isDark);  //для шапки окна программы

        }
        public void ApplyDarkTitleBar(bool dark)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int value = dark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, 19, ref value, sizeof(int));
        }

        private void DetailView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}