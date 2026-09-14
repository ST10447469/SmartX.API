namespace SmartX.API.Services;
using global::SmartX.API.Models;


/// <summary>
/// Recursively validates that a path exists inside a deployment tree, e.g.
/// confirming a sensor claiming to live in "Facility A/Zone 1/Sub-Zone B" is
/// actually configured under that branch.
/// </summary>
public class DeploymentValidator
{
    public bool ValidatePath(DeploymentNode root, string[] pathSegments, int depth = 0)
    {
        if (pathSegments.Length == 0) return false;

        // Base case: current node doesn't match the expected segment at this depth.
        if (!string.Equals(root.Name, pathSegments[depth], StringComparison.OrdinalIgnoreCase))
            return false;

        // Base case: we've matched every segment in the path.
        if (depth == pathSegments.Length - 1)
            return true;

        // Recursive case: try to match the remaining path in one of the children.
        foreach (var child in root.Children)
        {
            if (ValidatePath(child, pathSegments, depth + 1))
                return true;
        }

        return false;
    }
}
