using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NetworkMonitor.Models;

namespace NetworkMonitor.Services
{
    public class TracerouteGroupingService
    {
        private readonly ConcurrentDictionary<string, List<string>> _nodeHops =
            new ConcurrentDictionary<string, List<string>>();

        // Сколько раз подряд узел отсутствовал в совпадениях
        private readonly Dictionary<string, int> _missCounts = new Dictionary<string, int>();

        public ObservableCollection<RouteGroup> Groups { get; } =
            new ObservableCollection<RouteGroup>();

        public event Action GroupsChanged;

        private const int MinSharedHops = 2;
        private const int MaxMissesBeforeRemove = 3;

        // ── публичный API ──────────────────────────────────────────────────────

        public void UpdateNodeHops(string nodeId, List<string> hops)
        {
            if (hops != null && hops.Count > 0)
                _nodeHops[nodeId] = hops;

            Application.Current?.Dispatcher.Invoke(RebuildGroups);
        }

        public void RemoveNode(string nodeId)
        {
            _nodeHops.TryRemove(nodeId, out _);
            _missCounts.Remove(nodeId);
            Application.Current?.Dispatcher.Invoke(RebuildGroups);
        }

        /// <summary>True если узел не последний в группе → его traceroute можно пропустить.</summary>
        public bool IsNonTerminalGroupMember(string nodeId)
        {
            foreach (var g in Groups)
            {
                var idx = g.OrderedNodeIds.IndexOf(nodeId);
                if (idx >= 0 && idx < g.OrderedNodeIds.Count - 1)
                    return true;
            }
            return false;
        }

        // ── приватная логика ───────────────────────────────────────────────────

        private void RebuildGroups()
        {
            var nodeIds = _nodeHops.Keys.ToList();
            var candidates = BuildCandidateGroups(nodeIds);

            var candidateNodes = candidates
                .SelectMany(g => g.OrderedNodeIds).ToHashSet();

            // Обновляем счётчики пропусков
            var currentNodes = Groups.SelectMany(g => g.OrderedNodeIds).Distinct().ToList();
            foreach (var id in currentNodes)
            {
                if (!candidateNodes.Contains(id))
                    _missCounts[id] = (_missCounts.TryGetValue(id, out int m) ? m : 0) + 1;
                else
                    _missCounts.Remove(id);
            }

            // Стабилизация: узлы с miss < порога возвращаем в группу
            foreach (var oldGroup in Groups)
            {
                foreach (var nodeId in oldGroup.OrderedNodeIds)
                {
                    if (!_missCounts.TryGetValue(nodeId, out int misses) || misses >= MaxMissesBeforeRemove)
                        continue;

                    var target = candidates.FirstOrDefault(g =>
                        g.OrderedNodeIds.Any(id => oldGroup.OrderedNodeIds.Contains(id) && id != nodeId));

                    if (target != null && !target.OrderedNodeIds.Contains(nodeId))
                        InsertInOrder(target, nodeId);
                }
            }

            Groups.Clear();
            foreach (var g in candidates.Where(g => g.OrderedNodeIds.Count >= 2))
                Groups.Add(g);

            GroupsChanged?.Invoke();
        }

        private List<RouteGroup> BuildCandidateGroups(List<string> nodeIds)
        {
            var result = new List<RouteGroup>();
            var sorted = nodeIds.OrderBy(id => _nodeHops[id].Count).ToList();

            for (int i = 0; i < sorted.Count; i++)
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    var idA = sorted[i];
                    var idB = sorted[j];
                    var hopsA = _nodeHops[idA];
                    var hopsB = _nodeHops[idB];
                    var shared = LongestConsecutiveShared(hopsA, hopsB);

                    if (shared.Count < MinSharedHops) continue;

                    var group = result.FirstOrDefault(g =>
                        g.OrderedNodeIds.Contains(idA) || g.OrderedNodeIds.Contains(idB));

                    if (group == null)
                    {
                        group = new RouteGroup { SharedHops = shared };
                        // Меньше хопов → ближе к источнику → ставим первым
                        if (hopsA.Count <= hopsB.Count) { group.OrderedNodeIds.Add(idA); group.OrderedNodeIds.Add(idB); }
                        else { group.OrderedNodeIds.Add(idB); group.OrderedNodeIds.Add(idA); }
                        result.Add(group);
                    }
                    else
                    {
                        if (!group.OrderedNodeIds.Contains(idA)) InsertInOrder(group, idA);
                        if (!group.OrderedNodeIds.Contains(idB)) InsertInOrder(group, idB);
                    }
                }

            // Финальная сортировка по количеству хопов (меньше хопов = ближе к источнику)
            foreach (var g in result)
            {
                g.OrderedNodeIds = g.OrderedNodeIds
                    .OrderBy(id => _nodeHops.TryGetValue(id, out var h) ? h.Count : int.MaxValue)
                    .ToList();
            }

            return result;
        }

        private void InsertInOrder(RouteGroup group, string nodeId)
        {
            if (!_nodeHops.TryGetValue(nodeId, out var hops)) return;

            int insertAt = 0;
            foreach (var existId in group.OrderedNodeIds)
            {
                if (_nodeHops.TryGetValue(existId, out var existHops) && existHops.Count <= hops.Count)
                    insertAt++;
                else
                    break;
            }
            group.OrderedNodeIds.Insert(insertAt, nodeId);
        }

        private static List<string> LongestConsecutiveShared(List<string> a, List<string> b)
        {
            var best = new List<string>();
            for (int i = 0; i < a.Count; i++)
                for (int j = 0; j < b.Count; j++)
                {
                    if (a[i] != b[j]) continue;
                    var cur = new List<string>();
                    int ai = i, bj = j;
                    while (ai < a.Count && bj < b.Count && a[ai] == b[bj]) { cur.Add(a[ai]); ai++; bj++; }
                    if (cur.Count > best.Count) best = cur;
                }
            return best;
        }
    }
}