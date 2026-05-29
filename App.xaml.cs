using System.Windows;
using MaterialDesignThemes.Wpf;
using NetworkMonitor.Services;
using NLog;
using System;
using System.Net;
using NetworkMonitor.ViewModels;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NetworkMonitor
{
    public partial class App : Application
    {
        private static Mutex _mutex;
        private static EventWaitHandle _wakeEvent;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            // подписка на звершение работы -> выключение программы
            SessionEnding += (s, se) =>
            {
                var mainWindow = MainWindow as NetworkMonitor.Views.MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.ForceClose();
                }
            }; 

            _mutex = new Mutex(true, "NetworkMonitor_SingleInstance", out bool createdNew);

            if (!createdNew)
            {
                var wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "NetworkMonitor_WakeUp");
                wakeEvent.Set();
                Environment.Exit(0);
                return;
            }

            _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "NetworkMonitor_WakeUp");

            Task.Run(() =>
            {
                while (true)
                {
                    _wakeEvent.WaitOne();
                    Current.Dispatcher.Invoke(() =>
                    {
                        var win = Current.MainWindow;
                        if (win != null)
                        {
                            win.Show();
                            win.WindowState = WindowState.Normal;
                            win.Activate();
                        }
                    });
                }
            });

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
            UtcToLocalConverter.OffsetHours = settings.TimeZoneOffsetHours;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("Application exiting");
            LogManager.Shutdown();
            _wakeEvent?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}