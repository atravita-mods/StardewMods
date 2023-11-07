using StardewValley.TerrainFeatures;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions on trees.
/// </summary>
public static class TreeExtensions
{
    /// <summary>
    /// Checks to see if a tree is a palm tree.
    /// </summary>
    /// <param name="tree">Tree to check.</param>
    /// <returns>True if palm tree, false otherwise.</returns>
    public static bool IsPalmTree(this Tree? tree)
        => tree is not null && (tree.treeType.Value is Tree.palmTree or Tree.palmTree2);
}