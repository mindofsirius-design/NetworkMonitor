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
        public TracerouteGroupingService GroupingService { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private volatile bool _isWarmingUp = false; // флаг первого запуска
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
            _isWarmingUp = true;    // флаг первого запуска
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

                            // Для не-терминальных членов группы пропускаем traceroute
                            if (module.Name == "traceroute" && GroupingService?.IsNonTerminalGroupMember(node.Id) == true)
                                continue;

                            int interval = GetInterval(node, module);
                            _nextRun[key] = now.AddSeconds(interval);

                            tasks.Add(RunModuleAsync(node, module, ct));
                        }
                    }

                    if (tasks.Count > 0)
                        await Task.WhenAll(tasks);

                    if (_isWarmingUp)
                        _isWarmingUp = false;

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
            try 
            {
                await _semaphore.WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                using (var timeoutCts = new CancellationTokenSource(TaskTimeoutMs))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token))
                {
                    try
                    {
                        var result = await module.CheckAsync(node, linkedCts.Token);
                        if (module.Name == "ping" && node.Status != result.Status && !_isWarmingUp)
                        {
                            var nodeEvent = new NodeEvent
                            {
                                NodeId = node.Name,
                                EventType = "Смена статуса",
                                Message = $"{node.Name}: {node.Status} → {result.Status}",
                                OldStatus = node.Status,
                                NewStatus = result.Status,
                                Timestamp = DateTime.UtcNow
                            };
                            EventBus.Instance.PublishEvent(nodeEvent);
                        }
                        EventBus.Instance.PublishResult(result);
                    }

                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        Logger.Warn("Module {0} timed out for {1}", module.Name, node.Name);
                        if (ct.IsCancellationRequested) return;  // ← не делать retry при остановке
                        await Task.Delay(2000, ct);
                        try
                        {
                            using (var retryCts = new CancellationTokenSource(TaskTimeoutMs))
                            using (var retryLinked = CancellationTokenSource.CreateLinkedTokenSource(ct, retryCts.Token))
                            {
                                var result = await module.CheckAsync(node, retryLinked.Token);
                                
                                if (module.Name == "ping" && node.Status != result.Status && !_isWarmingUp)
                                {
                                    var nodeEvent = new NodeEvent
                                    {
                                        NodeId = node.Name,
                                        EventType = "Смена статуса",
                                        Message = $"{node.Name}: {node.Status} → {result.Status}",
                                        OldStatus = node.Status,
                                        NewStatus = result.Status,
                                        Timestamp = DateTime.UtcNow
                                    };
                                    EventBus.Instance.PublishEvent(nodeEvent);
                                }
                                EventBus.Instance.PublishResult(result);
                            }
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
            catch (OperationCanceledException) { }
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
            return DefaultIntervalSec * 2;
        }

        public void ResetSchedule(string nodeId = null)
        {
            if (nodeId == null)
            {
                _nextRun.Clear();
            }
            else
            {
                foreach (var key in _nextRun.Keys.Where(k => k.StartsWith(nodeId + ":")).ToList())
                    _nextRun.TryRemove(key, out _);
            }
        }

    }
}
