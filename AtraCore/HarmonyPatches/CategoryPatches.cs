using HarmonyLib;

using Microsoft.Xna.Framework;

namespace AtraCore.HarmonyPatches;

[HarmonyPatch(typeof(SObject))]
internal static class CategoryPatches
{
    private static readonly Dictionary<string, (string title, Color color)> _cache = new();

    internal static void Reset() => _cache.Clear();
}
