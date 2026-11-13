using SmartX.API.Part_2;


namespace SmartX.API.Part3;

/// <summary>Timestamp plus a sequence number, so two readings in the same tick never overwrite each other.</summary>
public readonly record struct HistoryKey(DateTime Time, long Seq) : IComparable<HistoryKey>
{
    public int CompareTo(HistoryKey other)
    {
        int c = Time.CompareTo(other.Time);
        return c != 0 ? c : Seq.CompareTo(other.Seq);
    }
}

public record TelemetryPoint(string Mac, string Metric, double Value, DateTime Time);
public record HistoryResult(string Mac, int TreeSize, int TreeHeight, int NodesVisited, int Matched, List<TelemetryPoint> Points);
public record HistorySummary(string Mac, int Readings, int TreeHeight);

/// <summary>One AVL tree per device, keyed by timestamp. Range queries never scan the whole history.</summary>
public class TelemetryHistory
{
    const int MaxPerNode = 2000;     // history is capped: the oldest reading is dropped first

    readonly Dictionary<string, AvlTree<HistoryKey, TelemetryPoint>> _trees = new(StringComparer.OrdinalIgnoreCase);
    readonly object _lock = new();
    long _seq;

    static DateTime Utc(DateTime t) =>
        t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);

    public void Add(StreamPacket p)
    {
        lock (_lock)
        {
            if (!_trees.TryGetValue(p.Mac, out var tree)) _trees[p.Mac] = tree = new AvlTree<HistoryKey, TelemetryPoint>();
            var time = Utc(p.Timestamp);
            tree.Insert(new HistoryKey(time, ++_seq), new TelemetryPoint(p.Mac, p.Metric, p.Value, time));
            while (tree.Count > MaxPerNode) tree.RemoveMin(out _);
        }
    }

    /// <summary>e.g. all moisture readings for one node between two times.</summary>
    public HistoryResult Query(string mac, DateTime from, DateTime to, string? metric, int take)
    {
        lock (_lock)
        {
            if (!_trees.TryGetValue(mac, out var tree))
                return new HistoryResult(mac, 0, 0, 0, 0, new List<TelemetryPoint>());

            var raw = tree.Range(new HistoryKey(from, long.MinValue), new HistoryKey(to, long.MaxValue), out int visited);
            IEnumerable<TelemetryPoint> points = raw.Select(kv => kv.Value);
            if (!string.IsNullOrWhiteSpace(metric) && metric != "all")
                points = points.Where(p => p.Metric.Equals(metric, StringComparison.OrdinalIgnoreCase));

            var list = points.ToList();
            int matched = list.Count;
            if (list.Count > take) list = list.GetRange(list.Count - take, take);
            return new HistoryResult(mac, tree.Count, tree.Height, visited, matched, list);
        }
    }

    public HistoryResult Latest(string mac, int take)
    {
        lock (_lock)
        {
            if (!_trees.TryGetValue(mac, out var tree))
                return new HistoryResult(mac, 0, 0, 0, 0, new List<TelemetryPoint>());
            var list = tree.Latest(take);
            return new HistoryResult(mac, tree.Count, tree.Height, 0, list.Count, list);
        }
    }

    public List<HistorySummary> Summary()
    {
        lock (_lock)
            return _trees.Select(kv => new HistorySummary(kv.Key, kv.Value.Count, kv.Value.Height))
                         .OrderBy(s => s.Mac).ToList();
    }
}