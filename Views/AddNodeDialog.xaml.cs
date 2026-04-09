using NetworkMonitor.Models;
using System;
using System.Windows;

namespace NetworkMonitor.Views
{
    public partial class AddNodeDialog : Window
    {
        public NetworkNode NewNode { get; private set; }

        public AddNodeDialog()
        {
            InitializeComponent();
            TypeBox.SelectedIndex = 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите название устройства.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IpBox.Text))
            {
                MessageBox.Show("Введите IP-адрес.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseCoordinate(LatBox.Text, out double lat) ||
                lat < -90 || lat > 90)
            {
                MessageBox.Show("Введите корректную широту (от -90 до 90).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseCoordinate(LonBox.Text, out double lon) ||
                lon < -180 || lon > 180)
            {
                MessageBox.Show("Введите корректную долготу (от -180 до 180).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedType = (TypeBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString()
                               ?? "Другое";

            NewNode = new NetworkNode
            {
                Name = NameBox.Text.Trim(),
                IpAddress = IpBox.Text.Trim(),
                DeviceType = selectedType,
                Latitude = lat,
                Longitude = lon,
                Description = DescBox.Text.Trim(),
                Status = NodeStatus.Unknown
            };

            DialogResult = true;
            Close();
        }

        private bool TryParseCoordinate(string text, out double result)
        {
            // Поддержка как точки так и запятой в качестве разделителя
            text = text?.Trim().Replace(',', '.');
            return double.TryParse(text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out result);
        }
    }
}