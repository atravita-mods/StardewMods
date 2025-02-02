using Newtonsoft.Json;

namespace SinZsEventTester.Framework.EventTester;
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "This is a record.")]
internal record Node(string ResponseKey, int ResponsePosition, List<Node> Children)
{
    /// <summary>
    /// The color of the current node.
    /// </summary>
    [JsonProperty]
    internal Color Color { get; set; } = Color.White;

    /// <summary>
    /// Whether or not the node and all its children are finished.
    /// </summary>
    /// <returns>True if finished, false otherwise.</returns>
    internal bool ChildrenFinished()
    {
        switch (this.Color)
        {
            case Color.Black:
                return true;
            case Color.Blue:
            case Color.White:
                return false;
        }

        foreach (Node child in this.Children)
        {
            switch (child.Color)
            {
                case Color.White:
                case Color.Blue:
                    return false;
                case Color.Black:
                    continue;
                case Color.Grey:
                    if (child.ChildrenFinished())
                    {
                        child.Color = Color.Black;
                        continue;
                    }
                    return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Marks the status of each node.
/// </summary>
internal enum Color
{
    /// <summary>
    /// This node has not been visited before and has not queued its children.
    /// </summary>
    White,

    /// <summary>
    /// This node has queued its children, but has not been fully visited.
    /// </summary>
    Grey,

    /// <summary>
    /// This node is fully visited.
    /// </summary>
    Black,

    /// <summary>
    /// This node has not been visited before, but should only queue a single (also blue) child.
    /// </summary>
    Blue,
}