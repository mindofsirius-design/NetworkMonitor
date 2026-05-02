using System.Windows;
using MaterialDesignThemes.Wpf;
using NetworkMonitor.Services;
using NLog;
using System.Net;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            // Принудительно включаем TLS 1.2 и 1.3 для всех HTTP-запросов (нужно для GMap.NET)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            base.OnStartup(e);
            Logger.Info("Application starting");

            var storage = new StorageService();
            var settings = storage.LoadSettings();
            if (settings != null && settings.DarkTheme)
            {
                var helper = new PaletteHelper();
                var theme = helper.GetTheme();
                theme.SetBaseTheme(BaseTheme.Dark);
                helper.SetTheme(theme);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application exiting");
            LogManager.Shutdown();
            base.OnExit(e);
        }
    }
}
