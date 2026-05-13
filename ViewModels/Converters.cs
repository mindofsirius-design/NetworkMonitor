using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NetworkMonitor.Models;
using NetworkMonitor.Services;

namespace NetworkMonitor.ViewModels
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? "Активен" : "Остановлен";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StatusToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush OnlineBrush;
        private static readonly SolidColorBrush OfflineBrush;
        private static readonly SolidColorBrush UnstableBrush;
        private static readonly SolidColorBrush UnknownBrush;

        static StatusToColorConverter()
        {
            OnlineBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); OnlineBrush.Freeze();
            OfflineBrush = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); OfflineBrush.Freeze();
            UnstableBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); UnstableBrush.Freeze();
            UnknownBrush = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)); UnknownBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeStatus status)
            {
                switch (status)
                {
                    case NodeStatus.Online: return OnlineBrush;
                    case NodeStatus.Offline: return OfflineBrush;
                    case NodeStatus.Unstable: return UnstableBrush;
                    default: return UnknownBrush;
                }
            }
            return UnknownBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ToastTypeToColorConverter : IValueConverter
    {
        private static readonly SolidColorBrush ErrorBrush;
        private static readonly SolidColorBrush WarningBrush;
        private static readonly SolidColorBrush InfoBrush;
        private static readonly SolidColorBrush GrayBrush;

        static ToastTypeToColorConverter()
        {
            ErrorBrush = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); ErrorBrush.Freeze();
            WarningBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)); WarningBrush.Freeze();
            InfoBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)); InfoBrush.Freeze();
            GrayBrush = new SolidColorBrush(Colors.Gray); GrayBrush.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToastType type)
            {
                switch (type)
                {
                    case ToastType.Error: return ErrorBrush;
                    case ToastType.Warning: return WarningBrush;
                    default: return InfoBrush;
                }
            }
            return GrayBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class UtcToLocalConverter : IValueConverter
    {
        public static int OffsetHours { get; set; } = 3;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                var local = dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                local = local.AddHours(OffsetHours);
                var fmt = parameter as string ?? "HH:mm:ss";
                return local.ToString(fmt);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
