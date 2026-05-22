# NetworkMonitor — CLAUDE.md

## Стек и окружение
- .NET Framework 4.8, WPF, C# 7.3
- IDE: Visual Studio 2019/2022
- Сборка: `msbuild NetworkMonitor.sln /p:Configuration=Debug /p:Platform=x64`
- Выход: `bin\Debug\` или `bin\x64\Debug\`

## Файловая структура проекта
```
NetworkMonitor/
├── Models/
│   ├── AppSettings.cs          — настройки приложения (мониторинг, карта, уведомления, email)
│   ├── MonitoringConfig.cs     — конфиги модулей: PingConfig, TcpConfig, SnmpConfig, TracerouteConfig
│   ├── MonitoringResult.cs     — результат одной проверки модуля
│   ├── NetworkNode.cs          — узел сети + NodeStatus enum
│   ├── NodeEvent.cs            — событие (смена статуса, алерт, ошибка модуля)
│   └── NodeLink.cs             — связь между двумя узлами на карте
│
├── Services/
│   ├── DatabaseService.cs      — SQLite: запись/чтение истории ping, модулей, событий
│   ├── EventBus.cs             — singleton pub/sub: OnResult, OnEvent
│   ├── ModuleRegistry.cs       — регистрация и обновление настроек модулей
│   ├── MonitoringScheduler.cs  — планировщик: запуск модулей по расписанию, retry
│   ├── NotificationService.cs  — toast, звук (SoundPlayer), email (SmtpClient)
│   ├── StorageService.cs       — JSON-сериализация nodes.json, links.json, settings.json
│   └── Modules/
│       ├── IMonitoringModule.cs    — интерфейс модуля
│       ├── PingModule.cs           — ICMP ping, расчёт latency и packet loss
│       ├── TcpModule.cs            — проверка доступности TCP-портов
│       ├── SnmpModule.cs           — SNMP GET (sysDescr, sysUpTime, sysName)
│       └── TracerouteModule.cs     — трассировка маршрута через TTL
│
├── ViewModels/
│   ├── BaseViewModel.cs        — INotifyPropertyChanged, OnPropertyChanged
│   ├── Converters.cs           — WPF-конвертеры: статус→цвет, null→visibility, UTC→local и др.
│   ├── EventLogViewModel.cs    — фильтрация событий по узлу, типу, дате
│   ├── MainViewModel.cs        — главный VM + RelayCommand
│   └── StatisticsViewModel.cs  — графики OxyPlot, таблица uptime
│
├── Views/
│   ├── MainWindow.xaml/.cs     — главное окно: карта GMap, список узлов, тосты, статусбар
│   ├── NodeDetailView.xaml/.cs — UserControl: детали узла, мини-график latency, кнопки
│   ├── AddNodeDialog.xaml/.cs  — диалог добавления узла с валидацией IP и координат
│   ├── SettingsDialog.xaml/.cs — настройки (TabControl: мониторинг, уведомления, email, оформление, данные, управление)
│   ├── EventLogView.xaml/.cs   — окно журнала событий
│   └── StatisticsView.xaml/.cs — окно статистики с графиками
│
├── Themes/
│   └── Theme.xaml              — кастомные стили: CompactDataGridRow, ToastBorder (анимация)
│
├── Resources/
│   ├── app.ico
│   ├── Noto/                   — шрифты NotoSans (Regular, Bold, Italic, BoldItalic)
│   └── Roboto/                 — шрифты Roboto (все начертания)
│
├── Assets/
│   └── Sounds/
│       └── alert.wav           — звук оповещения при offline
│
├── data/                       — создаётся автоматически при запуске
│   └── history.db              — SQLite база данных
│
├── logs/                       — создаётся автоматически NLog
│   └── app.log                 — лог приложения (ротация: 5 файлов по 5 МБ)
│
├── nodes.json                  — сохранённые узлы
├── links.json                  — сохранённые связи между узлами
├── settings.json               — сохранённые настройки
│
├── App.xaml/.cs                — точка входа, ресурсы, конвертеры
├── App.config                  — binding redirects, EntityFramework, SQLite провайдеры
├── NLog.config                 — конфигурация логирования
├── NetworkMonitor.csproj       — проект, ссылки на пакеты
├── NetworkMonitor.sln          — solution
└── packages.config             — NuGet пакеты
```

### Ключевые классы
| Класс | Роль |
|---|---|
| `MainViewModel` | центральный VM, владеет сервисами |
| `MonitoringScheduler` | планировщик опросов, семафор 50 задач |
| `ModuleRegistry` | реестр модулей мониторинга |
| `EventBus` | singleton, pub/sub между сервисами и VM |
| `DatabaseService` | SQLite, буферизованная запись (flush каждые 5 с или 100 записей) |
| `StorageService` | JSON-файлы: nodes.json, links.json, settings.json |
| `NotificationService` | toast, звук, email |

## Архитектура
Models/          — POCO: NetworkNode, NodeEvent, MonitoringResult, AppSettings, NodeLink
Services/        — бизнес-логика без UI
Modules/       — модули мониторинга (реализуют IMonitoringModule)
ViewModels/      — MVVM, наследуют BaseViewModel (INotifyPropertyChanged)
Views/           — XAML + code-behind (только UI-логика)
Themes/          — стили MaterialDesign

## Модули мониторинга
Реализуют `IMonitoringModule` (`Name`, `IsEnabled(node)`, `CheckAsync`).  
Зарегистрировать в `ModuleRegistry` конструкторе.  
Текущие: `ping`, `tcp`, `snmp`, `traceroute`.

## База данных (SQLite)
Таблицы: `ping_history`, `module_results`, `events`.  
Индексы по `(node_id, timestamp)`.  
Очистка старых записей: `DatabaseService.Cleanup(keepDays)`.  
**Важно:** единственное соединение `_connection` — не потокобезопасно для чтения/записи одновременно; запись защищена `_writeLock`.

## UI-соглашения
- Тема: MaterialDesign 3, Light/BlueGrey/Cyan (App.xaml)
- Конвертеры регистрируются в App.xaml как ресурсы
- `UtcToLocalConverter.OffsetHours` задаётся из `AppSettings.TimeZoneOffsetHours`
- Toast-уведомления: максимум 5, удаляются по клику (`Toast_MouseDown`)
- Карта: GMap.NET WPF

## Важные детали
- `NodeId` — `Guid` в виде строки, не меняется после создания
- `NetworkNode` реализует `INotifyPropertyChanged` сам (не через BaseViewModel)
- Статусы: `Online=0`, `Unknown=1`, `Unstable=2`, `Offline=3`
- Цвета статусов задаются и в `NetworkNode.StatusColor` (строка hex), и в `StatusToColorConverter` (Brush) — держать синхронно
- `MonitoringScheduler` при таймауте делает одну автоматическую повторную попытку

## Что НЕ делать
- Не обращаться к UI из фоновых потоков напрямую — только через `Application.Current.Dispatcher.Invoke`
- Не добавлять новые зависимости без обновления `packages.config` и `.csproj`
- Не хранить пароль SMTP в открытом виде в коде — только через `AppSettings`