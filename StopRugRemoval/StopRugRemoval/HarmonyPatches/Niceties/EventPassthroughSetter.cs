using AtraShared.ConstantsAndEnums;

using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Forcibly set the pass-through-things flag on every character in an event.
/// There's no reason not to set it and this prevents crashes.
/// </summary>
[HarmonyPatch(typeof(Event))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class EventPassthroughSetter
{
    [UsedImplicitly]
    [HarmonyPatch("setUpCharacters")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used implicitly.")]
    private static void Postfix(Event __instance)
    {
        if (!ModEntry.Config.Enabled)
        {
            return;
        }
        foreach (Farmer f in __instance.farmerActors)
        {
            f.ignoreCollisions = true;
        }
        foreach (NPC a in __instance.actors)
        {
            a.isCharging = true;
        }
    }
}