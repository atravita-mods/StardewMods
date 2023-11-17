using System.Reflection;

using HarmonyLib;

namespace EastScarp.HarmonyPatches;

[HarmonyPatch]
internal static class ScalePatches
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NPC), nameof(NPC.reloadSprite));
        yield return AccessTools.Method(typeof(NPC), nameof(NPC.dayUpdate));
    }

    private static void Postfix(NPC __instance)
    {
        var data = __instance.GetData();
        if (data is null)
        {
            return;
        }

        if (data.CustomFields?.TryGetValue("EastScarpe.NPCScale", out var val) == true && float.TryParse(val, out var scale))
        {
            ModEntry.ModMonitor.VerboseLog($"Assigning scale {scale} to npc {__instance.Name}");
            __instance.Scale = scale;
        }
    }
}
