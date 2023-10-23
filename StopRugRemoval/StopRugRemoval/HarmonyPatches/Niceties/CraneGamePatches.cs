using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Minigames;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Holds patches for the crane game.
/// </summary>
[HarmonyPatch(typeof(CraneGame.Claw))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class CraneGamePatches
{
    private static readonly Lazy<Action<CraneGame.Claw, int>> SetDropChance = new(
        () => typeof(CraneGame.Claw)
            .GetCachedField("_dropChances", ReflectionCache.FlagTypes.InstanceFlags)
            .GetInstanceFieldSetter<CraneGame.Claw, int>());

    [HarmonyPatch(nameof(CraneGame.Claw.GrabObject))]
    private static void Postfix(CraneGame.Claw __instance)
    {
        try
        {
            SetDropChance.Value(__instance, ModEntry.Config.CraneGameDifficulty);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding crane game difficulty", ex);
        }
    }
}
