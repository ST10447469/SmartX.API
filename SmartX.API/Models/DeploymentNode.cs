namespace SmartX.API.Models;

/// A node in a multi-tier deployment tree, e.g. Facility A -> Zone 1 -> Sub-Zone B.

public class DeploymentNode
{
    public string Name { get; set; } = string.Empty;
    public List<DeploymentNode> Children { get; set; } = new();
}