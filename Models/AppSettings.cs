namespace NetworkMonitor.Models
{
    public class AppSettings
    {
        //Общие
        public bool StartWithWindows { get; set; } = false;
        public bool AutoStartMonitoring { get; set; } = false;

        // Мониторинг
        public int PingIntervalSeconds { get; set; } = 30;
        public int PingTimeoutMs { get; set; } = 1000;
        public int PingRetries { get; set; } = 3;
        public int UnstableThresholdMs { get; set; } = 200;

        // Карта
        public double DefaultMapZoom { get; set; } = 10;
        public double DefaultMapLat { get; set; } = 55.75;
        public double DefaultMapLon { get; set; } = 37.61;
        public double MapZoom { get; set; } = 10;
        public double MapLat { get; set; } = 55.75;
        public double MapLon { get; set; } = 37.61;

        // Оформление
        public bool DarkTheme { get; set; } = false;
        public int MarkerSize { get; set; } = 36;
        public double MarkerOpacity { get; set; } = 1.0;

        // Уведомления
        public bool ToastEnabled { get; set; } = true;
        public bool SoundEnabled { get; set; } = true;
        public string SoundFilePath { get; set; } = "Assets/Sounds/alert.wav";
        public int SoundDebounceSec { get; set; } = 30;

        // Email (опционально)
        public bool EmailEnabled { get; set; } = false;
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string EmailFrom { get; set; } = "";
        public string EmailTo { get; set; } = "";

        // Управление
        public bool CenterMapOnSelect { get; set; } = false;
        public bool ZoomOnSelect { get; set; } = false;
        public bool AlwaysZoomToCenter { get; set; } = false;
        public bool MapLocked { get; set; } = false; // кнопка на главном экране

        // Данные
        public int TimeZoneOffsetHours { get; set; } = 3;
        public int HistoryKeepDays { get; set; } = 90;

        // Язык
        public string Language { get; set; } = "ru";
    }
}
