using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using NetworkMonitor.Models;
using NetworkMonitor.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkMonitor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainMap_Loaded(object sender, RoutedEventArgs e)
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            MainMap.MapProvider = GMapProviders.OpenStreetMap;
            MainMap.Position = new PointLatLng(55.75, 37.61); // Москва по умолчанию
            MainMap.MinZoom = 2;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 12;
            RefreshMarkers();
        }

        public void RefreshMarkers()
        {
            MainMap.Markers.Clear();

            foreach (var node in ViewModel.Nodes)
            {
                var marker = new GMapMarker(new PointLatLng(node.Latitude, node.Longitude));

                // Кружок-маркер
                var ellipse = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(node.StatusColor)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    ToolTip = $"{node.Name}\n{node.IpAddress}\nPing: {node.LastPingMs} мс"
                };

                ellipse.MouseLeftButtonDown += (s, e) =>
                {
                    ViewModel.SelectedNode = node;
                };

                marker.Shape = ellipse;
                marker.Offset = new System.Windows.Point(-9, -9);
                MainMap.Markers.Add(marker);
            }
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddNodeDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                ViewModel.Nodes.Add(dialog.NewNode);
                RefreshMarkers();
            }
        }

        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedNode != null)
            {
                ViewModel.Nodes.Remove(ViewModel.SelectedNode);
                ViewModel.SelectedNode = null;
                RefreshMarkers();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(ViewModel.Settings);
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}