# NetworkMonitor — Design Document

**Date:** 2026-04-09
**Approach:** Hybrid — сохраняем структуру и модели, переписываем ключевые части

## Decisions

- **Карта:** GMap.NET (уже работает, нулевые нативные зависимости, Win7 из коробки)
- **Графики:** OxyPlot (чистый WPF, без SkiaSharp, Win7-совместимый)
- **SNMP:** #SNMP (Lextm.SharpSnmpLib) — полноценная реализация
- **Модульная система:** статическая регистрация (ModuleRegistry), без плагинов/DI
- **Локализация:** только русский

## 1. Структура проекта

```
NetworkMonitor/
├── Models/
│   ├── NetworkNode.cs          ← доработать (добавить MonitoringConfig)
│   ├── NodeLink.cs             ← оставить
│   ├── AppSettings.cs          ← доработать (уведомления, email)
│   ├── MonitoringConfig.cs     ← NEW
│   ├── MonitoringResult.cs     ← NEW
│   └── NodeEvent.cs            ← NEW
│
├── Services/
│   ├── Modules/
│   │   ├── IMonitoringModule.cs    ← NEW
│   │   ├── PingModule.cs           ← NEW (замена PingService)
│   │   ├── TcpModule.cs            ← NEW
│   │   ├── SnmpModule.cs           ← NEW (#SNMP)
│   │   └── TracerouteModule.cs     ← NEW
│   ├── ModuleRegistry.cs       ← NEW
│   ├── MonitoringScheduler.cs  ← NEW (SemaphoreSlim(50))
│   ├── StorageService.cs       ← оставить (JSON для конфигов)
│   ├── DatabaseService.cs      ← NEW (SQLite)
│   ├── NotificationService.cs  ← NEW (toast + звук + email)
│   ├── EventBus.cs             ← NEW
│   └── PingService.cs          ← УДАЛИТЬ
│
├── ViewModels/
│   ├── BaseViewModel.cs        ← оставить
│   ├── MainViewModel.cs        ← доработать
│   ├── Converters.cs           ← доработать
│   ├── StatisticsViewModel.cs  ← NEW
│   └── EventLogViewModel.cs    ← NEW
│
├── Views/
│   ├── MainWindow.xaml/cs      ← доработать (линии, мини-карта, toast)
│   ├── AddNodeDialog.xaml/cs   ← доработать (настройки модулей)
│   ├── SettingsDialog.xaml/cs  ← доработать (вкладки)
│   ├── NodeDetailView.xaml     ← NEW (UserControl)
│   ├── StatisticsView.xaml     ← NEW (dashboard)
│   └── EventLogView.xaml       ← NEW (журнал)
│
├── Assets/
│   └── Sounds/
│       └── alert.wav           ← NEW
│
├── Themes/
│   └── Theme.xaml              ← NEW
```

## 2. Модели данных

### NetworkNode.cs — доработка

```csharp
class NetworkNode : INotifyPropertyChanged
{
    string Id, Name, IpAddress, DeviceType, Description
    double Latitude, Longitude
    NodeStatus Status          // Online, Offline, Unstable, Unknown
    long LastPingMs
    DateTime LastSeen
    MonitoringConfig Monitoring  // конфиг всех модулей
}
```

### MonitoringConfig.cs

```csharp
class MonitoringConfig
{
    PingConfig Ping       // { Enabled=true, IntervalSec=5 }
    TcpConfig Tcp         // { Enabled=false, Ports=[80,443] }
    SnmpConfig Snmp       // { Enabled=false, Community="public", Version="2c" }
    TracerouteConfig Traceroute  // { Enabled=false }
}
```

Каждый вложенный конфиг — отдельный класс с `Enabled` + параметры. Ping включён по умолчанию.

### MonitoringResult.cs

```csharp
class MonitoringResult
{
    string NodeId
    string ModuleName        // "ping", "tcp", "snmp", "traceroute"
    bool Success
    long LatencyMs
    double PacketLoss        // 0.0–1.0
    NodeStatus Status
    string Details           // текстовый результат
    DateTime Timestamp
}
```

### NodeEvent.cs

```csharp
class NodeEvent
{
    long Id                  // SQLite autoincrement
    string NodeId
    string EventType         // "StatusChanged", "Alert", "ModuleError"
    string Message
    NodeStatus? OldStatus, NewStatus
    DateTime Timestamp
}
```

### AppSettings.cs — доработка

Добавляем: ToastEnabled, SoundEnabled, SoundFilePath, SoundDebounceSec,
EmailEnabled, SmtpHost, SmtpPort, SmtpUser, SmtpPassword, EmailFrom, EmailTo,
HistoryKeepDays.

## 3. Модульная система и планировщик

### IMonitoringModule

```csharp
interface IMonitoringModule
{
    string Name { get; }
    bool IsEnabled(NetworkNode node);
    Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct);
}
```

### Реализации

- **PingModule** — `Ping.SendPingAsync()` × N retries, средний RTT, packet loss → статус
- **TcpModule** — `TcpClient.ConnectAsync()` на каждый порт, таймаут, список open/closed
- **SnmpModule** — `#SNMP Messenger.GetAsync()`, sysUpTime/sysDescr, v1/v2c
- **TracerouteModule** — ICMP с TTL 1→30, hop-ы (IP + RTT)

### ModuleRegistry

```csharp
class ModuleRegistry
{
    ModuleRegistry()
    {
        _modules.Add(new PingModule());
        _modules.Add(new TcpModule());
        _modules.Add(new SnmpModule());
        _modules.Add(new TracerouteModule());
    }
    IEnumerable<IMonitoringModule> GetEnabledModules(NetworkNode node);
}
```

### MonitoringScheduler

- `SemaphoreSlim(50)` — макс 50 параллельных задач
- Основной цикл тикает каждые 500ms
- Для каждого узла проверяет какие модули пора запускать (по индивидуальным интервалам)
- Таймаут 10 сек на каждый CheckAsync
- Retry: 1 повтор через 2 сек при неудаче
- Результат → `EventBus.Publish(result)`

### EventBus

```csharp
class EventBus
{
    event Action<MonitoringResult> OnResult;
    event Action<NodeEvent> OnEvent;
}
```

Подписчики: MainViewModel, DatabaseService, NotificationService.

## 4. Хранение данных

### StorageService (JSON) — без изменений

- `nodes.json`, `links.json`, `settings.json`

### DatabaseService (SQLite)

Файл: `data/history.db`

```sql
CREATE TABLE ping_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    node_id TEXT NOT NULL,
    latency_ms INTEGER,
    packet_loss REAL,
    status TEXT NOT NULL,
    timestamp TEXT NOT NULL
);

CREATE TABLE module_results (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    node_id TEXT NOT NULL,
    module_name TEXT NOT NULL,
    success INTEGER NOT NULL,
    details TEXT,
    timestamp TEXT NOT NULL
);

CREATE TABLE events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    node_id TEXT NOT NULL,
    event_type TEXT NOT NULL,
    message TEXT,
    old_status TEXT,
    new_status TEXT,
    timestamp TEXT NOT NULL
);

CREATE INDEX idx_ping_node_time ON ping_history(node_id, timestamp);
CREATE INDEX idx_events_node_time ON events(node_id, timestamp);
```

API: Initialize(), SavePingResult(), SaveModuleResult(), SaveEvent(),
GetPingHistory(), GetUptime(), GetEvents(), GetAllEvents(), Cleanup(keepDays=90).

Буферизация: flush каждые 5 сек или 100 записей.

Библиотека: `System.Data.SQLite`.

## 5. UI

### MainWindow layout

```
┌──────────────────────────────────────────────────────┐
│ Toolbar: [▶ Старт] [💾] [+ Узел] [⚙] [📊] [📋]    │
├────────────────────────────────────┬─────────────────┤
│                                    │ Список узлов    │
│         GMap.NET карта             │ (фильтр сверху) │
│                                    │                 │
│   ┌──────┐                         ├─────────────────┤
│   │мини- │                         │ NodeDetailView  │
│   │карта │                         │ + мини-график   │
│   └──────┘                         │                 │
├────────────────────────────────────┴─────────────────┤
│ StatusBar: Online: 12 | Offline: 3 | Unstable: 1     │
└──────────────────────────────────────────────────────┘
```

### Маркеры узлов

PackIcon из MaterialDesignThemes (Router, Server, Monitor, Camera, LanConnect).
Рендерятся в Bitmap для GMap.NET. Цвет фона по статусу. Тултип: имя, IP, ping, модули.

### Линии связей

`GMapRoute` между парами из NodeLink. Цвет по худшему статусу двух узлов.

### Мини-карта

Второй `GMapControl` 200×150px, zoom = основная - 4, синхронизация центра, viewport rectangle.

### Toast-уведомления

Стек в правом нижнем углу, slide-in/fade-out, макс 5, автоудаление через 5 сек.

### Звук

`SoundPlayer` + `alert.wav` при Online→Offline. Debounce 30 сек.

### NodeDetailView (UserControl)

Имя, IP, тип, статус, ping, packet loss, мини-график latency (OxyPlot, 100 точек),
список модулей с результатами, кнопки: редактировать, удалить, ping now.

### StatisticsView (окно)

Плитки Online/Offline/Unstable, график avg latency (LineSeries),
packet loss по узлам (BarSeries), таблица uptime (24h/7d/30d).
Фильтр периода: 1ч, 24ч, 7д, 30д.

### EventLogView (окно)

DataGrid: время, узел, тип, сообщение, старый→новый статус.
Фильтры: по узлу, типу, периоду. Пагинация по 500.

### SettingsDialog — вкладки

- **Мониторинг**: интервал, таймаут, retries, порог unstable
- **Уведомления**: toast вкл/выкл, звук вкл/выкл, debounce
- **Email**: enabled, SMTP настройки, кнопка "Тест"
- **Оформление**: тёмная тема toggle
- **Данные**: хранить N дней, очистить историю, экспорт конфигурации

### AddNodeDialog — доработка

Секция "Модули мониторинга": Ping (всегда вкл, интервал), TCP (toggle + порты),
SNMP (toggle + community + версия), Traceroute (toggle).

## 6. Тема и зависимости

### Тёмная/светлая

MaterialDesign PaletteHelper (уже работает). Применение при старте. OxyPlot адаптация цветов.

### Иконки

MaterialDesign PackIcon — нулевые дополнительные файлы.

### NLog

`NLog.config`: `logs/app.log`, ротация 5MB × 5 файлов.
Scheduler/Modules → Info, ошибки → Error, ping → Debug.

### NuGet

| Пакет | Версия | Статус |
|---|---|---|
| MaterialDesignThemes | 5.3.1 | уже есть |
| GMap.Net.WPF | 1.0.0.1 | уже есть |
| Newtonsoft.Json | 13.0.4 | уже есть |
| NLog | 6.1.2 | уже есть |
| OxyPlot.Wpf | 2.1.2 | NEW |
| System.Data.SQLite | 1.0.118 | NEW |
| Lextm.SharpSnmpLib | 12.5.2 | NEW |
