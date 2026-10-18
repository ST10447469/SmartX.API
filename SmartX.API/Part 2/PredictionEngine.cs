namespace SmartX.API.Part_2;


/// <summary>
/// Learns "event A is usually followed by event B" (a first-order transition table) from alerts,
/// searches and commands, then suggests what the operator is likely to do next. Rule-based checks
/// highlight problem devices immediately, even before any pattern has been learned.
///
/// Events: ALERT:{mac}  |  SEARCH:{term}  |  CMD:{mac}:{ACTION}
/// </summary>
public class PredictionEngine(DeviceRegistry registry)
{
    static readonly TimeSpan Window = TimeSpan.FromMinutes(2);   // max gap for two events to be "related"
    static readonly TimeSpan Stale = TimeSpan.FromMinutes(10);   // ignore a "last event" older than this

    readonly Dictionary<string, Dictionary<string, int>> _transitions = new();
    readonly object _lock = new();
    string? _last;
    DateTime _lastAt;

    public void RecordEvent(string ev)
    {
        lock (_lock)
        {
            if (_last is not null && ev != _last && DateTime.UtcNow - _lastAt <= Window)
            {
                if (!_transitions.TryGetValue(_last, out var next)) _transitions[_last] = next = new();
                next[ev] = next.GetValueOrDefault(ev) + 1;
            }
            _last = ev;
            _lastAt = DateTime.UtcNow;
        }
    }

    public List<Insight> Suggest()
    {
        var list = new List<Insight>();

        // 1) Rule-based: flag problem devices and nominate actuators in the same category.
        var problems = registry.Query(null, "alert", null);
        var actuators = registry.Query(null, "all", null).Where(d => d.DeviceType == "actuator").ToList();
        foreach (var d in problems)
        {
            list.Add(new("Automated insight", $"{d.Name} needs attention", string.Join("; ", d.Alerts), d.Mac, null, 1.0));
            if (d.DeviceType == "sensor" && d.Online)
                foreach (var a in actuators.Where(a => a.Category == d.Category && a.State != "ON"))
                    list.Add(new("Suggested action", $"Turn on {a.Name}",
                        $"Rule: {d.Name} is alerting and {a.Name} is in the same category ({d.Category}).", a.Mac, "ON", 0.5));
        }

        // 2) Learned: what usually follows the most recent event?
        string? last; Dictionary<string, int>? next = null;
        lock (_lock)
        {
            last = _last is not null && DateTime.UtcNow - _lastAt <= Stale ? _last : null;
            if (last is not null && _transitions.TryGetValue(last, out var n)) next = new(n);
        }
        if (last is not null && next is not null)
        {
            int total = next.Values.Sum();
            foreach (var (ev, count) in next.OrderByDescending(x => x.Value).Take(3))
            {
                double conf = (double)count / total;
                var detail = $"Learned: after '{Describe(last)}' this followed {count} of {total} time(s).";
                var parts = ev.Split(':', 3);
                switch (parts[0])
                {
                    case "CMD":
                        list.Add(new("Suggested action", $"{parts[2]} {registry.NameOf(parts[1])}", detail, parts[1], parts[2], conf)); break;
                    case "SEARCH":
                        list.Add(new("Suggested action", $"Search '{ev["SEARCH:".Length..]}'", detail, null, ev, conf)); break;
                    case "ALERT":
                        list.Add(new("Automated insight", $"Watch {registry.NameOf(parts[1])}", detail, parts[1], null, conf)); break;
                }
            }
        }
        return list.OrderByDescending(i => i.Confidence).Take(8).ToList();
    }

    string Describe(string ev)
    {
        var p = ev.Split(':', 3);
        return p[0] switch
        {
            "ALERT" => $"alert on {registry.NameOf(p[1])}",
            "CMD" => $"{p[2]} sent to {registry.NameOf(p[1])}",
            "SEARCH" => $"search '{ev["SEARCH:".Length..]}'",
            _ => ev
        };
    }
}