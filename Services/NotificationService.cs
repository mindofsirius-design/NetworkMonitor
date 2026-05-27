using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Models;
using NLog;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NetworkMonitor.Services
{
    public enum ToastType { Info, Warning, Error }

    public class ToastNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ToastType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class NotificationService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private AppSettings _settings;
        private DateTime _lastSoundTime = DateTime.MinValue;
        private SoundPlayer _soundPlayer;

        public ObservableCollection<ToastNotification> Toasts { get; } = new ObservableCollection<ToastNotification>();

        public NotificationService(AppSettings settings)
        {
            _settings = settings;
            LoadSound();
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            LoadSound();
        }

        private void LoadSound()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.SoundFilePath);
                if (File.Exists(path))
                    _soundPlayer = new SoundPlayer(path);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load sound file");
            }
        }

        public void OnNodeEvent(NodeEvent nodeEvent)
        {
            if (!_settings.ToastEnabled && !_settings.SoundEnabled && !_settings.EmailEnabled)
                return;

            if (_settings.ToastEnabled)
            {
                var type = ToastType.Info;
                if (nodeEvent.NewStatus == NodeStatus.Offline) type = ToastType.Error;
                else if (nodeEvent.NewStatus == NodeStatus.Unstable) type = ToastType.Warning;

                ShowToast(nodeEvent.Message, type);
            }

            if (_settings.SoundEnabled && nodeEvent.NewStatus == NodeStatus.Offline)
            {
                PlaySound();
            }

            if (_settings.EmailEnabled && nodeEvent.NewStatus == NodeStatus.Offline)
            {
                Task.Run(() => SendEmail(
                    $"[NetworkMonitor] {nodeEvent.Message}",
                    $"Event: {nodeEvent.EventType}\nNode: {nodeEvent.NodeId}\n{nodeEvent.Message}\nTime: {nodeEvent.Timestamp}"));
            }
        }

        public void ShowToast(string message, ToastType type = ToastType.Info)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Toasts.Add(new ToastNotification
                {
                    Title = type.ToString(),
                    Message = message,
                    Type = type
                });

                while (Toasts.Count > 5)
                    Toasts.RemoveAt(0);
            });
        }

        private void PlaySound()
        {
            var now = DateTime.Now;
            if ((now - _lastSoundTime).TotalSeconds < _settings.SoundDebounceSec)
                return;

            _lastSoundTime = now;
            try
            {
                _soundPlayer?.Play();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to play sound");
            }
        }

        private void SendEmail(string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(_settings.EmailFrom));
                message.To.Add(MailboxAddress.Parse(_settings.EmailTo));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using (var client = new SmtpClient())
                {
                    client.Connect(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                    client.Authenticate(_settings.SmtpUser, _settings.SmtpPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }
                Logger.Info("Email sent: {0}", subject);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to send email");
            }
        }

        public void RemoveToast(ToastNotification toast)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Toasts.Remove(toast);
            });
        }
    }
}
