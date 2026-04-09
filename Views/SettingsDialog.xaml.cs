using MaterialDesignThemes.Wpf;
using NetworkMonitor.Models;
using System.Windows;

namespace NetworkMonitor.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly AppSettings _settings;

        public SettingsDialog(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadSettings();
        }

        private void LoadSettings()
        {
            IntervalBox.Text = _settings.PingIntervalSeconds.ToString();
            TimeoutBox.Text = _settings.PingTimeoutMs.ToString();
            RetriesBox.Text = _settings.PingRetries.ToString();
            UnstableBox.Text = _settings.UnstableThresholdMs.ToString();
            DarkThemeToggle.IsChecked = _settings.DarkTheme;
            SoundToggle.IsChecked = _settings.SoundAlerts;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(IntervalBox.Text, out int interval) || interval < 1)
            {
                MessageBox.Show("Введите корректный интервал (секунды).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TimeoutBox.Text, out int timeout) || timeout < 100)
            {
                MessageBox.Show("Введите корректный таймаут (мин. 100 мс).", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(RetriesBox.Text, out int retries) || retries < 1)
            {
                MessageBox.Show("Введите корректное количество попыток.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(UnstableBox.Text, out int unstable) || unstable < 1)
            {
                MessageBox.Show("Введите корректный порог нестабильности.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _settings.PingIntervalSeconds = interval;
            _settings.PingTimeoutMs = timeout;
            _settings.PingRetries = retries;
            _settings.UnstableThresholdMs = unstable;
            _settings.DarkTheme = DarkThemeToggle.IsChecked == true;
            _settings.SoundAlerts = SoundToggle.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void DarkThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(new MaterialDesignDarkTheme());
            paletteHelper.SetTheme(theme);
        }

        private void DarkThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(new MaterialDesignLightTheme());
            paletteHelper.SetTheme(theme);
        }
    }
}