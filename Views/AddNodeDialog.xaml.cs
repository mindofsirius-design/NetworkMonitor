using NetworkMonitor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using MaterialDesignThemes.Wpf;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

namespace NetworkMonitor.Views
{
    public partial class AddNodeDialog : Window
    {
        public NetworkNode ResultNode { get; private set; }
        //Конструктор создания
        public AddNodeDialog(double lat = 0, double lon = 0)
        {
            InitializeComponent();

            if (lat != 0 || lon != 0)
            {
                LatBox.Text = lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                LonBox.Text = lon.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            }

            //Отрисовка кнопки "Добавить" в зависимости от установленной темы
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            bool isDark = theme.GetBaseTheme() == BaseTheme.Dark;
            var brush = new SolidColorBrush(isDark ? Colors.Black : Colors.White);
            AddButton.Foreground = brush;
        }
        //Конструктор редактирования
        public AddNodeDialog(NetworkNode existingNode)
        {
            InitializeComponent();

            NameBox.Text = existingNode.Name;
            IpBox.Text = existingNode.IpAddress;
            DescBox.Text = existingNode.Description;
            LatBox.Text = existingNode.Latitude.ToString("F6", CultureInfo.InvariantCulture);
            LonBox.Text = existingNode.Longitude.ToString("F6", CultureInfo.InvariantCulture);

            // Тип устройства
            foreach (ComboBoxItem item in TypeBox.Items)
                if (item.Content?.ToString() == existingNode.DeviceType)
                { TypeBox.SelectedItem = item; break; }

            // Ping
            PingIntervalBox.Text = existingNode.Monitoring?.Ping?.IntervalSec.ToString() ?? "5";

            // TCP
            TcpToggle.IsChecked = existingNode.Monitoring?.Tcp?.Enabled ?? false;
            TcpPortsBox.Text = string.Join(", ", existingNode.Monitoring?.Tcp?.Ports ?? new List<int>());

            // SNMP
            SnmpToggle.IsChecked = existingNode.Monitoring?.Snmp?.Enabled ?? false;
            SnmpCommunityBox.Text = existingNode.Monitoring?.Snmp?.Community ?? "public";
            SnmpVersionBox.SelectedIndex = existingNode.Monitoring?.Snmp?.Version == "1" ? 0 : 1;

            // Traceroute
            TracerouteToggle.IsChecked = existingNode.Monitoring?.Traceroute?.Enabled ?? false;

            // Сохранить ID
            _editingId = existingNode.Id;
            Title = "Редактировать узел";
            AddButton.Content = "Сохранить";
        }
        private string _editingId = null;

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("\u0412\u0432\u0435\u0434\u0438\u0442\u0435 \u0438\u043C\u044F \u0443\u0441\u0442\u0440\u043E\u0439\u0441\u0442\u0432\u0430.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IpBox.Text) || !System.Net.IPAddress.TryParse(IpBox.Text.Trim(), out _))
            {
                MessageBox.Show("Введите корректный IP-адрес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (_editingId != null)
                node.Id = _editingId;

            ResultNode = node;
            DialogResult = true;
            Close();
        }

        private void IpBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var newText = tb.Text.Insert(tb.CaretIndex, e.Text);
            e.Handled = !IsValidIpInput(newText);
        }

        private void IpBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsValidIpInput(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }

        private bool IsValidIpInput(string text)
        {
            // только цифры и точки
            foreach (char c in text)
                if (!char.IsDigit(c) && c != '.') return false;

            var parts = text.Split('.');

            // не более 4 октетов
            if (parts.Length > 4) return false;

            foreach (var part in parts)
            {
                // каждый октет не более 3 цифр
                if (part.Length > 3) return false;
                // значение 0-255
                if (part.Length > 0 && int.TryParse(part, out int val) && val > 255) return false;
            }

            return true;
        }

        private void IpBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var text = IpBox.Text.Trim();
            if (!string.IsNullOrEmpty(text) && !System.Net.IPAddress.TryParse(text, out _))
            {
                IpBox.BorderBrush = System.Windows.Media.Brushes.Red;
                IpBox.ToolTip = "Некорректный IP-адрес";
            }
            else
            {
                IpBox.ClearValue(BorderBrushProperty);
                IpBox.ToolTip = null;
            }
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
