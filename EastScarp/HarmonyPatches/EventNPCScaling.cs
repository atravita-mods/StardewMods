namespace EastScarp.HarmonyPatches;

using HarmonyLib;

/// <summary>
/// Handles applying the scale to event actors.
/// </summary>
[HarmonyPatch(typeof(Event))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony")]
internal static class EventNPCScaling
{
    [HarmonyPatch("addActor")]
    private static void Postfix(Event __instance)
    {
        try
        {
            if (__instance.actors.Count > 0)
            {
                ScalePatches.ApplyScale(__instance.actors[^1]);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("applying scale to event actors", ex);
        }
    }
}
