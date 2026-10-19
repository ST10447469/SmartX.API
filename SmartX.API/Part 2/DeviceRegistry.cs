namespace SmartX.API.Part_2;


public class DeviceRegistry
{
    public static readonly TimeSpan OfflineAfter = TimeSpan.FromSeconds(20);
    const int MaxHistory = 500;

    readonly Dictionary<string, StreamDevice> _devices = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, SortedDictionary<DateTime, double>> _history = new(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _alerts = new();   // "MAC|metric|reason"
    readonly object _lock = new();

    public long DuplicatesSuppressed { get; private set; }

    static string Key(string mac, string kind, string reason) => $"{mac}|{kind}|{reason}";
    const string OfflineReason = "Device disconnected";

    public void Apply(StreamPacket p)
    {
        lock (_lock)
        {
            if (!_devices.TryGetValue(p.Mac, out var d))
            {
                d = new StreamDevice { Mac = p.Mac, Name = p.Mac };
                _devices[p.Mac] = d;                       // O(1) insert
                _history[p.Mac] = new SortedDictionary<DateTime, double>();
            }
            if (!string.IsNullOrWhiteSpace(p.Name)) d.Name = p.Name!;
            d.Category = p.Category;
            d.DeviceType = p.DeviceType.ToLowerInvariant();
            d.Metric = p.Metric;
            d.LastValue = p.Value;
            d.LastSeen = DateTime.UtcNow;

            var h = _history[p.Mac];
            h[p.Timestamp] = p.Value;
            while (h.Count > MaxHistory) h.Remove(h.Keys.First());

            _alerts.Remove(Key(p.Mac, "offline", OfflineReason));   // node is back
        }
    }

    
    public bool RaiseAlert(string mac, string metric, string reason)
    {
        lock (_lock)
        {
            if (_alerts.Add(Key(mac, metric.ToLowerInvariant(), reason))) return true;
            DuplicatesSuppressed++;
            return false;
        }
    }

    public void ClearAlert(string mac, string metric)
    {
        lock (_lock)
            _alerts.RemoveWhere(k => k.StartsWith($"{mac}|{metric.ToLowerInvariant()}|", StringComparison.OrdinalIgnoreCase));
    }

  
    public List<string> SweepOffline()
    {
        var added = new List<string>();
        lock (_lock)
            foreach (var d in _devices.Values)
                if (DateTime.UtcNow - d.LastSeen > OfflineAfter && _alerts.Add(Key(d.Mac, "offline", OfflineReason)))
                    added.Add(d.Mac);
        return added;
    }

    public bool SetState(string mac, string state, out string previous)
    {
        lock (_lock)
        {
            previous = "";
            if (!_devices.TryGetValue(mac, out var d) || d.DeviceType != "actuator") return false;
            previous = d.State;
            d.State = state;
            return true;
        }
    }

    public string NameOf(string mac)
    {
        lock (_lock) return _devices.TryGetValue(mac, out var d) ? d.Name : mac;
    }

    public List<DeviceView> Query(string? category, string? alertState, string? search)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            IEnumerable<StreamDevice> q = _devices.Values;
            if (!string.IsNullOrWhiteSpace(category) && category != "all")
                q = q.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(d => d.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || d.Mac.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || d.Category.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || d.Metric.Contains(search, StringComparison.OrdinalIgnoreCase));

            var views = q.Select(d => new DeviceView(d.Mac, d.Name, d.Category, d.DeviceType, d.State, d.Metric,
                    d.LastValue, d.LastSeen, now - d.LastSeen <= OfflineAfter,
                    _alerts.Where(a => a.StartsWith(d.Mac + "|", StringComparison.OrdinalIgnoreCase))
                           .Select(a => a.Split('|')[2]).ToArray())).ToList();

            return (alertState switch
            {
                "alert" => views.Where(v => v.Alerts.Length > 0),
                "offline" => views.Where(v => !v.Online),
                "healthy" => views.Where(v => v.Alerts.Length == 0),
                _ => views
            }).OrderBy(v => v.Name).ToList();
        }
    }

    public List<HistoryPoint> History(string mac, int take = 100)
    {
        lock (_lock)
            return _history.TryGetValue(mac, out var h)
                ? h.TakeLast(take).Select(kv => new HistoryPoint(kv.Key, kv.Value)).ToList()
                : new List<HistoryPoint>();
    }

    public List<object> ActiveAlerts()
    {
        lock (_lock)
            return _alerts.Select(a => a.Split('|'))
                          .Select(p => (object)new { mac = p[0], device = _devices.TryGetValue(p[0], out var d) ? d.Name : p[0], reason = p[2] })
                          .ToList();
    }
}