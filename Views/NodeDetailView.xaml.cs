using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NetworkMonitor.Views
{
    public partial class NodeDetailView : UserControl
    {
        public event EventHandler<NetworkNode> DeleteRequested;
        public event EventHandler<NetworkNode> PingRequested;

        private DatabaseService _database;

        public NodeDetailView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public void SetDatabase(DatabaseService database)
        {
            _database = database;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is NetworkNode node && _database != null)
            {
                RefreshChart(node);
            }
        }

        public void RefreshChart(NetworkNode node)
        {
            if (_database == null || node == null) return;

            var history = _database.GetPingHistory(node.Id, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow);

            var model = new PlotModel();
            model.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm",
                IsAxisVisible = true,
                MajorGridlineStyle = LineStyle.Dot
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Title = "ms"
            });

            var series = new LineSeries
            {
                Color = OxyColors.CornflowerBlue,
                StrokeThickness = 2
            };

            var validPoints = history.Where(h => h.LatencyMs >= 0).ToList();
            foreach (var r in validPoints.Skip(Math.Max(0, validPoints.Count - 100)))
            {
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(r.Timestamp), r.LatencyMs));
            }

            model.Series.Add(series);
            LatencyChart.Model = model;
        }

        private void PingNow_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkNode node)
                PingRequested?.Invoke(this, node);
        }

        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkNode node)
            {
                var result = MessageBox.Show(
                    $"\u0423\u0434\u0430\u043B\u0438\u0442\u044C \u0443\u0437\u0435\u043B \"{node.Name}\"?",
                    "\u041F\u043E\u0434\u0442\u0432\u0435\u0440\u0436\u0434\u0435\u043D\u0438\u0435",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    DeleteRequested?.Invoke(this, node);
            }
        }
    }
}
