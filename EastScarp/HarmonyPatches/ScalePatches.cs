namespace EastScarp.HarmonyPatches;

using System.Reflection;

using HarmonyLib;

/// <summary>
/// Patches for scaling NPCs.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony")]
internal static class ScalePatches
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NPC), nameof(NPC.reloadSprite));
        yield return AccessTools.Method(typeof(NPC), nameof(NPC.dayUpdate));
    }

    /// <summary>
    /// Applies scaling to a specific NPC instance.
    /// </summary>
    /// <param name="__instance">The npc instance.</param>
    [HarmonyPostfix]
    internal static void ApplyScale(NPC __instance)
    {
        try
        {
            if (__instance.GetData() is not { } data)
            {
                return;
            }

            if (data.CustomFields?.TryGetValue("EastScarpe.NPCScale", out string? val) == true && float.TryParse(val, out float scale))
            {
                ModEntry.ModMonitor.VerboseLog($"Assigning scale {scale} to npc {__instance.Name}");
                __instance.Scale = scale;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("applying NPC scale", ex);
        }
    }
}
