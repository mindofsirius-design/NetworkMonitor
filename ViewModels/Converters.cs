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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NodeStatus status)
            {
                switch (status)
                {
                    case NodeStatus.Online: return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
                    case NodeStatus.Offline: return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
                    case NodeStatus.Unstable: return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                    default: return new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ToastTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToastType type)
            {
                switch (type)
                {
                    case ToastType.Error: return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
                    case ToastType.Warning: return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                    default: return new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
                }
            }
            return new SolidColorBrush(Colors.Gray);
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
}
