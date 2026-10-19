namespace SmartX.API.Part_2;


/// <summary>
/// Queue&lt;T&gt;      : standard FIFO lane for ordinary telemetry.
/// PriorityQueue : critical lane (power spike, moisture crash). Always drained first.
/// </summary>
public class IngestionPipeline(DeviceRegistry registry, PredictionEngine prediction)
{
    readonly Queue<StreamPacket> _standard = new();
    readonly PriorityQueue<StreamPacket, (int Severity, long Seq)> _critical = new();
    readonly LinkedList<ProcessedEntry> _log = new();
    readonly object _lock = new();
    long _seq;

    public volatile bool Paused;                 // demo aid: stack packets up, then release them
    public long Processed { get; private set; }
    public long CriticalProcessed { get; private set; }

    public (string Lane, string? Reason) Enqueue(StreamPacket p)
    {
        p = p with { Mac = p.Mac.Trim().ToUpperInvariant() };
        lock (_lock)
        {
            _seq++;
            if (AlertRules.IsCritical(p, out var reason))
            {
                _critical.Enqueue(p, (AlertRules.Severity(p), _seq));   // ties resolved FIFO by sequence
                return ("critical", reason);
            }
            _standard.Enqueue(p);
            return ("standard", null);
        }
    }

    public int ProcessPending(int max = 100)
    {
        if (Paused) return 0;
        int n = 0;
        while (n < max)
        {
            StreamPacket p; string lane;
            lock (_lock)
            {
                if (_critical.TryDequeue(out var c, out _)) { p = c; lane = "CRITICAL"; }
                else if (_standard.TryDequeue(out var s)) { p = s; lane = "STANDARD"; }
                else break;
            }
            Handle(p, lane);
            n++;
        }
        return n;
    }

    void Handle(StreamPacket p, string lane)
    {
        registry.Apply(p);
        if (AlertRules.IsCritical(p, out var reason))
        {
            if (registry.RaiseAlert(p.Mac, p.Metric, reason))      // false => duplicate suppressed by HashSet
                prediction.RecordEvent($"ALERT:{p.Mac}");
        }
        else registry.ClearAlert(p.Mac, p.Metric);

        lock (_lock)
        {
            Processed++;
            if (lane == "CRITICAL") CriticalProcessed++;
            _log.AddFirst(new ProcessedEntry(Processed, lane, p.Mac, p.Metric, p.Value, DateTime.UtcNow));
            while (_log.Count > 30) _log.RemoveLast();
        }
    }

    public object Snapshot()
    {
        lock (_lock)
            return new
            {
                standardDepth = _standard.Count,
                criticalDepth = _critical.Count,
                processed = Processed,
                criticalProcessed = CriticalProcessed,
                duplicatesSuppressed = registry.DuplicatesSuppressed,
                paused = Paused,
                recent = _log.ToArray()
            };
    }
}