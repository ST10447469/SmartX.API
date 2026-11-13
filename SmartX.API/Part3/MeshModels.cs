namespace SmartX.API.Part3;

public class MeshNode
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "sensor";     // gateway | repeater | sensor | actuator
    public double X { get; set; }                    // 0-100, used to draw the graph
    public double Y { get; set; }
    public bool Down { get; set; }
}

/// <summary>A wireless link. Cost models power use per hop (higher = more power).</summary>
public record MeshLink(string A, string B, double Cost);
public record MstResult(List<MeshLink> Edges, double TotalCost, Dictionary<string, string?> Parent);

public record NodeView(string Id, string Name, string Kind, double X, double Y, bool Down, bool Reachable, int Hops);
public record LinkView(string A, string B, double Cost, bool InMst, bool Active);
public record RerouteItem(string Id, string Name, List<string> OldPath, List<string>? NewPath, bool Isolated);
public record RerouteView(string DownNode, string DownName, List<RerouteItem> Affected);
public record TopologyView(List<NodeView> Nodes, List<LinkView> Links, double MstCost, int MstEdges,
                           List<string> Down, List<string> Isolated, List<RerouteView> Reroutes);

public record NodeHop(string Id, string Name, int Hops, string? Via);
public record ReachResult(string Start, List<NodeHop> Reachable, List<string> Down, List<string> CutOff);
public record RouteInfo(string Id, string Name, bool Down, bool Reachable,
                        List<string> BfsPath, int BfsHops, double BfsCost,
                        List<string> MstPath, double MstCost);