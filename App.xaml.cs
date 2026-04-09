using System.Windows;
using MaterialDesignThemes.Wpf;
using NetworkMonitor.Services;
using NLog;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
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
