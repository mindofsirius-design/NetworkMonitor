using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using NetworkMonitor.Models;

namespace NetworkMonitor.Views
{
    public partial class AddNodeDialog : Window
    {
        public NetworkNode ResultNode { get; private set; }

        public AddNodeDialog(double lat = 0, double lon = 0)
        {
            InitializeComponent();
            if (lat != 0 || lon != 0)
            {
                LatBox.Text = lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                LonBox.Text = lon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u0438\u043C\u044F \u0443\u0441\u0442\u0440\u043E\u0439\u0441\u0442\u0432\u0430.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IpBox.Text))
            {
                MessageBox.Show("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 IP-\u0430\u0434\u0440\u0435\u0441.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseCoordinate(LatBox.Text, -90, 90, out double lat))
            {
                MessageBox.Show("\u041D\u0435\u043A\u043E\u0440\u0440\u0435\u043A\u0442\u043D\u0430\u044F \u0448\u0438\u0440\u043E\u0442\u0430 (-90..90).", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseCoordinate(LonBox.Text, -180, 180, out double lon))
            {
                MessageBox.Show("\u041D\u0435\u043A\u043E\u0440\u0440\u0435\u043A\u0442\u043D\u0430\u044F \u0434\u043E\u043B\u0433\u043E\u0442\u0430 (-180..180).", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PingIntervalBox.Text, out int pingInterval) || pingInterval < 1)
                pingInterval = 5;

            var ports = new List<int>();
            if (TcpToggle.IsChecked == true)
            {
                foreach (var part in TcpPortsBox.Text.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int port) && port > 0 && port <= 65535)
                        ports.Add(port);
                }
            }

            string snmpVersion = (SnmpVersionBox.SelectedIndex == 0) ? "1" : "2c";

            var node = new NetworkNode
            {
                Name = NameBox.Text.Trim(),
                IpAddress = IpBox.Text.Trim(),
                DeviceType = ((System.Windows.Controls.ComboBoxItem)TypeBox.SelectedItem)?.Content?.ToString() ?? "Other",
                Latitude = lat,
                Longitude = lon,
                Description = DescBox.Text?.Trim() ?? "",
                Status = NodeStatus.Unknown,
                Monitoring = new MonitoringConfig
                {
                    Ping = new PingConfig { Enabled = true, IntervalSec = pingInterval },
                    Tcp = new TcpConfig { Enabled = TcpToggle.IsChecked == true, Ports = ports.Any() ? ports : new List<int> { 80, 443 } },
                    Snmp = new SnmpConfig
                    {
                        Enabled = SnmpToggle.IsChecked == true,
                        Community = SnmpCommunityBox.Text?.Trim() ?? "public",
                        Version = snmpVersion
                    },
                    Traceroute = new TracerouteConfig { Enabled = TracerouteToggle.IsChecked == true }
                }
            };

            ResultNode = node;
            DialogResult = true;
            Close();
        }

        private bool TryParseCoordinate(string text, double min, double max, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Replace(',', '.');
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return false;
            return value >= min && value <= max;
        }
    }
}
