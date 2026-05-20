using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows;
using MaterialDesignThemes.Wpf;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using NetworkMonitor.ViewModels;
using System.Windows.Input;
using System.Windows.Media;

namespace NetworkMonitor.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly AppSettings _settings;
        private DatabaseService _database;
        private readonly MainWindow _mainWindow; //Для темной темы для букв сверху

        public SettingsDialog(AppSettings settings, DatabaseService database = null, MainWindow mainWindow = null)
        {
            InitializeComponent();
            _settings = settings;
            _database = database;
            _mainWindow = mainWindow;
            LoadSettings();
            Loaded += (s, e) =>     //для корректного выделения элементов
            {
                MainTabControl.SelectionChanged += (s2, e2) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FocusManager.SetFocusedElement(MainTabControl, null);
                        Keyboard.ClearFocus();
                    }), System.Windows.Threading.DispatcherPriority.Input);
                };
            };

            //Отрисовка кнопки "Сохранить" в зависимости от установленной темы
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            bool isDark = theme.GetBaseTheme() == BaseTheme.Dark;
            var brush = new SolidColorBrush(isDark ? Colors.Black : Colors.White);
            SaveButton.Foreground = brush;

        }

        private void LoadSettings()
        {
            IntervalBox.Text = _settings.PingIntervalSeconds.ToString();
            TimeoutBox.Text = _settings.PingTimeoutMs.ToString();
            RetriesBox.Text = _settings.PingRetries.ToString();
            UnstableBox.Text = _settings.UnstableThresholdMs.ToString();

            ToastToggle.IsChecked = _settings.ToastEnabled;
            SoundToggle.IsChecked = _settings.SoundEnabled;
            SoundDebounceBox.Text = _settings.SoundDebounceSec.ToString();

            EmailToggle.IsChecked = _settings.EmailEnabled;
            SmtpHostBox.Text = _settings.SmtpHost;
            SmtpPortBox.Text = _settings.SmtpPort.ToString();
            SmtpUserBox.Text = _settings.SmtpUser;
            SmtpPasswordBox.Password = _settings.SmtpPassword;
            EmailFromBox.Text = _settings.EmailFrom;
            EmailToBox.Text = _settings.EmailTo;

            //Оформление
            DarkThemeToggle.IsChecked = _settings.DarkTheme;
            MarkerSizeSlider.Value = _settings.MarkerSize;
            MarkerOpacitySlider.Value = _settings.MarkerOpacity;

            KeepDaysBox.Text = _settings.HistoryKeepDays.ToString();

            //Вкладка "Управление"
            CenterMapOnSelectToggle.IsChecked = _settings.CenterMapOnSelect;
            ZoomOnSelectToggle.IsChecked = _settings.ZoomOnSelect;

            TimeZoneOffsetBox.Text = _settings.TimeZoneOffsetHours.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(IntervalBox.Text, out int interval) || interval < 1)
            {
                MessageBox.Show("\u0418\u043D\u0442\u0435\u0440\u0432\u0430\u043B \u0434\u043E\u043B\u0436\u0435\u043D \u0431\u044B\u0442\u044C >= 1 \u0441\u0435\u043A.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TimeoutBox.Text, out int timeout) || timeout < 100)
            {
                MessageBox.Show("\u0422\u0430\u0439\u043C\u0430\u0443\u0442 \u0434\u043E\u043B\u0436\u0435\u043D \u0431\u044B\u0442\u044C >= 100 \u043C\u0441.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(RetriesBox.Text, out int retries) || retries < 1)
            {
                MessageBox.Show("\u041A\u043E\u043B-\u0432\u043E \u043F\u043E\u043F\u044B\u0442\u043E\u043A \u0434\u043E\u043B\u0436\u043D\u043E \u0431\u044B\u0442\u044C >= 1.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(UnstableBox.Text, out int unstable) || unstable < 1)
            {
                MessageBox.Show("\u041F\u043E\u0440\u043E\u0433 \u0434\u043E\u043B\u0436\u0435\u043D \u0431\u044B\u0442\u044C >= 1 \u043C\u0441.", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _settings.PingIntervalSeconds = interval;
            _settings.PingTimeoutMs = timeout;
            _settings.PingRetries = retries;
            _settings.UnstableThresholdMs = unstable;

            _settings.ToastEnabled = ToastToggle.IsChecked == true;
            _settings.SoundEnabled = SoundToggle.IsChecked == true;

            if (int.TryParse(SoundDebounceBox.Text, out int debounce) && debounce >= 1)
                _settings.SoundDebounceSec = debounce;

            _settings.EmailEnabled = EmailToggle.IsChecked == true;
            _settings.SmtpHost = SmtpHostBox.Text?.Trim() ?? "";

            if (int.TryParse(SmtpPortBox.Text, out int port)) _settings.SmtpPort = port;
            _settings.SmtpUser = SmtpUserBox.Text?.Trim() ?? "";
            _settings.SmtpPassword = SmtpPasswordBox.Password ?? "";
            _settings.EmailFrom = EmailFromBox.Text?.Trim() ?? "";
            _settings.EmailTo = EmailToBox.Text?.Trim() ?? "";
            
            //Вкладка "Оформление"
            _settings.DarkTheme = DarkThemeToggle.IsChecked == true;
            _settings.MarkerSize = (int)MarkerSizeSlider.Value;
            _settings.MarkerOpacity = MarkerOpacitySlider.Value;

            if (int.TryParse(KeepDaysBox.Text, out int keepDays) && keepDays >= 1)
                _settings.HistoryKeepDays = keepDays;

            //Вкладка "Управление"
            _settings.CenterMapOnSelect = CenterMapOnSelectToggle.IsChecked == true;
            _settings.ZoomOnSelect = ZoomOnSelectToggle.IsChecked == true;
            
            //Вкладка "Данные"
            if (int.TryParse(TimeZoneOffsetBox.Text, out int tz))
            {
                _settings.TimeZoneOffsetHours = tz;
                UtcToLocalConverter.OffsetHours = tz;
            }

            //Тема тема
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            theme.SetBaseTheme(DarkThemeToggle.IsChecked == true ? BaseTheme.Dark : BaseTheme.Light);
            helper.SetTheme(theme);
            _mainWindow?.UpdateToolbarForeground(DarkThemeToggle.IsChecked == true);

            DialogResult = true;
            Close();
        }

        private async void TestEmail_Click(object sender, RoutedEventArgs e)
        {
            var host = SmtpHostBox.Text?.Trim();
            var port = int.TryParse(SmtpPortBox.Text, out int p) ? p : 587;
            var user = SmtpUserBox.Text?.Trim();
            var pass = SmtpPasswordBox.Password;
            var from = EmailFromBox.Text?.Trim();
            var to = EmailToBox.Text?.Trim();

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(user, pass);
                        client.Timeout = 10000;
                        using (var msg = new MailMessage(from, to,
                            "[NetworkMonitor] \u0422\u0435\u0441\u0442\u043E\u0432\u043E\u0435 \u0441\u043E\u043E\u0431\u0449\u0435\u043D\u0438\u0435",
                            "\u0415\u0441\u043B\u0438 \u0432\u044B \u0432\u0438\u0434\u0438\u0442\u0435 \u044D\u0442\u043E \u043F\u0438\u0441\u044C\u043C\u043E \u2014 email-\u0443\u0432\u0435\u0434\u043E\u043C\u043B\u0435\u043D\u0438\u044F \u0440\u0430\u0431\u043E\u0442\u0430\u044E\u0442."))
                        {
                            client.Send(msg);
                        }
                    }
                });
                MessageBox.Show("\u0422\u0435\u0441\u0442\u043E\u0432\u043E\u0435 \u043F\u0438\u0441\u044C\u043C\u043E \u043E\u0442\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u043E!", "\u0423\u0441\u043F\u0435\u0445", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"\u041E\u0448\u0438\u0431\u043A\u0430: {ex.Message}", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("\u0423\u0434\u0430\u043B\u0438\u0442\u044C \u0432\u0441\u044E \u0438\u0441\u0442\u043E\u0440\u0438\u044E?", "\u041F\u043E\u0434\u0442\u0432\u0435\u0440\u0436\u0434\u0435\u043D\u0438\u0435", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _database?.Cleanup(0);
                MessageBox.Show("\u0418\u0441\u0442\u043E\u0440\u0438\u044F \u043E\u0447\u0438\u0449\u0435\u043D\u0430.", "\u0413\u043E\u0442\u043E\u0432\u043E", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "networkmonitor_backup",
                DefaultExt = ".json",
                Filter = "JSON|*.json"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var storage = new StorageService();
                    var backup = new
                    {
                        Nodes = storage.LoadNodes(),
                        Links = storage.LoadLinks(),
                        Settings = _settings
                    };
                    File.WriteAllText(dlg.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(backup, Newtonsoft.Json.Formatting.Indented));
                    MessageBox.Show("\u041A\u043E\u043D\u0444\u0438\u0433\u0443\u0440\u0430\u0446\u0438\u044F \u044D\u043A\u0441\u043F\u043E\u0440\u0442\u0438\u0440\u043E\u0432\u0430\u043D\u0430.", "\u0413\u043E\u0442\u043E\u0432\u043E", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"\u041E\u0448\u0438\u0431\u043A\u0430: {ex.Message}", "\u041E\u0448\u0438\u0431\u043A\u0430", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //Выключение галочки на "Приближать при выборе узла", если галочка на "Перемешать при выборе узла" была выключенна
        private void CenterMapOnSelectToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ZoomOnSelectToggle.IsChecked = false;
        }

    }
}
