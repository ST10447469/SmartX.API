using Microsoft.AspNetCore.Components;

namespace SmartX.API.Part3;

/// <summary>
/// The IoT mesh as an undirected weighted graph (adjacency lists).
/// BFS  : who can still reach the gateway, in how many hops, and which nodes are cut off.
/// Prim : minimum spanning tree = the lowest total power cost way to connect every live node to the gateway.
/// </summary>
public class MeshGraph
{
    public const string GatewayId = "GATEWAY";

    readonly Dictionary<string, MeshNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, List<(string To, double Cost)>> _adj = new(StringComparer.OrdinalIgnoreCase);
    readonly List<MeshLink> _links = new();
    readonly object _lock = new();

    public MeshGraph() => Seed();

    static string Norm(string id) => id.Trim().ToUpperInvariant();
    static string Key(string a, string b) => string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

    // ------------------------------------------------------------------ building the mesh

    void AddNodeCore(string id, string name, string kind, double x, double y)
    {
        _nodes[id] = new MeshNode { Id = id, Name = name, Kind = kind, X = x, Y = y };
        _adj[id] = new List<(string To, double Cost)>();
    }

    void AddLinkCore(string a, string b, double cost)
    {
        _adj[a].Add((b, cost));
        _adj[b].Add((a, cost));
        _links.Add(new MeshLink(a, b, cost));
    }

    void Seed()
    {
        _nodes.Clear(); _adj.Clear(); _links.Clear();
        const string N1 = "24:6F:28:AA:00:01", N2 = "24:6F:28:AA:00:02", N3 = "24:6F:28:AA:00:03", N4 = "24:6F:28:AA:00:04";
        const string N5 = "24:6F:28:AA:00:05", N6 = "24:6F:28:AA:00:06", N7 = "24:6F:28:AA:00:07", N8 = "24:6F:28:AA:00:08";

        AddNodeCore(GatewayId, "Gateway", "gateway", 50, 50);
        AddNodeCore(N5, "Repeater West", "repeater", 30, 50);
        AddNodeCore(N6, "Repeater East", "repeater", 70, 50);
        AddNodeCore(N1, "Moisture Sensor B", "sensor", 14, 22);
        AddNodeCore(N2, "Pump A", "actuator", 14, 78);
        AddNodeCore(N3, "Smart Relay 1", "actuator", 86, 22);
        AddNodeCore(N4, "Greenhouse Temp", "sensor", 86, 78);
        AddNodeCore(N7, "Tank Level Sensor", "sensor", 50, 12);
        AddNodeCore(N8, "Light Sensor", "sensor", 50, 88);

        AddLinkCore(GatewayId, N5, 2); AddLinkCore(GatewayId, N6, 2);
        AddLinkCore(GatewayId, N7, 4); AddLinkCore(GatewayId, N8, 4);
        AddLinkCore(N5, N1, 3); AddLinkCore(N5, N2, 3); AddLinkCore(N1, N2, 5);
        AddLinkCore(N6, N3, 3); AddLinkCore(N6, N4, 3); AddLinkCore(N3, N4, 5);
        AddLinkCore(N7, N1, 6); AddLinkCore(N7, N3, 6);
        AddLinkCore(N8, N2, 6); AddLinkCore(N8, N4, 6);
        AddLinkCore(N5, N7, 3); AddLinkCore(N6, N8, 3);
    }

    public void Reset() { lock (_lock) Seed(); }

    /// <summary>Simulates a hardware or power failure. The gateway itself cannot be taken down.</summary>
    public bool SetDown(string id, bool down)
    {
        lock (_lock)
        {
            id = Norm(id);
            if (id == GatewayId || !_nodes.TryGetValue(id, out var node)) return false;
            node.Down = down;
            return true;
        }
    }

    // ------------------------------------------------------------------ breadth-first search

    /// <summary>
    /// Breadth-first search from 'start' using a Queue. Down nodes are never entered unless
    /// ignoreDown is true (used to see what the healthy network looked like).
    /// Parent[x] is the next hop from x back towards 'start'.
    /// </summary>
    (Dictionary<string, string?> Parent, Dictionary<string, int> Depth) Bfs(string start, bool ignoreDown)
    {
        var parent = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var depth = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!_nodes.TryGetValue(start, out var s) || (!ignoreDown && s.Down)) return (parent, depth);

        var queue = new Queue<string>();
        parent[start] = null;
        depth[start] = 0;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var (to, _) in _adj[cur])
            {
                if (parent.ContainsKey(to)) continue;
                if (!ignoreDown && _nodes[to].Down) continue;
                parent[to] = cur;
                depth[to] = depth[cur] + 1;
                queue.Enqueue(to);
            }
        }
        return (parent, depth);
    }

    static List<string> ChainToRoot(Dictionary<string, string?> parent, string id)
    {
        var path = new List<string>();
        string? cur = id;
        while (cur != null) { path.Add(cur); cur = parent[cur]; }
        return path;
    }

    double CostOf(string a, string b) => _adj[a].First(x => string.Equals(x.To, b, StringComparison.OrdinalIgnoreCase)).Cost;

    double PathCost(List<string> path)
    {
        double total = 0;
        for (int i = 0; i < path.Count - 1; i++) total += CostOf(path[i], path[i + 1]);
        return total;
    }

    /// <summary>Every device currently connected to 'start' (the gateway or any subnet node), plus who is down or cut off.</summary>
    public ReachResult? Reachable(string start)
    {
        lock (_lock)
        {
            start = Norm(start);
            if (!_nodes.ContainsKey(start)) return null;
            var (parent, depth) = Bfs(start, false);
            var reach = parent.Keys.OrderBy(k => depth[k]).ThenBy(k => _nodes[k].Name)
                              .Select(k => new NodeHop(k, _nodes[k].Name, depth[k], parent[k])).ToList();
            var down = _nodes.Values.Where(x => x.Down).Select(x => x.Id).ToList();
            var cut = _nodes.Values.Where(x => !x.Down && !parent.ContainsKey(x.Id)).Select(x => x.Id).ToList();
            return new ReachResult(start, reach, down, cut);
        }
    }

    // ------------------------------------------------------------------ minimum spanning tree (Prim)

    /// <summary>
    /// Prim's algorithm grown from the gateway: repeatedly take the cheapest link (PriorityQueue)
    /// that joins a new live node to the tree. The result connects every reachable node at the
    /// lowest total power cost.
    /// </summary>
    public MstResult Mst()
    {
        lock (_lock)
        {
            var inTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { GatewayId };
            var parent = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { [GatewayId] = null };
            var edges = new List<MeshLink>();
            double total = 0;
            var pq = new PriorityQueue<(string From, string To, double Cost), double>();

            foreach (var (to, cost) in _adj[GatewayId])
                if (!_nodes[to].Down) pq.Enqueue((GatewayId, to, cost), cost);

            while (pq.TryDequeue(out var e, out _))
            {
                if (inTree.Contains(e.To)) continue;
                inTree.Add(e.To);
                parent[e.To] = e.From;
                edges.Add(new MeshLink(e.From, e.To, e.Cost));
                total += e.Cost;
                foreach (var (next, nextCost) in _adj[e.To])
                    if (!inTree.Contains(next) && !_nodes[next].Down) pq.Enqueue((e.To, next, nextCost), nextCost);
            }
            return new MstResult(edges, total, parent);
        }
    }

    // ------------------------------------------------------------------ routes and self-healing

    public RouteInfo? Route(string id)
    {
        lock (_lock)
        {
            id = Norm(id);
            if (!_nodes.TryGetValue(id, out var node)) return null;
            var (parent, _) = Bfs(GatewayId, false);
            var mst = Mst();
            bool reachable = !node.Down && parent.ContainsKey(id);
            var bfsPath = reachable ? ChainToRoot(parent, id) : new List<string>();
            var mstPath = reachable && mst.Parent.ContainsKey(id) ? ChainToRoot(mst.Parent, id) : new List<string>();
            return new RouteInfo(id, node.Name, node.Down, reachable,
                bfsPath, bfsPath.Count == 0 ? 0 : bfsPath.Count - 1, PathCost(bfsPath),
                mstPath, PathCost(mstPath));
        }
    }

    /// <summary>
    /// For every down node: which nodes used to route through it, and where they hop now.
    /// A null NewPath means the node is cut off from the gateway.
    /// </summary>
    public List<RerouteView> Reroutes()
    {
        lock (_lock)
        {
            var result = new List<RerouteView>();
            var healthy = Bfs(GatewayId, true).Parent;
            var current = Bfs(GatewayId, false).Parent;

            foreach (var failed in _nodes.Values.Where(x => x.Down))
            {
                var affected = new List<RerouteItem>();
                foreach (var node in _nodes.Values)
                {
                    if (node.Id == failed.Id || node.Down || !healthy.ContainsKey(node.Id)) continue;
                    var oldPath = ChainToRoot(healthy, node.Id);
                    if (!oldPath.Contains(failed.Id, StringComparer.OrdinalIgnoreCase)) continue;
                    bool ok = current.ContainsKey(node.Id);
                    affected.Add(new RerouteItem(node.Id, node.Name, oldPath, ok ? ChainToRoot(current, node.Id) : null, !ok));
                }
                result.Add(new RerouteView(failed.Id, failed.Name, affected));
            }
            return result;
        }
    }

    /// <summary>Everything the topology page needs in one call.</summary>
    public TopologyView Snapshot()
    {
        lock (_lock)
        {
            var (parent, depth) = Bfs(GatewayId, false);
            var mst = Mst();
            var mstKeys = new HashSet<string>(mst.Edges.Select(e => Key(e.A, e.B)));

            var nodes = _nodes.Values.OrderBy(x => x.Kind == "gateway" ? 0 : 1).ThenBy(x => x.Name)
                .Select(x => new NodeView(x.Id, x.Name, x.Kind, x.X, x.Y, x.Down,
                    parent.ContainsKey(x.Id), depth.TryGetValue(x.Id, out var hops) ? hops : -1)).ToList();

            var links = _links.Select(l => new LinkView(l.A, l.B, l.Cost, mstKeys.Contains(Key(l.A, l.B)),
                !_nodes[l.A].Down && !_nodes[l.B].Down)).ToList();

            var down = _nodes.Values.Where(x => x.Down).Select(x => x.Id).ToList();
            var isolated = _nodes.Values.Where(x => !x.Down && !parent.ContainsKey(x.Id)).Select(x => x.Id).ToList();
            return new TopologyView(nodes, links, mst.TotalCost, mst.Edges.Count, down, isolated, Reroutes());
        }
    }
}
