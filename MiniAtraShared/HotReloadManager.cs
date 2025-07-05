#if USING_HARMONY
using HarmonyLib;

using MiniAtraShared;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(HotReloadManager))]

namespace MiniAtraShared;

internal static class HotReloadManager
{
    public static void ClearCache(Type[]? types)
    {
        var harmony = new Harmony("hot.reload.handler");

        var methods = Harmony.GetAllPatchedMethods().ToArray();

        foreach (var method in methods)
        {
            harmony.Patch(original: method);
        }
    }
}
#endif