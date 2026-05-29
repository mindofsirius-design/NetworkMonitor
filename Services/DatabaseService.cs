using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
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

        private readonly List<MonitoringResult> _resultBuffer = new List<MonitoringResult>();
        private readonly List<NodeEvent> _eventBuffer = new List<NodeEvent>();
        private readonly object _bufferLock = new object();
        private readonly object _writeLock = new object();
        private System.Threading.Timer _flushTimer;

        public DatabaseService(string dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetworkMonitor", "data", "history.db");
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
            Flush();
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

            lock (_writeLock)
            {
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
                            else
                            {
                                ExecuteNonQuery(
                                    "INSERT INTO module_results (node_id, module_name, success, details, timestamp) VALUES (@nid, @mod, @suc, @det, @ts)",
                                    new SQLiteParameter("@nid", r.NodeId),
                                    new SQLiteParameter("@mod", r.ModuleName),
                                    new SQLiteParameter("@suc", r.Success ? 1 : 0),
                                    new SQLiteParameter("@det", r.Details),
                                    new SQLiteParameter("@ts", r.Timestamp.ToString("o")));
                            }
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
                            Timestamp = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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
                            Timestamp = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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
