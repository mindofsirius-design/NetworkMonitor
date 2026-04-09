# NetworkMonitor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Transform existing basic ping monitor into a modular network monitoring system with SQLite history, OxyPlot graphs, toast notifications, and map enhancements.

**Architecture:** Hybrid rewrite — keep MVVM structure, GMap.NET map, Material Design theme. Replace PingService with modular IMonitoringModule system. Add EventBus for decoupled communication, SQLite for history, OxyPlot for charts. Static module registration via ModuleRegistry.

**Tech Stack:** C# / WPF / .NET Framework 4.8, GMap.NET, MaterialDesignThemes, OxyPlot.Wpf, System.Data.SQLite, Lextm.SharpSnmpLib, NLog, Newtonsoft.Json

---

## Task 1: Add NuGet Packages

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\packages.config`
- Modify: `D:\Ягор\NetworkMonitor\NetworkMonitor.csproj`
- Modify: `D:\Ягор\NetworkMonitor\App.config`

**Step 1: Install new NuGet packages**

Run in VS Package Manager Console (or via nuget CLI from project root):

```
Install-Package OxyPlot.Wpf -Version 2.1.2
Install-Package System.Data.SQLite -Version 1.0.118
Install-Package Lextm.SharpSnmpLib -Version 12.5.2
```

These commands will auto-update `packages.config`, `.csproj` references, and `App.config` binding redirects.

**Step 2: Verify build**

Build the solution (Ctrl+Shift+B or `msbuild NetworkMonitor.sln`). Expected: build succeeds with 0 errors.

**Step 3: Commit**

```bash
git add packages.config NetworkMonitor.csproj App.config
git commit -m "feat: add OxyPlot, SQLite, SharpSnmpLib packages"
```

---

## Task 2: New Model Classes

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Models\MonitoringConfig.cs`
- Create: `D:\Ягор\NetworkMonitor\Models\MonitoringResult.cs`
- Create: `D:\Ягор\NetworkMonitor\Models\NodeEvent.cs`

**Step 1: Create MonitoringConfig.cs**

```csharp
using System.Collections.Generic;

namespace NetworkMonitor.Models
{
    public class PingConfig
    {
        public bool Enabled { get; set; } = true;
        public int IntervalSec { get; set; } = 5;
    }

    public class TcpConfig
    {
        public bool Enabled { get; set; } = false;
        public List<int> Ports { get; set; } = new List<int> { 80, 443 };
    }

    public class SnmpConfig
    {
        public bool Enabled { get; set; } = false;
        public string Community { get; set; } = "public";
        public string Version { get; set; } = "2c";
    }

    public class TracerouteConfig
    {
        public bool Enabled { get; set; } = false;
    }

    public class MonitoringConfig
    {
        public PingConfig Ping { get; set; } = new PingConfig();
        public TcpConfig Tcp { get; set; } = new TcpConfig();
        public SnmpConfig Snmp { get; set; } = new SnmpConfig();
        public TracerouteConfig Traceroute { get; set; } = new TracerouteConfig();
    }
}
```

**Step 2: Create MonitoringResult.cs**

```csharp
using System;

namespace NetworkMonitor.Models
{
    public class MonitoringResult
    {
        public string NodeId { get; set; }
        public string ModuleName { get; set; }
        public bool Success { get; set; }
        public long LatencyMs { get; set; }
        public double PacketLoss { get; set; }
        public NodeStatus Status { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
```

**Step 3: Create NodeEvent.cs**

```csharp
using System;

namespace NetworkMonitor.Models
{
    public class NodeEvent
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }
        public NodeStatus? OldStatus { get; set; }
        public NodeStatus? NewStatus { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
```

**Step 4: Add files to .csproj**

Add inside the `<ItemGroup>` containing other `<Compile>` entries:

```xml
<Compile Include="Models\MonitoringConfig.cs" />
<Compile Include="Models\MonitoringResult.cs" />
<Compile Include="Models\NodeEvent.cs" />
```

**Step 5: Build and verify**

Build solution. Expected: 0 errors.

**Step 6: Commit**

```bash
git add Models/MonitoringConfig.cs Models/MonitoringResult.cs Models/NodeEvent.cs NetworkMonitor.csproj
git commit -m "feat: add MonitoringConfig, MonitoringResult, NodeEvent models"
```

---

## Task 3: Update Existing Models

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\Models\NetworkNode.cs`
- Modify: `D:\Ягор\NetworkMonitor\Models\AppSettings.cs`

**Step 1: Add MonitoringConfig to NetworkNode**

In `NetworkNode.cs`, add property after the `Description` property:

```csharp
public MonitoringConfig Monitoring { get; set; } = new MonitoringConfig();
```

**Step 2: Expand AppSettings**

Replace the entire `AppSettings.cs` with:

```csharp
namespace NetworkMonitor.Models
{
    public class AppSettings
    {
        // Мониторинг
        public int PingIntervalSeconds { get; set; } = 30;
        public int PingTimeoutMs { get; set; } = 1000;
        public int PingRetries { get; set; } = 3;
        public int UnstableThresholdMs { get; set; } = 200;

        // Карта
        public double MapZoom { get; set; } = 10;
        public double MapLat { get; set; } = 55.75;
        public double MapLon { get; set; } = 37.61;

        // Оформление
        public bool DarkTheme { get; set; } = false;

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

        // Данные
        public int HistoryKeepDays { get; set; } = 90;

        // Язык
        public string Language { get; set; } = "ru";
    }
}
```

**Step 3: Build and verify**

Build solution. Expected: 0 errors. The removed `SoundAlerts` field may cause a build error in `SettingsDialog.xaml.cs` — fix by replacing `SoundAlerts` references with `SoundEnabled`.

In `Views/SettingsDialog.xaml.cs`, find:
```csharp
SoundToggle.IsChecked = _settings.SoundAlerts;
```
Replace with:
```csharp
SoundToggle.IsChecked = _settings.SoundEnabled;
```

And find:
```csharp
_settings.SoundAlerts = SoundToggle.IsChecked == true;
```
Replace with:
```csharp
_settings.SoundEnabled = SoundToggle.IsChecked == true;
```

**Step 4: Commit**

```bash
git add Models/NetworkNode.cs Models/AppSettings.cs Views/SettingsDialog.xaml.cs
git commit -m "feat: add MonitoringConfig to NetworkNode, expand AppSettings"
```

---

## Task 4: EventBus

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Services\EventBus.cs`

**Step 1: Create EventBus.cs**

```csharp
using System;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services
{
    public class EventBus
    {
        private static readonly Lazy<EventBus> _instance = new Lazy<EventBus>(() => new EventBus());
        public static EventBus Instance => _instance.Value;

        public event Action<MonitoringResult> OnResult;
        public event Action<NodeEvent> OnEvent;

        public void PublishResult(MonitoringResult result)
        {
            OnResult?.Invoke(result);
        }

        public void PublishEvent(NodeEvent nodeEvent)
        {
            OnEvent?.Invoke(nodeEvent);
        }
    }
}
```

**Step 2: Add to .csproj**

```xml
<Compile Include="Services\EventBus.cs" />
```

**Step 3: Build and commit**

```bash
git add Services/EventBus.cs NetworkMonitor.csproj
git commit -m "feat: add EventBus for decoupled service communication"
```

---

## Task 5: IMonitoringModule + PingModule

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Services\Modules\IMonitoringModule.cs`
- Create: `D:\Ягор\NetworkMonitor\Services\Modules\PingModule.cs`

**Step 1: Create Modules directory and IMonitoringModule.cs**

```csharp
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services.Modules
{
    public interface IMonitoringModule
    {
        string Name { get; }
        bool IsEnabled(NetworkNode node);
        Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct);
    }
}
```

**Step 2: Create PingModule.cs**

```csharp
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class PingModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "ping";

        public int TimeoutMs { get; set; } = 1000;
        public int Retries { get; set; } = 3;
        public int UnstableThresholdMs { get; set; } = 200;

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Ping?.Enabled ?? true;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var result = new MonitoringResult
            {
                NodeId = node.Id,
                ModuleName = Name,
                Timestamp = DateTime.UtcNow
            };

            int successCount = 0;
            long totalMs = 0;

            using (var pinger = new Ping())
            {
                for (int i = 0; i < Retries; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var reply = await pinger.SendPingAsync(node.IpAddress, TimeoutMs);
                        if (reply.Status == IPStatus.Success)
                        {
                            successCount++;
                            totalMs += reply.RoundtripTime;
                        }
                    }
                    catch (PingException ex)
                    {
                        Logger.Debug(ex, "Ping failed for {0}", node.IpAddress);
                    }
                }
            }

            if (successCount == 0)
            {
                result.Success = false;
                result.LatencyMs = -1;
                result.PacketLoss = 1.0;
                result.Status = NodeStatus.Offline;
                result.Details = "All pings failed";
            }
            else
            {
                long avgMs = totalMs / successCount;
                double loss = 1.0 - (double)successCount / Retries;

                result.Success = true;
                result.LatencyMs = avgMs;
                result.PacketLoss = loss;
                result.Status = avgMs > UnstableThresholdMs || loss > 0.3
                    ? NodeStatus.Unstable
                    : NodeStatus.Online;
                result.Details = $"avg={avgMs}ms, loss={loss:P0}";
            }

            Logger.Info("Ping {0} ({1}): {2}, {3}ms", node.Name, node.IpAddress, result.Status, result.LatencyMs);
            return result;
        }
    }
}
```

**Step 3: Add to .csproj**

```xml
<Compile Include="Services\Modules\IMonitoringModule.cs" />
<Compile Include="Services\Modules\PingModule.cs" />
```

**Step 4: Build and commit**

```bash
git add Services/Modules/ NetworkMonitor.csproj
git commit -m "feat: add IMonitoringModule interface and PingModule"
```

---

## Task 6: TcpModule, SnmpModule, TracerouteModule

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Services\Modules\TcpModule.cs`
- Create: `D:\Ягор\NetworkMonitor\Services\Modules\SnmpModule.cs`
- Create: `D:\Ягор\NetworkMonitor\Services\Modules\TracerouteModule.cs`

**Step 1: Create TcpModule.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class TcpModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "tcp";
        public int TimeoutMs { get; set; } = 5000;

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Tcp?.Enabled ?? false;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var ports = node.Monitoring?.Tcp?.Ports ?? new List<int>();
            var openPorts = new List<int>();
            var closedPorts = new List<int>();

            foreach (int port in ports)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync(node.IpAddress, port);
                        var delayTask = Task.Delay(TimeoutMs, ct);
                        var completed = await Task.WhenAny(connectTask, delayTask);

                        if (completed == connectTask && client.Connected)
                            openPorts.Add(port);
                        else
                            closedPorts.Add(port);
                    }
                }
                catch
                {
                    closedPorts.Add(port);
                }
            }

            bool allOpen = closedPorts.Count == 0 && openPorts.Count > 0;
            var sb = new StringBuilder();
            if (openPorts.Count > 0) sb.AppendFormat("Open: {0}", string.Join(", ", openPorts));
            if (closedPorts.Count > 0)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.AppendFormat("Closed: {0}", string.Join(", ", closedPorts));
            }

            var result = new MonitoringResult
            {
                NodeId = node.Id,
                ModuleName = Name,
                Success = allOpen,
                Status = allOpen ? NodeStatus.Online : (openPorts.Count > 0 ? NodeStatus.Unstable : NodeStatus.Offline),
                Details = sb.ToString(),
                Timestamp = DateTime.UtcNow
            };

            Logger.Info("TCP {0}: {1}", node.Name, result.Details);
            return result;
        }
    }
}
```

**Step 2: Create SnmpModule.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class SnmpModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "snmp";
        public int TimeoutMs { get; set; } = 5000;

        // Standard OIDs
        private static readonly ObjectIdentifier SysDescr = new ObjectIdentifier("1.3.6.1.2.1.1.1.0");
        private static readonly ObjectIdentifier SysUpTime = new ObjectIdentifier("1.3.6.1.2.1.1.3.0");
        private static readonly ObjectIdentifier SysName = new ObjectIdentifier("1.3.6.1.2.1.1.5.0");

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Snmp?.Enabled ?? false;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var config = node.Monitoring?.Snmp ?? new SnmpConfig();

            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var version = config.Version == "1" ? VersionCode.V1 : VersionCode.V2;
                    var endpoint = new IPEndPoint(IPAddress.Parse(node.IpAddress), 161);
                    var community = new OctetString(config.Community);

                    var variables = new List<Variable>
                    {
                        new Variable(SysDescr),
                        new Variable(SysUpTime),
                        new Variable(SysName)
                    };

                    var results = Messenger.Get(
                        version,
                        endpoint,
                        community,
                        variables,
                        TimeoutMs);

                    var details = new List<string>();
                    foreach (var v in results)
                    {
                        details.Add($"{v.Id}={v.Data}");
                    }

                    Logger.Info("SNMP {0}: OK, {1} OIDs", node.Name, results.Count);

                    return new MonitoringResult
                    {
                        NodeId = node.Id,
                        ModuleName = Name,
                        Success = true,
                        Status = NodeStatus.Online,
                        Details = string.Join("; ", details),
                        Timestamp = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "SNMP {0} failed", node.Name);
                    return new MonitoringResult
                    {
                        NodeId = node.Id,
                        ModuleName = Name,
                        Success = false,
                        Status = NodeStatus.Offline,
                        Details = ex.Message,
                        Timestamp = DateTime.UtcNow
                    };
                }
            }, ct);
        }
    }
}
```

**Step 3: Create TracerouteModule.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services.Modules
{
    public class TracerouteModule : IMonitoringModule
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public string Name => "traceroute";
        public int MaxHops { get; set; } = 30;
        public int TimeoutMs { get; set; } = 3000;

        public bool IsEnabled(NetworkNode node)
        {
            return node.Monitoring?.Traceroute?.Enabled ?? false;
        }

        public async Task<MonitoringResult> CheckAsync(NetworkNode node, CancellationToken ct)
        {
            var hops = new List<string>();
            bool reachedTarget = false;

            await Task.Run(() =>
            {
                var buffer = new byte[32];
                using (var pinger = new Ping())
                {
                    for (int ttl = 1; ttl <= MaxHops; ttl++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var options = new PingOptions(ttl, true);
                        try
                        {
                            var reply = pinger.Send(node.IpAddress, TimeoutMs, buffer, options);

                            if (reply.Status == IPStatus.TtlExpired)
                            {
                                hops.Add($"{ttl}: {reply.Address} ({reply.RoundtripTime}ms)");
                            }
                            else if (reply.Status == IPStatus.Success)
                            {
                                hops.Add($"{ttl}: {reply.Address} ({reply.RoundtripTime}ms)");
                                reachedTarget = true;
                                break;
                            }
                            else
                            {
                                hops.Add($"{ttl}: * ({reply.Status})");
                            }
                        }
                        catch (PingException)
                        {
                            hops.Add($"{ttl}: * (timeout)");
                        }
                    }
                }
            }, ct);

            var sb = new StringBuilder();
            foreach (var hop in hops)
                sb.AppendLine(hop);

            Logger.Info("Traceroute {0}: {1} hops, reached={2}", node.Name, hops.Count, reachedTarget);

            return new MonitoringResult
            {
                NodeId = node.Id,
                ModuleName = Name,
                Success = reachedTarget,
                Status = reachedTarget ? NodeStatus.Online : NodeStatus.Offline,
                Details = sb.ToString().TrimEnd(),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
```

**Step 4: Add to .csproj**

```xml
<Compile Include="Services\Modules\TcpModule.cs" />
<Compile Include="Services\Modules\SnmpModule.cs" />
<Compile Include="Services\Modules\TracerouteModule.cs" />
```

**Step 5: Build and commit**

```bash
git add Services/Modules/ NetworkMonitor.csproj
git commit -m "feat: add TcpModule, SnmpModule, TracerouteModule"
```

---

## Task 7: ModuleRegistry + MonitoringScheduler

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Services\ModuleRegistry.cs`
- Create: `D:\Ягор\NetworkMonitor\Services\MonitoringScheduler.cs`

**Step 1: Create ModuleRegistry.cs**

```csharp
using System.Collections.Generic;
using System.Linq;
using NetworkMonitor.Models;
using NetworkMonitor.Services.Modules;

namespace NetworkMonitor.Services
{
    public class ModuleRegistry
    {
        private readonly List<IMonitoringModule> _modules = new List<IMonitoringModule>();

        public ModuleRegistry(AppSettings settings)
        {
            _modules.Add(new PingModule
            {
                TimeoutMs = settings.PingTimeoutMs,
                Retries = settings.PingRetries,
                UnstableThresholdMs = settings.UnstableThresholdMs
            });
            _modules.Add(new TcpModule());
            _modules.Add(new SnmpModule());
            _modules.Add(new TracerouteModule());
        }

        public IEnumerable<IMonitoringModule> GetAllModules() => _modules.AsReadOnly();

        public IEnumerable<IMonitoringModule> GetEnabledModules(NetworkNode node)
        {
            return _modules.Where(m => m.IsEnabled(node));
        }

        public void UpdateSettings(AppSettings settings)
        {
            var ping = _modules.OfType<PingModule>().FirstOrDefault();
            if (ping != null)
            {
                ping.TimeoutMs = settings.PingTimeoutMs;
                ping.Retries = settings.PingRetries;
                ping.UnstableThresholdMs = settings.UnstableThresholdMs;
            }
        }
    }
}
```

**Step 2: Create MonitoringScheduler.cs**

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetworkMonitor.Models;
using NetworkMonitor.Services.Modules;
using NLog;

namespace NetworkMonitor.Services
{
    public class MonitoringScheduler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ModuleRegistry _registry;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(50, 50);
        private readonly ConcurrentDictionary<string, DateTime> _nextRun = new ConcurrentDictionary<string, DateTime>();
        private CancellationTokenSource _cts;
        private List<NetworkNode> _nodes = new List<NetworkNode>();
        private readonly object _nodesLock = new object();

        public int DefaultIntervalSec { get; set; } = 5;
        public int TaskTimeoutMs { get; set; } = 10000;

        public MonitoringScheduler(ModuleRegistry registry)
        {
            _registry = registry;
        }

        public void Start(IEnumerable<NetworkNode> nodes)
        {
            Stop();
            lock (_nodesLock)
            {
                _nodes = nodes.ToList();
            }
            _nextRun.Clear();
            _cts = new CancellationTokenSource();
            Task.Run(() => RunLoop(_cts.Token));
            Logger.Info("Scheduler started with {0} nodes", _nodes.Count);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            Logger.Info("Scheduler stopped");
        }

        public void UpdateNodes(IEnumerable<NetworkNode> nodes)
        {
            lock (_nodesLock)
            {
                _nodes = nodes.ToList();
            }
        }

        private async Task RunLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    List<NetworkNode> snapshot;
                    lock (_nodesLock)
                    {
                        snapshot = _nodes.ToList();
                    }

                    var now = DateTime.UtcNow;
                    var tasks = new List<Task>();

                    foreach (var node in snapshot)
                    {
                        foreach (var module in _registry.GetEnabledModules(node))
                        {
                            var key = $"{node.Id}:{module.Name}";
                            var nextTime = _nextRun.GetOrAdd(key, DateTime.MinValue);

                            if (now < nextTime)
                                continue;

                            int interval = GetInterval(node, module);
                            _nextRun[key] = now.AddSeconds(interval);

                            tasks.Add(RunModuleAsync(node, module, ct));
                        }
                    }

                    if (tasks.Count > 0)
                        await Task.WhenAll(tasks);

                    await Task.Delay(500, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Scheduler loop error");
                    await Task.Delay(1000, ct);
                }
            }
        }

        private async Task RunModuleAsync(NetworkNode node, IMonitoringModule module, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                using (var timeoutCts = new CancellationTokenSource(TaskTimeoutMs))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token))
                {
                    try
                    {
                        var result = await module.CheckAsync(node, linkedCts.Token);
                        EventBus.Instance.PublishResult(result);

                        // Detect status change
                        if (module.Name == "ping" && node.Status != result.Status)
                        {
                            var nodeEvent = new NodeEvent
                            {
                                NodeId = node.Id,
                                EventType = "StatusChanged",
                                Message = $"{node.Name}: {node.Status} → {result.Status}",
                                OldStatus = node.Status,
                                NewStatus = result.Status,
                                Timestamp = DateTime.UtcNow
                            };
                            EventBus.Instance.PublishEvent(nodeEvent);
                        }
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        Logger.Warn("Module {0} timed out for {1}", module.Name, node.Name);
                        // Retry once after 2 sec
                        await Task.Delay(2000, ct);
                        try
                        {
                            var result = await module.CheckAsync(node, ct);
                            EventBus.Instance.PublishResult(result);
                        }
                        catch (Exception retryEx)
                        {
                            Logger.Error(retryEx, "Retry failed: {0} for {1}", module.Name, node.Name);
                            EventBus.Instance.PublishResult(new MonitoringResult
                            {
                                NodeId = node.Id,
                                ModuleName = module.Name,
                                Success = false,
                                Status = NodeStatus.Offline,
                                Details = $"Timeout + retry failed: {retryEx.Message}",
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Scheduler stopping
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "RunModuleAsync error: {0} for {1}", module.Name, node.Name);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private int GetInterval(NetworkNode node, IMonitoringModule module)
        {
            if (module.Name == "ping")
                return node.Monitoring?.Ping?.IntervalSec ?? DefaultIntervalSec;
            return DefaultIntervalSec * 2; // other modules run less frequently
        }
    }
}
```

**Step 3: Add to .csproj**

```xml
<Compile Include="Services\ModuleRegistry.cs" />
<Compile Include="Services\MonitoringScheduler.cs" />
```

**Step 4: Build and commit**

```bash
git add Services/ModuleRegistry.cs Services/MonitoringScheduler.cs NetworkMonitor.csproj
git commit -m "feat: add ModuleRegistry and MonitoringScheduler"
```

---

## Task 8: DatabaseService (SQLite)

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Services\DatabaseService.cs`

**Step 1: Create DatabaseService.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using NetworkMonitor.Models;
using NLog;

namespace NetworkMonitor.Services
{
    public class DatabaseService : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _dbPath;
        private SQLiteConnection _connection;

        // Write buffer
        private readonly List<MonitoringResult> _resultBuffer = new List<MonitoringResult>();
        private readonly List<NodeEvent> _eventBuffer = new List<NodeEvent>();
        private readonly object _bufferLock = new object();
        private System.Threading.Timer _flushTimer;

        public DatabaseService(string dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "history.db");
        }

        public void Initialize()
        {
            var dir = Path.GetDirectoryName(_dbPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
            _connection.Open();

            ExecuteNonQuery(@"
                CREATE TABLE IF NOT EXISTS ping_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    node_id TEXT NOT NULL,
                    latency_ms INTEGER,
                    packet_loss REAL,
                    status TEXT NOT NULL,
                    timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS module_results (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    node_id TEXT NOT NULL,
                    module_name TEXT NOT NULL,
                    success INTEGER NOT NULL,
                    details TEXT,
                    timestamp TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS events (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    node_id TEXT NOT NULL,
                    event_type TEXT NOT NULL,
                    message TEXT,
                    old_status TEXT,
                    new_status TEXT,
                    timestamp TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_ping_node_time ON ping_history(node_id, timestamp);
                CREATE INDEX IF NOT EXISTS idx_module_node_time ON module_results(node_id, timestamp);
                CREATE INDEX IF NOT EXISTS idx_events_node_time ON events(node_id, timestamp);
            ");

            _flushTimer = new System.Threading.Timer(_ => Flush(), null, 5000, 5000);
            Logger.Info("Database initialized: {0}", _dbPath);
        }

        public void SavePingResult(MonitoringResult result)
        {
            lock (_bufferLock)
            {
                _resultBuffer.Add(result);
                if (_resultBuffer.Count >= 100)
                    Flush();
            }
        }

        public void SaveModuleResult(MonitoringResult result)
        {
            lock (_bufferLock)
            {
                _resultBuffer.Add(result);
            }
        }

        public void SaveEvent(NodeEvent nodeEvent)
        {
            lock (_bufferLock)
            {
                _eventBuffer.Add(nodeEvent);
            }
            Flush(); // events flush immediately
        }

        public void Flush()
        {
            List<MonitoringResult> results;
            List<NodeEvent> events;

            lock (_bufferLock)
            {
                if (_resultBuffer.Count == 0 && _eventBuffer.Count == 0) return;
                results = new List<MonitoringResult>(_resultBuffer);
                events = new List<NodeEvent>(_eventBuffer);
                _resultBuffer.Clear();
                _eventBuffer.Clear();
            }

            try
            {
                using (var tx = _connection.BeginTransaction())
                {
                    foreach (var r in results)
                    {
                        if (r.ModuleName == "ping")
                        {
                            ExecuteNonQuery(
                                "INSERT INTO ping_history (node_id, latency_ms, packet_loss, status, timestamp) VALUES (@nid, @lat, @loss, @st, @ts)",
                                new SQLiteParameter("@nid", r.NodeId),
                                new SQLiteParameter("@lat", r.LatencyMs),
                                new SQLiteParameter("@loss", r.PacketLoss),
                                new SQLiteParameter("@st", r.Status.ToString()),
                                new SQLiteParameter("@ts", r.Timestamp.ToString("o")));
                        }

                        ExecuteNonQuery(
                            "INSERT INTO module_results (node_id, module_name, success, details, timestamp) VALUES (@nid, @mod, @suc, @det, @ts)",
                            new SQLiteParameter("@nid", r.NodeId),
                            new SQLiteParameter("@mod", r.ModuleName),
                            new SQLiteParameter("@suc", r.Success ? 1 : 0),
                            new SQLiteParameter("@det", r.Details),
                            new SQLiteParameter("@ts", r.Timestamp.ToString("o")));
                    }

                    foreach (var e in events)
                    {
                        ExecuteNonQuery(
                            "INSERT INTO events (node_id, event_type, message, old_status, new_status, timestamp) VALUES (@nid, @et, @msg, @old, @new, @ts)",
                            new SQLiteParameter("@nid", e.NodeId),
                            new SQLiteParameter("@et", e.EventType),
                            new SQLiteParameter("@msg", e.Message),
                            new SQLiteParameter("@old", e.OldStatus?.ToString()),
                            new SQLiteParameter("@new", e.NewStatus?.ToString()),
                            new SQLiteParameter("@ts", e.Timestamp.ToString("o")));
                    }

                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Database flush error");
            }
        }

        public List<MonitoringResult> GetPingHistory(string nodeId, DateTime from, DateTime to)
        {
            var list = new List<MonitoringResult>();
            using (var cmd = new SQLiteCommand(
                "SELECT node_id, latency_ms, packet_loss, status, timestamp FROM ping_history WHERE node_id=@nid AND timestamp BETWEEN @from AND @to ORDER BY timestamp",
                _connection))
            {
                cmd.Parameters.AddWithValue("@nid", nodeId);
                cmd.Parameters.AddWithValue("@from", from.ToString("o"));
                cmd.Parameters.AddWithValue("@to", to.ToString("o"));

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MonitoringResult
                        {
                            NodeId = reader.GetString(0),
                            ModuleName = "ping",
                            LatencyMs = reader.IsDBNull(1) ? -1 : reader.GetInt64(1),
                            PacketLoss = reader.IsDBNull(2) ? 1.0 : reader.GetDouble(2),
                            Status = Enum.TryParse<NodeStatus>(reader.GetString(3), out var s) ? s : NodeStatus.Unknown,
                            Timestamp = DateTime.Parse(reader.GetString(4))
                        });
                    }
                }
            }
            return list;
        }

        public double GetUptime(string nodeId, DateTime from, DateTime to)
        {
            using (var cmd = new SQLiteCommand(
                "SELECT COUNT(*) FROM ping_history WHERE node_id=@nid AND timestamp BETWEEN @from AND @to",
                _connection))
            {
                cmd.Parameters.AddWithValue("@nid", nodeId);
                cmd.Parameters.AddWithValue("@from", from.ToString("o"));
                cmd.Parameters.AddWithValue("@to", to.ToString("o"));
                long total = (long)cmd.ExecuteScalar();
                if (total == 0) return 0;

                using (var cmd2 = new SQLiteCommand(
                    "SELECT COUNT(*) FROM ping_history WHERE node_id=@nid AND timestamp BETWEEN @from AND @to AND status IN ('Online','Unstable')",
                    _connection))
                {
                    cmd2.Parameters.AddWithValue("@nid", nodeId);
                    cmd2.Parameters.AddWithValue("@from", from.ToString("o"));
                    cmd2.Parameters.AddWithValue("@to", to.ToString("o"));
                    long online = (long)cmd2.ExecuteScalar();
                    return (double)online / total * 100.0;
                }
            }
        }

        public List<NodeEvent> GetEvents(string nodeId, int limit = 100)
        {
            return ReadEvents(
                "SELECT id, node_id, event_type, message, old_status, new_status, timestamp FROM events WHERE node_id=@nid ORDER BY timestamp DESC LIMIT @lim",
                new SQLiteParameter("@nid", nodeId),
                new SQLiteParameter("@lim", limit));
        }

        public List<NodeEvent> GetAllEvents(int limit = 500)
        {
            return ReadEvents(
                "SELECT id, node_id, event_type, message, old_status, new_status, timestamp FROM events ORDER BY timestamp DESC LIMIT @lim",
                new SQLiteParameter("@lim", limit));
        }

        private List<NodeEvent> ReadEvents(string sql, params SQLiteParameter[] parameters)
        {
            var list = new List<NodeEvent>();
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddRange(parameters);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new NodeEvent
                        {
                            Id = reader.GetInt64(0),
                            NodeId = reader.GetString(1),
                            EventType = reader.GetString(2),
                            Message = reader.IsDBNull(3) ? null : reader.GetString(3),
                            OldStatus = reader.IsDBNull(4) ? (NodeStatus?)null : (Enum.TryParse<NodeStatus>(reader.GetString(4), out var os) ? os : (NodeStatus?)null),
                            NewStatus = reader.IsDBNull(5) ? (NodeStatus?)null : (Enum.TryParse<NodeStatus>(reader.GetString(5), out var ns) ? ns : (NodeStatus?)null),
                            Timestamp = DateTime.Parse(reader.GetString(6))
                        });
                    }
                }
            }
            return list;
        }

        public void Cleanup(int keepDays = 90)
        {
            var cutoff = DateTime.UtcNow.AddDays(-keepDays).ToString("o");
            ExecuteNonQuery("DELETE FROM ping_history WHERE timestamp < @ts", new SQLiteParameter("@ts", cutoff));
            ExecuteNonQuery("DELETE FROM module_results WHERE timestamp < @ts", new SQLiteParameter("@ts", cutoff));
            ExecuteNonQuery("DELETE FROM events WHERE timestamp < @ts", new SQLiteParameter("@ts", cutoff));
            Logger.Info("Database cleanup: removed records older than {0} days", keepDays);
        }

        private void ExecuteNonQuery(string sql, params SQLiteParameter[] parameters)
        {
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            Flush();
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
```

**Step 2: Add to .csproj**

```xml
<Compile Include="Services\DatabaseService.cs" />
```

**Step 3: Build and commit**

```bash
git add Services/DatabaseService.cs NetworkMonitor.csproj
git commit -m "feat: add SQLite DatabaseService with buffered writes"
```

---

## Task 9: NotificationService + NLog Config

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Services\NotificationService.cs`
- Create: `D:\Ягор\NetworkMonitor\NLog.config`

**Step 1: Create NotificationService.cs**

```csharp
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using NetworkMonitor.Models;
using NLog;

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

            // Toast
            if (_settings.ToastEnabled)
            {
                var type = ToastType.Info;
                if (nodeEvent.NewStatus == NodeStatus.Offline) type = ToastType.Error;
                else if (nodeEvent.NewStatus == NodeStatus.Unstable) type = ToastType.Warning;

                ShowToast(nodeEvent.Message, type);
            }

            // Sound (only on transition to Offline)
            if (_settings.SoundEnabled && nodeEvent.NewStatus == NodeStatus.Offline)
            {
                PlaySound();
            }

            // Email
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

                // Keep max 5
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
                using (var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword);

                    var msg = new MailMessage(_settings.EmailFrom, _settings.EmailTo, subject, body);
                    client.Send(msg);
                    Logger.Info("Email sent: {0}", subject);
                }
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
```

**Step 2: Create NLog.config**

Create `D:\Ягор\NetworkMonitor\NLog.config`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

  <targets>
    <target name="file" xsi:type="File"
            fileName="${basedir}/logs/app.log"
            archiveFileName="${basedir}/logs/app.{#}.log"
            archiveNumbering="Rolling"
            maxArchiveFiles="5"
            archiveAboveSize="5242880"
            layout="${longdate} ${level:uppercase=true} ${logger:shortName=true} ${message} ${exception:format=tostring}" />

    <target name="console" xsi:type="Console"
            layout="${shortdate} ${level:uppercase=true} ${message}" />
  </targets>

  <rules>
    <logger name="*" minlevel="Info" writeTo="file" />
    <logger name="*" minlevel="Debug" writeTo="console" />
  </rules>
</nlog>
```

**Step 3: Add NLog.config to .csproj (Copy to output)**

```xml
<None Include="NLog.config">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

And add `NotificationService.cs`:

```xml
<Compile Include="Services\NotificationService.cs" />
```

**Step 4: Build and commit**

```bash
git add Services/NotificationService.cs NLog.config NetworkMonitor.csproj
git commit -m "feat: add NotificationService (toast/sound/email) and NLog config"
```

---

## Task 10: Update MainViewModel

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\ViewModels\MainViewModel.cs`
- Delete: `D:\Ягор\NetworkMonitor\Services\PingService.cs`

**Step 1: Replace MainViewModel.cs entirely**

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NetworkMonitor.Models;
using NetworkMonitor.Services;

namespace NetworkMonitor.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly StorageService _storage = new StorageService();
        private readonly ModuleRegistry _moduleRegistry;
        private readonly MonitoringScheduler _scheduler;
        private readonly DatabaseService _database;
        private readonly NotificationService _notifications;

        public ObservableCollection<NetworkNode> Nodes { get; set; }
        public ObservableCollection<NodeLink> Links { get; set; }
        public AppSettings Settings { get; set; }

        public ObservableCollection<ToastNotification> Toasts => _notifications.Toasts;

        private NetworkNode _selectedNode;
        public NetworkNode SelectedNode
        {
            get => _selectedNode;
            set { _selectedNode = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelectedNode)); }
        }

        public bool HasSelectedNode => _selectedNode != null;

        private bool _isMonitoring;
        public bool IsMonitoring
        {
            get => _isMonitoring;
            set { _isMonitoring = value; OnPropertyChanged(); OnPropertyChanged(nameof(MonitoringLabel)); }
        }

        public string MonitoringLabel => IsMonitoring ? "\u23F9 \u0421\u0442\u043E\u043F" : "\u25B6 \u0421\u0442\u0430\u0440\u0442";

        public bool HasNodes => Nodes.Count > 0;

        // Dashboard counters
        private int _onlineCount;
        public int OnlineCount { get => _onlineCount; set { _onlineCount = value; OnPropertyChanged(); } }

        private int _offlineCount;
        public int OfflineCount { get => _offlineCount; set { _offlineCount = value; OnPropertyChanged(); } }

        private int _unstableCount;
        public int UnstableCount { get => _unstableCount; set { _unstableCount = value; OnPropertyChanged(); } }

        public ICommand ToggleMonitoringCommand { get; }
        public ICommand SaveCommand { get; }

        public DatabaseService Database => _database;
        public NotificationService Notifications => _notifications;

        public MainViewModel()
        {
            Settings = _storage.LoadSettings() ?? new AppSettings();
            Nodes = new ObservableCollection<NetworkNode>(_storage.LoadNodes() ?? new System.Collections.Generic.List<NetworkNode>());
            Links = new ObservableCollection<NodeLink>(_storage.LoadLinks() ?? new System.Collections.Generic.List<NodeLink>());

            _moduleRegistry = new ModuleRegistry(Settings);
            _scheduler = new MonitoringScheduler(_moduleRegistry)
            {
                DefaultIntervalSec = Settings.PingIntervalSeconds
            };

            _database = new DatabaseService();
            _database.Initialize();

            _notifications = new NotificationService(Settings);

            // Subscribe to EventBus
            EventBus.Instance.OnResult += OnMonitoringResult;
            EventBus.Instance.OnEvent += OnNodeEvent;

            ToggleMonitoringCommand = new RelayCommand(ToggleMonitoring);
            SaveCommand = new RelayCommand(Save);

            Nodes.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasNodes));
        }

        private void ToggleMonitoring()
        {
            if (IsMonitoring)
            {
                _scheduler.Stop();
                IsMonitoring = false;
            }
            else
            {
                _scheduler.Start(Nodes);
                IsMonitoring = true;
            }
        }

        private void Save()
        {
            _storage.SaveNodes(Nodes.ToList());
            _storage.SaveLinks(Links.ToList());
            _storage.SaveSettings(Settings);
        }

        private void OnMonitoringResult(MonitoringResult result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var node = Nodes.FirstOrDefault(n => n.Id == result.NodeId);
                if (node == null) return;

                if (result.ModuleName == "ping")
                {
                    node.Status = result.Status;
                    node.LastPingMs = result.LatencyMs;
                    node.LastSeen = result.Timestamp;
                }

                UpdateCounters();
            });

            // Save to database
            if (result.ModuleName == "ping")
                _database.SavePingResult(result);
            else
                _database.SaveModuleResult(result);
        }

        private void OnNodeEvent(NodeEvent nodeEvent)
        {
            _database.SaveEvent(nodeEvent);
            _notifications.OnNodeEvent(nodeEvent);
        }

        public void UpdateCounters()
        {
            OnlineCount = Nodes.Count(n => n.Status == NodeStatus.Online);
            OfflineCount = Nodes.Count(n => n.Status == NodeStatus.Offline);
            UnstableCount = Nodes.Count(n => n.Status == NodeStatus.Unstable);
        }

        public void UpdateSchedulerNodes()
        {
            if (IsMonitoring)
                _scheduler.UpdateNodes(Nodes);
        }

        public void UpdateSettings()
        {
            _moduleRegistry.UpdateSettings(Settings);
            _scheduler.DefaultIntervalSec = Settings.PingIntervalSeconds;
            _notifications.UpdateSettings(Settings);
        }

        public void Shutdown()
        {
            _scheduler.Stop();
            _database.Dispose();
        }
    }
}
```

**Step 2: Delete PingService.cs**

Delete `D:\Ягор\NetworkMonitor\Services\PingService.cs` and remove `<Compile Include="Services\PingService.cs" />` from `.csproj`.

**Step 3: Build and commit**

```bash
git add ViewModels/MainViewModel.cs NetworkMonitor.csproj
git rm Services/PingService.cs
git commit -m "feat: replace PingService with modular scheduler in MainViewModel"
```

---

## Task 11: StatisticsViewModel + EventLogViewModel

**Files:**
- Create: `D:\Ягор\NetworkMonitor\ViewModels\StatisticsViewModel.cs`
- Create: `D:\Ягор\NetworkMonitor\ViewModels\EventLogViewModel.cs`

**Step 1: Create StatisticsViewModel.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NetworkMonitor.ViewModels
{
    public class UptimeRow
    {
        public string NodeName { get; set; }
        public string Uptime24h { get; set; }
        public string Uptime7d { get; set; }
        public string Uptime30d { get; set; }
    }

    public class StatisticsViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly ObservableCollection<NetworkNode> _nodes;

        public PlotModel LatencyPlot { get; private set; }
        public PlotModel PacketLossPlot { get; private set; }
        public ObservableCollection<UptimeRow> UptimeRows { get; } = new ObservableCollection<UptimeRow>();

        private int _onlineCount;
        public int OnlineCount { get => _onlineCount; set { _onlineCount = value; OnPropertyChanged(); } }

        private int _offlineCount;
        public int OfflineCount { get => _offlineCount; set { _offlineCount = value; OnPropertyChanged(); } }

        private int _unstableCount;
        public int UnstableCount { get => _unstableCount; set { _unstableCount = value; OnPropertyChanged(); } }

        private string _selectedPeriod = "24h";
        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set { _selectedPeriod = value; OnPropertyChanged(); Refresh(); }
        }

        public List<string> Periods { get; } = new List<string> { "1h", "24h", "7d", "30d" };

        public StatisticsViewModel(DatabaseService database, ObservableCollection<NetworkNode> nodes)
        {
            _database = database;
            _nodes = nodes;
            Refresh();
        }

        public void Refresh()
        {
            OnlineCount = _nodes.Count(n => n.Status == NodeStatus.Online);
            OfflineCount = _nodes.Count(n => n.Status == NodeStatus.Offline);
            UnstableCount = _nodes.Count(n => n.Status == NodeStatus.Unstable);

            var (from, to) = GetPeriodRange();
            BuildLatencyPlot(from, to);
            BuildPacketLossPlot(from, to);
            BuildUptimeTable();
        }

        private (DateTime from, DateTime to) GetPeriodRange()
        {
            var to = DateTime.UtcNow;
            DateTime from;
            switch (_selectedPeriod)
            {
                case "1h": from = to.AddHours(-1); break;
                case "7d": from = to.AddDays(-7); break;
                case "30d": from = to.AddDays(-30); break;
                default: from = to.AddHours(-24); break;
            }
            return (from, to);
        }

        private void BuildLatencyPlot(DateTime from, DateTime to)
        {
            var model = new PlotModel { Title = "Latency (ms)" };
            model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "HH:mm" });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0 });

            foreach (var node in _nodes.Take(10))
            {
                var history = _database.GetPingHistory(node.Id, from, to);
                if (history.Count == 0) continue;

                var series = new LineSeries { Title = node.Name };
                foreach (var r in history.Where(h => h.LatencyMs >= 0))
                {
                    series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(r.Timestamp), r.LatencyMs));
                }
                model.Series.Add(series);
            }

            LatencyPlot = model;
            OnPropertyChanged(nameof(LatencyPlot));
        }

        private void BuildPacketLossPlot(DateTime from, DateTime to)
        {
            var model = new PlotModel { Title = "Packet Loss (%)" };
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
            model.Axes.Add(categoryAxis);
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 100 });

            var series = new BarSeries();

            foreach (var node in _nodes)
            {
                var history = _database.GetPingHistory(node.Id, from, to);
                if (history.Count == 0) continue;

                double avgLoss = history.Average(h => h.PacketLoss) * 100;
                categoryAxis.Labels.Add(node.Name);
                series.Items.Add(new BarItem(avgLoss));
            }

            model.Series.Add(series);
            PacketLossPlot = model;
            OnPropertyChanged(nameof(PacketLossPlot));
        }

        private void BuildUptimeTable()
        {
            UptimeRows.Clear();
            var now = DateTime.UtcNow;

            foreach (var node in _nodes)
            {
                UptimeRows.Add(new UptimeRow
                {
                    NodeName = node.Name,
                    Uptime24h = $"{_database.GetUptime(node.Id, now.AddHours(-24), now):F1}%",
                    Uptime7d = $"{_database.GetUptime(node.Id, now.AddDays(-7), now):F1}%",
                    Uptime30d = $"{_database.GetUptime(node.Id, now.AddDays(-30), now):F1}%"
                });
            }
        }
    }
}
```

**Step 2: Create EventLogViewModel.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetworkMonitor.Models;
using NetworkMonitor.Services;

namespace NetworkMonitor.ViewModels
{
    public class EventLogViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly ObservableCollection<NetworkNode> _nodes;

        public ObservableCollection<NodeEvent> Events { get; } = new ObservableCollection<NodeEvent>();
        public List<string> NodeNames { get; } = new List<string>();
        public List<string> EventTypes { get; } = new List<string> { "All", "StatusChanged", "Alert", "ModuleError" };

        private string _selectedNodeName = "All";
        public string SelectedNodeName
        {
            get => _selectedNodeName;
            set { _selectedNodeName = value; OnPropertyChanged(); Refresh(); }
        }

        private string _selectedEventType = "All";
        public string SelectedEventType
        {
            get => _selectedEventType;
            set { _selectedEventType = value; OnPropertyChanged(); Refresh(); }
        }

        private DateTime? _dateFrom;
        public DateTime? DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); Refresh(); }
        }

        private DateTime? _dateTo;
        public DateTime? DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); Refresh(); }
        }

        public EventLogViewModel(DatabaseService database, ObservableCollection<NetworkNode> nodes)
        {
            _database = database;
            _nodes = nodes;

            NodeNames.Add("All");
            NodeNames.AddRange(nodes.Select(n => n.Name));

            Refresh();
        }

        public void Refresh()
        {
            Events.Clear();

            var allEvents = _database.GetAllEvents(500);

            var filtered = allEvents.AsEnumerable();

            if (_selectedNodeName != "All")
            {
                var node = _nodes.FirstOrDefault(n => n.Name == _selectedNodeName);
                if (node != null)
                    filtered = filtered.Where(e => e.NodeId == node.Id);
            }

            if (_selectedEventType != "All")
                filtered = filtered.Where(e => e.EventType == _selectedEventType);

            if (_dateFrom.HasValue)
                filtered = filtered.Where(e => e.Timestamp >= _dateFrom.Value);

            if (_dateTo.HasValue)
                filtered = filtered.Where(e => e.Timestamp <= _dateTo.Value.AddDays(1));

            foreach (var ev in filtered)
                Events.Add(ev);
        }
    }
}
```

**Step 3: Add to .csproj**

```xml
<Compile Include="ViewModels\StatisticsViewModel.cs" />
<Compile Include="ViewModels\EventLogViewModel.cs" />
```

**Step 4: Build and commit**

```bash
git add ViewModels/StatisticsViewModel.cs ViewModels/EventLogViewModel.cs NetworkMonitor.csproj
git commit -m "feat: add StatisticsViewModel and EventLogViewModel"
```

---

## Task 12: Update Converters

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\ViewModels\Converters.cs`

**Step 1: Replace Converters.cs**

```csharp
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
            => (value is bool b && b) ? "\u0410\u043A\u0442\u0438\u0432\u0435\u043D" : "\u041E\u0441\u0442\u0430\u043D\u043E\u0432\u043B\u0435\u043D";

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
```

**Step 2: Register new converters in App.xaml**

Add inside `<Application.Resources>` → `<ResourceDictionary>`:

```xml
<local:StatusToColorConverter x:Key="StatusToColorConverter"/>
<local:ToastTypeToColorConverter x:Key="ToastTypeToColorConverter"/>
<local:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibilityConverter"/>
```

Make sure xmlns:local is declared: `xmlns:local="clr-namespace:NetworkMonitor.ViewModels"`

**Step 3: Build and commit**

```bash
git add ViewModels/Converters.cs App.xaml
git commit -m "feat: add StatusToColor, ToastTypeToColor, InverseBoolToVisibility converters"
```

---

## Task 13: NodeDetailView (UserControl)

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Views\NodeDetailView.xaml`
- Create: `D:\Ягор\NetworkMonitor\Views\NodeDetailView.xaml.cs`

**Step 1: Create NodeDetailView.xaml**

```xml
<UserControl x:Class="NetworkMonitor.Views.NodeDetailView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:oxy="http://oxyplot.org/wpf"
             xmlns:local="clr-namespace:NetworkMonitor.ViewModels">
    <UserControl.Resources>
        <local:StatusToColorConverter x:Key="StatusToColor"/>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="12">
            <!-- Header -->
            <TextBlock Text="{Binding Name}" FontSize="18" FontWeight="Bold" Margin="0,0,0,4"/>
            <TextBlock Text="{Binding IpAddress}" FontSize="14" Foreground="Gray" Margin="0,0,0,8"/>

            <!-- Status -->
            <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                <Ellipse Width="12" Height="12" Fill="{Binding Status, Converter={StaticResource StatusToColor}}" Margin="0,0,6,0"/>
                <TextBlock Text="{Binding Status}" FontSize="14"/>
            </StackPanel>

            <!-- Info -->
            <TextBlock Margin="0,0,0,4">
                <Run Text="Тип: " FontWeight="SemiBold"/>
                <Run Text="{Binding DeviceType, Mode=OneWay}"/>
            </TextBlock>
            <TextBlock Margin="0,0,0,4">
                <Run Text="Ping: " FontWeight="SemiBold"/>
                <Run Text="{Binding LastPingMs, StringFormat={}{0} ms, Mode=OneWay}"/>
            </TextBlock>
            <TextBlock Margin="0,0,0,12">
                <Run Text="Последний раз: " FontWeight="SemiBold"/>
                <Run Text="{Binding LastSeen, StringFormat=HH:mm:ss, Mode=OneWay}"/>
            </TextBlock>

            <!-- Mini latency chart -->
            <TextBlock Text="Latency" FontWeight="SemiBold" Margin="0,0,0,4"/>
            <oxy:PlotView x:Name="LatencyChart" Height="120" Margin="0,0,0,12"/>

            <!-- Action buttons -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="Ping" Click="PingNow_Click" Margin="0,0,8,0"
                        Style="{StaticResource MaterialDesignOutlinedButton}" Padding="8,4"/>
                <Button Content="Удалить" Click="DeleteNode_Click"
                        Style="{StaticResource MaterialDesignOutlinedButton}" Padding="8,4"
                        Foreground="#F44336"/>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

**Step 2: Create NodeDetailView.xaml.cs**

```csharp
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace NetworkMonitor.Views
{
    public partial class NodeDetailView : UserControl
    {
        public event EventHandler<NetworkNode> DeleteRequested;
        public event EventHandler<NetworkNode> PingRequested;

        private DatabaseService _database;

        public NodeDetailView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        public void SetDatabase(DatabaseService database)
        {
            _database = database;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is NetworkNode node && _database != null)
            {
                RefreshChart(node);
            }
        }

        public void RefreshChart(NetworkNode node)
        {
            if (_database == null || node == null) return;

            var history = _database.GetPingHistory(node.Id, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow);

            var model = new PlotModel();
            model.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "HH:mm",
                IsAxisVisible = true,
                MajorGridlineStyle = LineStyle.Dot
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = 0,
                Title = "ms"
            });

            var series = new LineSeries
            {
                Color = OxyColors.CornflowerBlue,
                StrokeThickness = 2
            };

            foreach (var r in history.Where(h => h.LatencyMs >= 0).TakeLast(100))
            {
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(r.Timestamp), r.LatencyMs));
            }

            model.Series.Add(series);
            LatencyChart.Model = model;
        }

        private void PingNow_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkNode node)
                PingRequested?.Invoke(this, node);
        }

        private void DeleteNode_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is NetworkNode node)
            {
                var result = MessageBox.Show(
                    $"Удалить узел \"{node.Name}\"?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    DeleteRequested?.Invoke(this, node);
            }
        }
    }
}
```

**Step 3: Add to .csproj**

```xml
<Page Include="Views\NodeDetailView.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
<Compile Include="Views\NodeDetailView.xaml.cs">
  <DependentUpon>NodeDetailView.xaml</DependentUpon>
</Compile>
```

**Step 4: Build and commit**

```bash
git add Views/NodeDetailView.xaml Views/NodeDetailView.xaml.cs NetworkMonitor.csproj
git commit -m "feat: add NodeDetailView with latency chart"
```

---

## Task 14: Update AddNodeDialog (module config section)

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\Views\AddNodeDialog.xaml`
- Modify: `D:\Ягор\NetworkMonitor\Views\AddNodeDialog.xaml.cs`

**Step 1: Replace AddNodeDialog.xaml**

```xml
<Window x:Class="NetworkMonitor.Views.AddNodeDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="Добавить узел" Width="420" Height="600"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize"
        Style="{StaticResource MaterialDesignWindow}">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="20">
            <TextBlock Text="Новый узел" FontSize="20" FontWeight="Bold" Margin="0,0,0,16"/>

            <TextBox x:Name="NameBox" md:HintAssist.Hint="Имя устройства" Margin="0,0,0,8"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
            <TextBox x:Name="IpBox" md:HintAssist.Hint="IP-адрес" Margin="0,0,0,8"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
            <ComboBox x:Name="TypeBox" md:HintAssist.Hint="Тип устройства" Margin="0,0,0,8"
                      Style="{StaticResource MaterialDesignOutlinedComboBox}" SelectedIndex="0">
                <ComboBoxItem Content="Router"/>
                <ComboBoxItem Content="Switch"/>
                <ComboBoxItem Content="Server"/>
                <ComboBoxItem Content="PC"/>
                <ComboBoxItem Content="Camera"/>
                <ComboBoxItem Content="Special"/>
                <ComboBoxItem Content="Other"/>
            </ComboBox>
            <TextBox x:Name="LatBox" md:HintAssist.Hint="Широта (-90..90)" Margin="0,0,0,8"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
            <TextBox x:Name="LonBox" md:HintAssist.Hint="Долгота (-180..180)" Margin="0,0,0,8"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
            <TextBox x:Name="DescBox" md:HintAssist.Hint="Описание" Margin="0,0,0,16"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"/>

            <!-- Модули мониторинга -->
            <TextBlock Text="Модули мониторинга" FontSize="16" FontWeight="SemiBold" Margin="0,0,0,12"/>

            <!-- Ping -->
            <StackPanel Margin="0,0,0,8">
                <StackPanel Orientation="Horizontal">
                    <md:PackIcon Kind="LanConnect" VerticalAlignment="Center" Margin="0,0,8,0"/>
                    <TextBlock Text="Ping (всегда включён)" VerticalAlignment="Center" FontWeight="SemiBold"/>
                </StackPanel>
                <StackPanel Orientation="Horizontal" Margin="28,4,0,0">
                    <TextBlock Text="Интервал (сек):" VerticalAlignment="Center" Margin="0,0,8,0"/>
                    <TextBox x:Name="PingIntervalBox" Width="60" Text="5"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}" Padding="4,2"/>
                </StackPanel>
            </StackPanel>

            <!-- TCP -->
            <StackPanel Margin="0,0,0,8">
                <StackPanel Orientation="Horizontal">
                    <ToggleButton x:Name="TcpToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}" Margin="0,0,8,0"/>
                    <TextBlock Text="TCP Ports" VerticalAlignment="Center" FontWeight="SemiBold"/>
                </StackPanel>
                <TextBox x:Name="TcpPortsBox" md:HintAssist.Hint="Порты (через запятую)" Text="80, 443"
                         Style="{StaticResource MaterialDesignOutlinedTextBox}" Margin="28,4,0,0"
                         IsEnabled="{Binding IsChecked, ElementName=TcpToggle}"/>
            </StackPanel>

            <!-- SNMP -->
            <StackPanel Margin="0,0,0,8">
                <StackPanel Orientation="Horizontal">
                    <ToggleButton x:Name="SnmpToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}" Margin="0,0,8,0"/>
                    <TextBlock Text="SNMP" VerticalAlignment="Center" FontWeight="SemiBold"/>
                </StackPanel>
                <StackPanel Margin="28,4,0,0" IsEnabled="{Binding IsChecked, ElementName=SnmpToggle}">
                    <TextBox x:Name="SnmpCommunityBox" md:HintAssist.Hint="Community" Text="public"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}" Margin="0,0,0,4"/>
                    <ComboBox x:Name="SnmpVersionBox" md:HintAssist.Hint="Версия"
                              Style="{StaticResource MaterialDesignOutlinedComboBox}" SelectedIndex="1">
                        <ComboBoxItem Content="v1"/>
                        <ComboBoxItem Content="v2c"/>
                    </ComboBox>
                </StackPanel>
            </StackPanel>

            <!-- Traceroute -->
            <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
                <ToggleButton x:Name="TracerouteToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}" Margin="0,0,8,0"/>
                <TextBlock Text="Traceroute" VerticalAlignment="Center" FontWeight="SemiBold"/>
            </StackPanel>

            <!-- Buttons -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="Отмена" IsCancel="True" Margin="0,0,8,0"
                        Style="{StaticResource MaterialDesignOutlinedButton}"/>
                <Button Content="Добавить" Click="AddButton_Click"
                        Style="{StaticResource MaterialDesignRaisedButton}"/>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</Window>
```

**Step 2: Replace AddNodeDialog.xaml.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using NetworkMonitor.Models;

namespace NetworkMonitor.Views
{
    public partial class AddNodeDialog : Window
    {
        public NetworkNode ResultNode { get; private set; }

        public AddNodeDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите имя устройства.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(IpBox.Text))
            {
                MessageBox.Show("Введите IP-адрес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseCoordinate(LatBox.Text, -90, 90, out double lat))
            {
                MessageBox.Show("Некорректная широта (-90..90).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseCoordinate(LonBox.Text, -180, 180, out double lon))
            {
                MessageBox.Show("Некорректная долгота (-180..180).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse ping interval
            if (!int.TryParse(PingIntervalBox.Text, out int pingInterval) || pingInterval < 1)
                pingInterval = 5;

            // Parse TCP ports
            var ports = new List<int>();
            if (TcpToggle.IsChecked == true)
            {
                foreach (var part in TcpPortsBox.Text.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int port) && port > 0 && port <= 65535)
                        ports.Add(port);
                }
            }

            // SNMP version
            string snmpVersion = (SnmpVersionBox.SelectedIndex == 0) ? "1" : "2c";

            var node = new NetworkNode
            {
                Name = NameBox.Text.Trim(),
                IpAddress = IpBox.Text.Trim(),
                DeviceType = ((System.Windows.Controls.ComboBoxItem)TypeBox.SelectedItem)?.Content?.ToString() ?? "Other",
                Latitude = lat,
                Longitude = lon,
                Description = DescBox.Text?.Trim() ?? "",
                Status = NodeStatus.Unknown,
                Monitoring = new MonitoringConfig
                {
                    Ping = new PingConfig { Enabled = true, IntervalSec = pingInterval },
                    Tcp = new TcpConfig { Enabled = TcpToggle.IsChecked == true, Ports = ports.Any() ? ports : new List<int> { 80, 443 } },
                    Snmp = new SnmpConfig
                    {
                        Enabled = SnmpToggle.IsChecked == true,
                        Community = SnmpCommunityBox.Text?.Trim() ?? "public",
                        Version = snmpVersion
                    },
                    Traceroute = new TracerouteConfig { Enabled = TracerouteToggle.IsChecked == true }
                }
            };

            ResultNode = node;
            DialogResult = true;
            Close();
        }

        private bool TryParseCoordinate(string text, double min, double max, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Replace(',', '.');
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return false;
            return value >= min && value <= max;
        }
    }
}
```

**Step 3: Build and commit**

```bash
git add Views/AddNodeDialog.xaml Views/AddNodeDialog.xaml.cs
git commit -m "feat: add module config section to AddNodeDialog"
```

---

## Task 15: Update SettingsDialog (tabbed)

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\Views\SettingsDialog.xaml`
- Modify: `D:\Ягор\NetworkMonitor\Views\SettingsDialog.xaml.cs`

**Step 1: Replace SettingsDialog.xaml**

```xml
<Window x:Class="NetworkMonitor.Views.SettingsDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="Настройки" Width="480" Height="520"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize"
        Style="{StaticResource MaterialDesignWindow}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TabControl Style="{StaticResource MaterialDesignNavigationRailTabControl}" Margin="8">
            <!-- Мониторинг -->
            <TabItem Header="Мониторинг">
                <StackPanel Margin="16">
                    <TextBox x:Name="IntervalBox" md:HintAssist.Hint="Интервал ping (сек)" Margin="0,0,0,8"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                    <TextBox x:Name="TimeoutBox" md:HintAssist.Hint="Таймаут ping (мс)" Margin="0,0,0,8"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                    <TextBox x:Name="RetriesBox" md:HintAssist.Hint="Кол-во попыток" Margin="0,0,0,8"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                    <TextBox x:Name="UnstableBox" md:HintAssist.Hint="Порог нестабильности (мс)" Margin="0,0,0,8"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                </StackPanel>
            </TabItem>

            <!-- Уведомления -->
            <TabItem Header="Уведомления">
                <StackPanel Margin="16">
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
                        <ToggleButton x:Name="ToastToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}" Margin="0,0,8,0"/>
                        <TextBlock Text="Toast-уведомления" VerticalAlignment="Center"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
                        <ToggleButton x:Name="SoundToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}" Margin="0,0,8,0"/>
                        <TextBlock Text="Звуковые оповещения" VerticalAlignment="Center"/>
                    </StackPanel>
                    <TextBox x:Name="SoundDebounceBox" md:HintAssist.Hint="Пауза между звуками (сек)" Margin="0,0,0,8"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                </StackPanel>
            </TabItem>

            <!-- Email -->
            <TabItem Header="Email">
                <StackPanel Margin="16">
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
                        <ToggleButton x:Name="EmailToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}" Margin="0,0,8,0"/>
                        <TextBlock Text="Отправка email" VerticalAlignment="Center"/>
                    </StackPanel>
                    <StackPanel IsEnabled="{Binding IsChecked, ElementName=EmailToggle}">
                        <TextBox x:Name="SmtpHostBox" md:HintAssist.Hint="SMTP сервер" Margin="0,0,0,8"
                                 Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                        <TextBox x:Name="SmtpPortBox" md:HintAssist.Hint="Порт" Margin="0,0,0,8"
                                 Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                        <TextBox x:Name="SmtpUserBox" md:HintAssist.Hint="Пользователь" Margin="0,0,0,8"
                                 Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                        <PasswordBox x:Name="SmtpPasswordBox" md:HintAssist.Hint="Пароль" Margin="0,0,0,8"
                                     Style="{StaticResource MaterialDesignOutlinedPasswordBox}"/>
                        <TextBox x:Name="EmailFromBox" md:HintAssist.Hint="От кого" Margin="0,0,0,8"
                                 Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                        <TextBox x:Name="EmailToBox" md:HintAssist.Hint="Кому" Margin="0,0,0,8"
                                 Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                        <Button Content="Тест" Click="TestEmail_Click" Margin="0,0,0,8"
                                Style="{StaticResource MaterialDesignOutlinedButton}" HorizontalAlignment="Left"/>
                    </StackPanel>
                </StackPanel>
            </TabItem>

            <!-- Оформление -->
            <TabItem Header="Оформление">
                <StackPanel Margin="16">
                    <StackPanel Orientation="Horizontal" Margin="0,0,0,12">
                        <ToggleButton x:Name="DarkThemeToggle" Style="{StaticResource MaterialDesignSwitchToggleButton}"
                                      Checked="DarkThemeToggle_Checked" Unchecked="DarkThemeToggle_Unchecked" Margin="0,0,8,0"/>
                        <TextBlock Text="Тёмная тема" VerticalAlignment="Center"/>
                    </StackPanel>
                </StackPanel>
            </TabItem>

            <!-- Данные -->
            <TabItem Header="Данные">
                <StackPanel Margin="16">
                    <TextBox x:Name="KeepDaysBox" md:HintAssist.Hint="Хранить историю (дней)" Margin="0,0,0,12"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"/>
                    <Button Content="Очистить историю" Click="ClearHistory_Click" Margin="0,0,0,8"
                            Style="{StaticResource MaterialDesignOutlinedButton}" HorizontalAlignment="Left"/>
                    <Button Content="Экспорт конфигурации" Click="ExportConfig_Click"
                            Style="{StaticResource MaterialDesignOutlinedButton}" HorizontalAlignment="Left"/>
                </StackPanel>
            </TabItem>
        </TabControl>

        <!-- Bottom buttons -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Margin="16">
            <Button Content="Отмена" IsCancel="True" Margin="0,0,8,0"
                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
            <Button Content="Сохранить" Click="SaveButton_Click"
                    Style="{StaticResource MaterialDesignRaisedButton}"/>
        </StackPanel>
    </Grid>
</Window>
```

**Step 2: Replace SettingsDialog.xaml.cs**

```csharp
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows;
using MaterialDesignThemes.Wpf;
using NetworkMonitor.Models;
using NetworkMonitor.Services;

namespace NetworkMonitor.Views
{
    public partial class SettingsDialog : Window
    {
        private readonly AppSettings _settings;
        private DatabaseService _database;

        public SettingsDialog(AppSettings settings, DatabaseService database = null)
        {
            InitializeComponent();
            _settings = settings;
            _database = database;
            LoadSettings();
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

            DarkThemeToggle.IsChecked = _settings.DarkTheme;
            KeepDaysBox.Text = _settings.HistoryKeepDays.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(IntervalBox.Text, out int interval) || interval < 1)
            {
                MessageBox.Show("Интервал должен быть >= 1 сек.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TimeoutBox.Text, out int timeout) || timeout < 100)
            {
                MessageBox.Show("Таймаут должен быть >= 100 мс.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(RetriesBox.Text, out int retries) || retries < 1)
            {
                MessageBox.Show("Кол-во попыток должно быть >= 1.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(UnstableBox.Text, out int unstable) || unstable < 1)
            {
                MessageBox.Show("Порог должен быть >= 1 мс.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            _settings.DarkTheme = DarkThemeToggle.IsChecked == true;

            if (int.TryParse(KeepDaysBox.Text, out int keepDays) && keepDays >= 1)
                _settings.HistoryKeepDays = keepDays;

            DialogResult = true;
            Close();
        }

        private void DarkThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            theme.SetBaseTheme(BaseTheme.Dark);
            helper.SetTheme(theme);
        }

        private void DarkThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            var helper = new PaletteHelper();
            var theme = helper.GetTheme();
            theme.SetBaseTheme(BaseTheme.Light);
            helper.SetTheme(theme);
        }

        private void TestEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var client = new SmtpClient(SmtpHostBox.Text?.Trim(), int.Parse(SmtpPortBox.Text)))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(SmtpUserBox.Text?.Trim(), SmtpPasswordBox.Password);
                    client.Timeout = 10000;
                    var msg = new MailMessage(EmailFromBox.Text?.Trim(), EmailToBox.Text?.Trim(),
                        "[NetworkMonitor] Тестовое сообщение", "Если вы видите это письмо — email-уведомления работают.");
                    client.Send(msg);
                    MessageBox.Show("Тестовое письмо отправлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Удалить всю историю?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _database?.Cleanup(0);
                MessageBox.Show("История очищена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show("Конфигурация экспортирована.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
```

**Step 3: Build and commit**

```bash
git add Views/SettingsDialog.xaml Views/SettingsDialog.xaml.cs
git commit -m "feat: update SettingsDialog with tabs (notifications, email, data)"
```

---

## Task 16: StatisticsView + EventLogView

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Views\StatisticsView.xaml`
- Create: `D:\Ягор\NetworkMonitor\Views\StatisticsView.xaml.cs`
- Create: `D:\Ягор\NetworkMonitor\Views\EventLogView.xaml`
- Create: `D:\Ягор\NetworkMonitor\Views\EventLogView.xaml.cs`

**Step 1: Create StatisticsView.xaml**

```xml
<Window x:Class="NetworkMonitor.Views.StatisticsView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:oxy="http://oxyplot.org/wpf"
        Title="Статистика" Width="900" Height="700"
        WindowStartupLocation="CenterOwner"
        Style="{StaticResource MaterialDesignWindow}">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header + Period selector -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,12">
            <TextBlock Text="Dashboard" FontSize="22" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,20,0"/>
            <TextBlock Text="Период:" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <ComboBox x:Name="PeriodBox" ItemsSource="{Binding Periods}" SelectedItem="{Binding SelectedPeriod}"
                      Style="{StaticResource MaterialDesignOutlinedComboBox}" Width="100"/>
        </StackPanel>

        <!-- Status tiles -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,0,0,12">
            <Border Background="#4CAF50" CornerRadius="8" Padding="20,12" Margin="0,0,8,0">
                <StackPanel>
                    <TextBlock Text="Online" Foreground="White" FontSize="13"/>
                    <TextBlock Text="{Binding OnlineCount}" Foreground="White" FontSize="28" FontWeight="Bold"/>
                </StackPanel>
            </Border>
            <Border Background="#F44336" CornerRadius="8" Padding="20,12" Margin="0,0,8,0">
                <StackPanel>
                    <TextBlock Text="Offline" Foreground="White" FontSize="13"/>
                    <TextBlock Text="{Binding OfflineCount}" Foreground="White" FontSize="28" FontWeight="Bold"/>
                </StackPanel>
            </Border>
            <Border Background="#FFC107" CornerRadius="8" Padding="20,12">
                <StackPanel>
                    <TextBlock Text="Unstable" FontSize="13"/>
                    <TextBlock Text="{Binding UnstableCount}" FontSize="28" FontWeight="Bold"/>
                </StackPanel>
            </Border>
        </StackPanel>

        <!-- Latency chart -->
        <oxy:PlotView Grid.Row="2" Model="{Binding LatencyPlot}" Margin="0,0,0,8"/>

        <!-- Packet loss chart -->
        <oxy:PlotView Grid.Row="3" Model="{Binding PacketLossPlot}" Margin="0,0,0,8"/>

        <!-- Uptime table -->
        <DataGrid Grid.Row="4" ItemsSource="{Binding UptimeRows}" AutoGenerateColumns="False"
                  IsReadOnly="True" MaxHeight="200"
                  Style="{StaticResource MaterialDesignDataGrid}">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Узел" Binding="{Binding NodeName}" Width="*"/>
                <DataGridTextColumn Header="24ч" Binding="{Binding Uptime24h}" Width="80"/>
                <DataGridTextColumn Header="7д" Binding="{Binding Uptime7d}" Width="80"/>
                <DataGridTextColumn Header="30д" Binding="{Binding Uptime30d}" Width="80"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</Window>
```

**Step 2: Create StatisticsView.xaml.cs**

```csharp
using System.Windows;
using NetworkMonitor.ViewModels;

namespace NetworkMonitor.Views
{
    public partial class StatisticsView : Window
    {
        public StatisticsView(StatisticsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
```

**Step 3: Create EventLogView.xaml**

```xml
<Window x:Class="NetworkMonitor.Views.EventLogView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="Журнал событий" Width="850" Height="550"
        WindowStartupLocation="CenterOwner"
        Style="{StaticResource MaterialDesignWindow}">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Filters -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,12">
            <TextBlock Text="Узел:" VerticalAlignment="Center" Margin="0,0,6,0"/>
            <ComboBox ItemsSource="{Binding NodeNames}" SelectedItem="{Binding SelectedNodeName}"
                      Style="{StaticResource MaterialDesignOutlinedComboBox}" Width="150" Margin="0,0,12,0"/>

            <TextBlock Text="Тип:" VerticalAlignment="Center" Margin="0,0,6,0"/>
            <ComboBox ItemsSource="{Binding EventTypes}" SelectedItem="{Binding SelectedEventType}"
                      Style="{StaticResource MaterialDesignOutlinedComboBox}" Width="150" Margin="0,0,12,0"/>

            <TextBlock Text="С:" VerticalAlignment="Center" Margin="0,0,6,0"/>
            <DatePicker SelectedDate="{Binding DateFrom}" Width="130" Margin="0,0,12,0"/>

            <TextBlock Text="По:" VerticalAlignment="Center" Margin="0,0,6,0"/>
            <DatePicker SelectedDate="{Binding DateTo}" Width="130"/>
        </StackPanel>

        <!-- Events table -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding Events}" AutoGenerateColumns="False"
                  IsReadOnly="True" Style="{StaticResource MaterialDesignDataGrid}">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Время" Binding="{Binding Timestamp, StringFormat=yyyy-MM-dd HH:mm:ss}" Width="150"/>
                <DataGridTextColumn Header="Узел" Binding="{Binding NodeId}" Width="120"/>
                <DataGridTextColumn Header="Тип" Binding="{Binding EventType}" Width="120"/>
                <DataGridTextColumn Header="Сообщение" Binding="{Binding Message}" Width="*"/>
                <DataGridTextColumn Header="Было" Binding="{Binding OldStatus}" Width="80"/>
                <DataGridTextColumn Header="Стало" Binding="{Binding NewStatus}" Width="80"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</Window>
```

**Step 4: Create EventLogView.xaml.cs**

```csharp
using System.Windows;
using NetworkMonitor.ViewModels;

namespace NetworkMonitor.Views
{
    public partial class EventLogView : Window
    {
        public EventLogView(EventLogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
```

**Step 5: Add to .csproj**

```xml
<Page Include="Views\StatisticsView.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
<Compile Include="Views\StatisticsView.xaml.cs">
  <DependentUpon>StatisticsView.xaml</DependentUpon>
</Compile>
<Page Include="Views\EventLogView.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
<Compile Include="Views\EventLogView.xaml.cs">
  <DependentUpon>EventLogView.xaml</DependentUpon>
</Compile>
```

**Step 6: Build and commit**

```bash
git add Views/StatisticsView.xaml Views/StatisticsView.xaml.cs Views/EventLogView.xaml Views/EventLogView.xaml.cs NetworkMonitor.csproj
git commit -m "feat: add StatisticsView and EventLogView"
```

---

## Task 17: Update MainWindow (toolbar, lines, mini-map, toast)

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\Views\MainWindow.xaml`
- Modify: `D:\Ягор\NetworkMonitor\Views\MainWindow.xaml.cs`

**Step 1: Replace MainWindow.xaml**

```xml
<Window x:Class="NetworkMonitor.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
        xmlns:gmap="clr-namespace:GMap.NET.WindowsPresentation;assembly=GMap.NET.WindowsPresentation"
        xmlns:views="clr-namespace:NetworkMonitor.Views"
        xmlns:vm="clr-namespace:NetworkMonitor.ViewModels"
        Title="NetworkMonitor" Width="1100" Height="700"
        MinWidth="900" MinHeight="600"
        Style="{StaticResource MaterialDesignWindow}"
        Closing="Window_Closing">
    <Window.Resources>
        <vm:StatusToColorConverter x:Key="StatusToColor"/>
        <vm:ToastTypeToColorConverter x:Key="ToastTypeToColor"/>
        <vm:NullToVisibilityConverter x:Key="NullToVis"/>
        <vm:InverseBoolToVisibilityConverter x:Key="InverseBoolToVis"/>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="56"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="30"/>
        </Grid.RowDefinitions>

        <!-- Toolbar -->
        <Border Grid.Row="0" Background="{DynamicResource MaterialDesign.Brush.Primary}" Padding="12,0">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <md:PackIcon Kind="LanConnect" Width="24" Height="24" Foreground="White" Margin="0,0,12,0"/>
                <TextBlock Text="NetworkMonitor" FontSize="18" FontWeight="Bold" Foreground="White" VerticalAlignment="Center" Margin="0,0,24,0"/>

                <Button Content="{Binding MonitoringLabel}" Command="{Binding ToggleMonitoringCommand}"
                        Style="{StaticResource MaterialDesignFlatLightBgButton}" Foreground="White" Margin="0,0,8,0"/>
                <Button Content="Сохранить" Command="{Binding SaveCommand}"
                        Style="{StaticResource MaterialDesignFlatLightBgButton}" Foreground="White" Margin="0,0,8,0"/>
                <Button Content="+ Узел" Click="AddNodeButton_Click"
                        Style="{StaticResource MaterialDesignFlatLightBgButton}" Foreground="White" Margin="0,0,8,0"/>
                <Button Click="SettingsButton_Click"
                        Style="{StaticResource MaterialDesignFlatLightBgButton}" Foreground="White" Margin="0,0,8,0">
                    <md:PackIcon Kind="Cog"/>
                </Button>
                <Button Click="StatisticsButton_Click"
                        Style="{StaticResource MaterialDesignFlatLightBgButton}" Foreground="White" Margin="0,0,8,0">
                    <md:PackIcon Kind="ChartLine"/>
                </Button>
                <Button Click="EventLogButton_Click"
                        Style="{StaticResource MaterialDesignFlatLightBgButton}" Foreground="White">
                    <md:PackIcon Kind="FormatListBulleted"/>
                </Button>
            </StackPanel>
        </Border>

        <!-- Main content -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="300"/>
            </Grid.ColumnDefinitions>

            <!-- Map area -->
            <Grid Grid.Column="0">
                <gmap:GMapControl x:Name="MainMap" Loaded="MainMap_Loaded"
                                  Zoom="10" MinZoom="2" MaxZoom="18"
                                  MouseWheelZoomType="MousePositionAndCenter"
                                  DragButton="Left"/>

                <!-- Mini-map -->
                <Border HorizontalAlignment="Left" VerticalAlignment="Bottom"
                        Margin="12" Width="200" Height="150"
                        BorderBrush="Gray" BorderThickness="2" CornerRadius="4"
                        Background="White" Opacity="0.9">
                    <gmap:GMapControl x:Name="MiniMap"
                                      Zoom="6" MinZoom="1" MaxZoom="18"
                                      CanDragMap="False" MouseWheelZoomEnabled="False"/>
                </Border>

                <!-- Empty state -->
                <TextBlock Text="Нет данных. Добавьте узлы для начала мониторинга."
                           HorizontalAlignment="Center" VerticalAlignment="Center"
                           FontSize="16" Foreground="Gray"
                           Visibility="{Binding HasNodes, Converter={StaticResource InverseBoolToVis}}"/>
            </Grid>

            <!-- Right panel -->
            <Grid Grid.Column="1">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>

                <!-- Search/filter -->
                <TextBox x:Name="SearchBox" Grid.Row="0"
                         md:HintAssist.Hint="Поиск по имени/IP"
                         Style="{StaticResource MaterialDesignOutlinedTextBox}"
                         Margin="8,8,8,4" TextChanged="SearchBox_TextChanged"/>

                <!-- Node list -->
                <ListView x:Name="NodeListView" Grid.Row="1"
                          ItemsSource="{Binding Nodes}"
                          SelectedItem="{Binding SelectedNode}"
                          Margin="8,0" SelectionChanged="NodeListView_SelectionChanged">
                    <ListView.ItemTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" Margin="4">
                                <Ellipse Width="10" Height="10"
                                         Fill="{Binding Status, Converter={StaticResource StatusToColor}}"
                                         Margin="0,0,8,0" VerticalAlignment="Center"/>
                                <StackPanel>
                                    <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                                    <TextBlock FontSize="11" Foreground="Gray">
                                        <Run Text="{Binding IpAddress, Mode=OneWay}"/>
                                        <Run Text=" | "/>
                                        <Run Text="{Binding LastPingMs, StringFormat={}{0}ms, Mode=OneWay}"/>
                                    </TextBlock>
                                </StackPanel>
                            </StackPanel>
                        </DataTemplate>
                    </ListView.ItemTemplate>
                </ListView>

                <!-- Node detail -->
                <views:NodeDetailView x:Name="DetailView" Grid.Row="2"
                                      MaxHeight="320"
                                      DataContext="{Binding SelectedNode}"
                                      Visibility="{Binding DataContext.SelectedNode, RelativeSource={RelativeSource AncestorType=Window}, Converter={StaticResource NullToVis}}"/>
            </Grid>
        </Grid>

        <!-- Toast notifications overlay -->
        <ItemsControl Grid.Row="1" ItemsSource="{Binding Toasts}"
                      HorizontalAlignment="Right" VerticalAlignment="Bottom"
                      Margin="0,0,316,12" IsHitTestVisible="True">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Border Background="{Binding Type, Converter={StaticResource ToastTypeToColor}}"
                            CornerRadius="6" Padding="12,8" Margin="0,4" MinWidth="280" MaxWidth="350"
                            Opacity="0.95" MouseDown="Toast_MouseDown" Cursor="Hand">
                        <StackPanel>
                            <TextBlock Text="{Binding Title}" FontWeight="Bold" Foreground="White" FontSize="12"/>
                            <TextBlock Text="{Binding Message}" Foreground="White" FontSize="12" TextWrapping="Wrap"/>
                        </StackPanel>
                    </Border>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <!-- Status bar -->
        <Border Grid.Row="2" Background="{DynamicResource MaterialDesign.Brush.Primary.Dark}" Padding="12,0">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Foreground="White" FontSize="12" Margin="0,0,16,0">
                    <Run Text="Online: "/><Run Text="{Binding OnlineCount, Mode=OneWay}"/>
                </TextBlock>
                <TextBlock Foreground="White" FontSize="12" Margin="0,0,16,0">
                    <Run Text="Offline: "/><Run Text="{Binding OfflineCount, Mode=OneWay}"/>
                </TextBlock>
                <TextBlock Foreground="White" FontSize="12" Margin="0,0,16,0">
                    <Run Text="Unstable: "/><Run Text="{Binding UnstableCount, Mode=OneWay}"/>
                </TextBlock>
                <TextBlock Foreground="White" FontSize="12">
                    <Run Text="Всего: "/><Run Text="{Binding Nodes.Count, Mode=OneWay}"/>
                </TextBlock>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

**Step 2: Replace MainWindow.xaml.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using NetworkMonitor.Models;
using NetworkMonitor.Services;
using NetworkMonitor.ViewModels;

namespace NetworkMonitor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm;
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _toastTimer;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            // Set up detail view
            DetailView.SetDatabase(_vm.Database);
            DetailView.DeleteRequested += (s, node) =>
            {
                _vm.Nodes.Remove(node);
                _vm.SelectedNode = null;
                _vm.UpdateSchedulerNodes();
                RefreshMarkers();
            };

            // Periodic marker + mini-map refresh
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _refreshTimer.Tick += (s, e) =>
            {
                RefreshMarkers();
                _vm.UpdateCounters();
                if (_vm.SelectedNode != null)
                    DetailView.RefreshChart(_vm.SelectedNode);
            };
            _refreshTimer.Start();

            // Toast auto-remove timer
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _toastTimer.Tick += (s, e) =>
            {
                var expired = _vm.Notifications.Toasts
                    .Where(t => (DateTime.Now - t.CreatedAt).TotalSeconds > 5)
                    .ToList();
                foreach (var t in expired)
                    _vm.Notifications.Toasts.Remove(t);
            };
            _toastTimer.Start();
        }

        private void MainMap_Loaded(object sender, RoutedEventArgs e)
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            MainMap.MapProvider = OpenStreetMapProvider.Instance;
            MainMap.Position = new PointLatLng(_vm.Settings.MapLat, _vm.Settings.MapLon);
            MainMap.Zoom = _vm.Settings.MapZoom;
            MainMap.OnPositionChanged += MainMap_OnPositionChanged;
            MainMap.OnMapZoomChanged += MainMap_OnMapZoomChanged;

            // Mini-map setup
            MiniMap.MapProvider = OpenStreetMapProvider.Instance;
            MiniMap.Position = MainMap.Position;
            MiniMap.Zoom = Math.Max(1, MainMap.Zoom - 4);

            RefreshMarkers();
        }

        private void MainMap_OnPositionChanged(PointLatLng point)
        {
            MiniMap.Position = point;
        }

        private void MainMap_OnMapZoomChanged()
        {
            MiniMap.Zoom = Math.Max(1, MainMap.Zoom - 4);
        }

        private void RefreshMarkers()
        {
            MainMap.Markers.Clear();

            // Draw links first (lines between nodes)
            foreach (var link in _vm.Links)
            {
                var source = _vm.Nodes.FirstOrDefault(n => n.Id == link.SourceNodeId);
                var target = _vm.Nodes.FirstOrDefault(n => n.Id == link.TargetNodeId);
                if (source == null || target == null) continue;

                var route = new GMapRoute(new List<PointLatLng>
                {
                    new PointLatLng(source.Latitude, source.Longitude),
                    new PointLatLng(target.Latitude, target.Longitude)
                });

                // Color by worst status
                var worstStatus = (NodeStatus)Math.Max((int)source.Status, (int)target.Status);
                Color lineColor;
                switch (worstStatus)
                {
                    case NodeStatus.Online: lineColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                    case NodeStatus.Unstable: lineColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                    case NodeStatus.Offline: lineColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                    default: lineColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                }

                route.Shape = new Line
                {
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2,
                    Opacity = 0.7
                };

                MainMap.Markers.Add(route);
            }

            // Draw node markers
            foreach (var node in _vm.Nodes)
            {
                Color markerColor;
                switch (node.Status)
                {
                    case NodeStatus.Online: markerColor = Color.FromRgb(0x4C, 0xAF, 0x50); break;
                    case NodeStatus.Unstable: markerColor = Color.FromRgb(0xFF, 0xC1, 0x07); break;
                    case NodeStatus.Offline: markerColor = Color.FromRgb(0xF4, 0x43, 0x36); break;
                    default: markerColor = Color.FromRgb(0x9E, 0x9E, 0x9E); break;
                }

                var iconKind = GetIconKind(node.DeviceType);

                var grid = new Grid { Width = 36, Height = 36 };
                grid.Children.Add(new Ellipse
                {
                    Fill = new SolidColorBrush(markerColor),
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Width = 36,
                    Height = 36
                });
                grid.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = iconKind,
                    Width = 18,
                    Height = 18,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });

                grid.ToolTip = $"{node.Name}\n{node.IpAddress}\n{node.Status} | {node.LastPingMs}ms";

                var capturedNode = node;
                grid.MouseLeftButtonDown += (s, e) =>
                {
                    _vm.SelectedNode = capturedNode;
                    NodeListView.SelectedItem = capturedNode;
                };

                var marker = new GMapMarker(new PointLatLng(node.Latitude, node.Longitude))
                {
                    Shape = grid,
                    Offset = new Point(-18, -18)
                };
                MainMap.Markers.Add(marker);
            }
        }

        private MaterialDesignThemes.Wpf.PackIconKind GetIconKind(string deviceType)
        {
            switch (deviceType?.ToLower())
            {
                case "router": return MaterialDesignThemes.Wpf.PackIconKind.Router;
                case "server": return MaterialDesignThemes.Wpf.PackIconKind.Server;
                case "switch": return MaterialDesignThemes.Wpf.PackIconKind.LanConnect;
                case "camera": return MaterialDesignThemes.Wpf.PackIconKind.Camera;
                case "pc": return MaterialDesignThemes.Wpf.PackIconKind.Monitor;
                default: return MaterialDesignThemes.Wpf.PackIconKind.Devices;
            }
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddNodeDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultNode != null)
            {
                _vm.Nodes.Add(dialog.ResultNode);
                _vm.UpdateSchedulerNodes();
                RefreshMarkers();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(_vm.Settings, _vm.Database) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _vm.UpdateSettings();
                _vm.Save();
            }
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsVm = new StatisticsViewModel(_vm.Database, _vm.Nodes);
            var window = new StatisticsView(statsVm) { Owner = this };
            window.Show();
        }

        private void EventLogButton_Click(object sender, RoutedEventArgs e)
        {
            var logVm = new EventLogViewModel(_vm.Database, _vm.Nodes);
            var window = new EventLogView(logVm) { Owner = this };
            window.Show();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filter = SearchBox.Text?.Trim().ToLower();
            if (string.IsNullOrEmpty(filter))
            {
                NodeListView.ItemsSource = _vm.Nodes;
                return;
            }
            NodeListView.ItemsSource = _vm.Nodes
                .Where(n => (n.Name?.ToLower().Contains(filter) ?? false) ||
                            (n.IpAddress?.ToLower().Contains(filter) ?? false))
                .ToList();
        }

        private void NodeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.SelectedNode != null)
                DetailView.RefreshChart(_vm.SelectedNode);
        }

        private void Toast_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToastNotification toast)
                _vm.Notifications.RemoveToast(toast);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _refreshTimer.Stop();
            _toastTimer.Stop();
            _vm.Save();
            _vm.Shutdown();
        }
    }
}
```

**Step 3: Build and commit**

```bash
git add Views/MainWindow.xaml Views/MainWindow.xaml.cs
git commit -m "feat: update MainWindow with links, mini-map, toast, toolbar buttons"
```

---

## Task 18: App.xaml.cs (startup wiring + dark theme)

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\App.xaml.cs`
- Modify: `D:\Ягор\NetworkMonitor\App.xaml`

**Step 1: Update App.xaml.cs**

```csharp
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

            // Apply saved theme
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
```

**Step 2: Update App.xaml — add OxyPlot namespace (if needed for resource references)**

Ensure the converters are registered. Add `xmlns:vm="clr-namespace:NetworkMonitor.ViewModels"` to the Application tag if not present, and register all converters:

```xml
<Application x:Class="NetworkMonitor.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:vm="clr-namespace:NetworkMonitor.ViewModels"
             StartupUri="Views/MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <md:BundledTheme BaseTheme="Light" PrimaryColor="BlueGrey" SecondaryColor="Cyan"/>
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml"/>
            </ResourceDictionary.MergedDictionaries>

            <vm:NullToVisibilityConverter x:Key="NullToVisibilityConverter"/>
            <vm:BoolToStringConverter x:Key="BoolToStringConverter"/>
            <vm:StatusToColorConverter x:Key="StatusToColorConverter"/>
            <vm:ToastTypeToColorConverter x:Key="ToastTypeToColorConverter"/>
            <vm:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibilityConverter"/>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**Step 3: Build and commit**

```bash
git add App.xaml App.xaml.cs
git commit -m "feat: add startup theme apply and NLog init in App.xaml.cs"
```

---

## Task 19: Sound Asset + Theme.xaml

**Files:**
- Create: `D:\Ягор\NetworkMonitor\Assets\Sounds\alert.wav`
- Create: `D:\Ягор\NetworkMonitor\Themes\Theme.xaml`

**Step 1: Create alert.wav placeholder**

Generate a minimal WAV file programmatically or copy a system sound. For now, use a Windows system sound as placeholder:

```bash
cp "C:/Windows/Media/Windows Notify System Generic.wav" "D:/Ягор/NetworkMonitor/Assets/Sounds/alert.wav"
```

If that file doesn't exist, use any available `.wav` from `C:/Windows/Media/`.

**Step 2: Create Theme.xaml**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Custom DataGrid row style for compact display -->
    <Style x:Key="CompactDataGridRow" TargetType="DataGridRow" BasedOn="{StaticResource MaterialDesignDataGridRow}">
        <Setter Property="MinHeight" Value="28"/>
    </Style>

    <!-- Toast animation style -->
    <Style x:Key="ToastBorder" TargetType="Border">
        <Style.Triggers>
            <EventTrigger RoutedEvent="Loaded">
                <BeginStoryboard>
                    <Storyboard>
                        <DoubleAnimation Storyboard.TargetProperty="Opacity"
                                         From="0" To="0.95" Duration="0:0:0.3"/>
                        <ThicknessAnimation Storyboard.TargetProperty="Margin"
                                            From="0,20,0,0" To="0,4,0,0" Duration="0:0:0.3"/>
                    </Storyboard>
                </BeginStoryboard>
            </EventTrigger>
        </Style.Triggers>
    </Style>
</ResourceDictionary>
```

**Step 3: Add to .csproj**

```xml
<Content Include="Assets\Sounds\alert.wav">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
<Page Include="Themes\Theme.xaml">
  <SubType>Designer</SubType>
  <Generator>MSBuild:Compile</Generator>
</Page>
```

**Step 4: Build and commit**

```bash
git add Assets/Sounds/alert.wav Themes/Theme.xaml NetworkMonitor.csproj
git commit -m "feat: add alert sound and Theme.xaml with toast animation"
```

---

## Task 20: Final .csproj Cleanup + Build

**Files:**
- Modify: `D:\Ягор\NetworkMonitor\NetworkMonitor.csproj`

**Step 1: Verify all new files are in .csproj**

Ensure all `<Compile>` and `<Page>` entries from previous tasks are present. Remove the old `PingService.cs` entry if still there. Remove unused Mapsui/SkiaSharp references if desired (optional — they don't hurt but add bloat).

**Step 2: Full build**

Build the solution. Fix any compilation errors.

Common issues to watch for:
- Missing `using` statements
- Namespace mismatches
- Removed `SoundAlerts` property references
- GMapRoute constructor differences (may need to verify GMap.NET API)

**Step 3: Run the application**

Launch and verify:
- [ ] App starts without crash
- [ ] Map loads with OSM tiles
- [ ] Mini-map visible in bottom-left corner
- [ ] Can add a node via dialog (with module config)
- [ ] Start monitoring — ping starts, status colors update
- [ ] Status bar shows Online/Offline/Unstable counts
- [ ] Click node → NodeDetailView shows with latency chart
- [ ] Settings dialog opens with 5 tabs
- [ ] Statistics window opens with graphs
- [ ] Event log window opens
- [ ] Toast appears when node goes offline
- [ ] Dark theme toggle works

**Step 4: Final commit**

```bash
git add -A
git commit -m "feat: NetworkMonitor v2.0 — modular monitoring, SQLite, charts, notifications"
```
