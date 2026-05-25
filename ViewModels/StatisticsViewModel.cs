using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using MaterialDesignThemes.Wpf;

namespace NetworkMonitor.ViewModels
{
    public class UptimeRow
    {
        public string NodeName { get; set; }
        public string Uptime24h { get; set; }
        public string Uptime7d { get; set; }
        public string Uptime30d { get; set; }
    }

    public class StatisticsViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly ObservableCollection<NetworkNode> _nodes;

        public PlotModel LatencyPlot { get; private set; }
        public PlotModel PacketLossPlot { get; private set; }
        public ObservableCollection<UptimeRow> UptimeRows { get; } = new ObservableCollection<UptimeRow>();

        private int _onlineCount;
        public int OnlineCount { get => _onlineCount; set { _onlineCount = value; OnPropertyChanged(); } }

        private int _offlineCount;
        public int OfflineCount { get => _offlineCount; set { _offlineCount = value; OnPropertyChanged(); } }

        private int _unstableCount;
        public int UnstableCount { get => _unstableCount; set { _unstableCount = value; OnPropertyChanged(); } }

        private string _selectedPeriod = "24h";
        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set { _selectedPeriod = value; OnPropertyChanged(); Refresh(); }
        }

        public List<string> Periods { get; } = new List<string> { "1h", "24h", "7d", "30d" };

        public StatisticsViewModel(DatabaseService database, ObservableCollection<NetworkNode> nodes)
        {
            _database = database;
            _nodes = nodes;
            Refresh();
        }

        public void Refresh()
        {
            OnlineCount = _nodes.Count(n => n.Status == NodeStatus.Online);
            OfflineCount = _nodes.Count(n => n.Status == NodeStatus.Offline);
            UnstableCount = _nodes.Count(n => n.Status == NodeStatus.Unstable);

            var range = GetPeriodRange();
            BuildLatencyPlot(range.Item1, range.Item2);
            BuildPacketLossPlot(range.Item1, range.Item2);
            BuildUptimeTable();
        }

        private Tuple<DateTime, DateTime> GetPeriodRange()
        {
            var to = DateTime.UtcNow;
            DateTime from;
            switch (_selectedPeriod)
            {
                case "1h": from = to.AddHours(-1); break;
                case "7d": from = to.AddDays(-7); break;
                case "30d": from = to.AddDays(-30); break;
                default: from = to.AddHours(-24); break;
            }
            return Tuple.Create(from, to);
        }

        private void BuildLatencyPlot(DateTime from, DateTime to)
        {
            // Определяем тёмную тему
            var helper = new MaterialDesignThemes.Wpf.PaletteHelper();
            var theme = helper.GetTheme();
            bool isDark = theme.GetBaseTheme() == MaterialDesignThemes.Wpf.BaseTheme.Dark;
            var bgColor = isDark ? OxyColor.FromRgb(30, 30, 30) : OxyColors.White;
            var textColor = isDark ? OxyColors.LightGray : OxyColors.Black;
            var gridColor = isDark ? OxyColor.FromRgb(60, 60, 60) : OxyColor.FromRgb(220, 220, 220);

            //Создаем полотно
            var model = new PlotModel { Title = "Задержка (мс)",
                                        Background = bgColor,
                                        PlotAreaBackground = bgColor,
                                        TextColor = textColor,
                                        PlotAreaBorderColor = gridColor
            };
            //model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "HH:mm" });
            //model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0 });

            model.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm",
                AxislineColor = textColor,
                TextColor = textColor,
                TicklineColor = textColor,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridColor
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                AxislineColor = textColor,
                TextColor = textColor,
                TicklineColor = textColor,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = gridColor
            });

            // Собираем все точки по всем узлам, группируем по минуте, берём среднее
            var allPoints = new Dictionary<DateTime, List<long>>();

            foreach (var node in _nodes)
            {
                var history = _database.GetPingHistory(node.Id, from, to);
                foreach (var r in history.Where(h => h.LatencyMs >= 0))
                {
                    var bucket = new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day,
                                              r.Timestamp.Hour, r.Timestamp.Minute, 0, DateTimeKind.Utc);
                    if (!allPoints.ContainsKey(bucket))
                        allPoints[bucket] = new List<long>();
                    allPoints[bucket].Add(r.LatencyMs);
                }
            }

            if (allPoints.Count > 0)
            {
                var series = new LineSeries { Title = "Среднее",
                                              TrackerFormatString = "{0}\nВремя: {2:HH:mm}\nЗадержка: {4:F1} мс" };
                foreach (var kvp in allPoints.OrderBy(k => k.Key))
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(kvp.Key), kvp.Value.Average()));
                model.Series.Add(series);
            }

            LatencyPlot = model;
            OnPropertyChanged(nameof(LatencyPlot));
        }

        private void BuildPacketLossPlot(DateTime from, DateTime to)
        {
            // Определяем тёмную тему
            var helper = new MaterialDesignThemes.Wpf.PaletteHelper();
            var theme = helper.GetTheme();
            bool isDark = theme.GetBaseTheme() == MaterialDesignThemes.Wpf.BaseTheme.Dark;
           
            var bgColor = isDark ? OxyColor.FromRgb(30, 30, 30) : OxyColors.White;
            var textColor = isDark ? OxyColors.LightGray : OxyColors.Black;
            var gridColor = isDark ? OxyColor.FromRgb(60, 60, 60) : OxyColor.FromRgb(220, 220, 220);
            
            //Создаем полотно
            var model = new PlotModel { Title = "Потери пакетов (%)",              
                                        Background = bgColor,
                                        PlotAreaBackground = bgColor,
                                        TextColor = textColor,
                                        PlotAreaBorderColor = gridColor
            };
            model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "HH:mm" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100 });

            var allPoints = new Dictionary<DateTime, List<double>>();

            foreach (var node in _nodes)
            {
                var history = _database.GetPingHistory(node.Id, from, to);
                foreach (var r in history)
                {
                    var bucket = new DateTime(r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day,
                                              r.Timestamp.Hour, r.Timestamp.Minute, 0, DateTimeKind.Utc);
                    if (!allPoints.ContainsKey(bucket))
                        allPoints[bucket] = new List<double>();
                    allPoints[bucket].Add(r.PacketLoss * 100);
                }
            }

            if (allPoints.Count > 0)
            {
                var series = new LineSeries { Title = "Среднее",
                                              TrackerFormatString = "{0}\nВремя: {2:HH:mm}\nПотери: {4:F1} %"
                };
                foreach (var kvp in allPoints.OrderBy(k => k.Key))
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(kvp.Key), kvp.Value.Average()));
                model.Series.Add(series);
            }

            PacketLossPlot = model;
            OnPropertyChanged(nameof(PacketLossPlot));
        }

        private void BuildUptimeTable()
        {
            UptimeRows.Clear();
            var now = DateTime.UtcNow;

            foreach (var node in _nodes)
            {
                UptimeRows.Add(new UptimeRow
                {
                    NodeName = node.Name,
                    Uptime24h = $"{_database.GetUptime(node.Id, now.AddHours(-24), now):F1}%",
                    Uptime7d = $"{_database.GetUptime(node.Id, now.AddDays(-7), now):F1}%",
                    Uptime30d = $"{_database.GetUptime(node.Id, now.AddDays(-30), now):F1}%"
                });
            }
        }
    }
}
