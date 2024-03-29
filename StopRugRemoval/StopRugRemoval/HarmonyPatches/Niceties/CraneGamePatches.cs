﻿using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using HarmonyLib;

using StardewValley.Minigames;

namespace StopRugRemoval.HarmonyPatches.Niceties;

[HarmonyPatch(typeof(CraneGame.Claw))]
internal static class CraneGamePatches
{
    private static readonly Lazy<Action<CraneGame.Claw, int>> SetDropChance = new(
        () => typeof(CraneGame.Claw)
            .GetCachedField("_dropChances", ReflectionCache.FlagTypes.InstanceFlags)
            .GetInstanceFieldSetter<CraneGame.Claw, int>());

    [HarmonyPatch(nameof(CraneGame.Claw.GrabObject))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named For Harmony.")]
    private static void Postfix(CraneGame.Claw __instance)
        => SetDropChance.Value(__instance, ModEntry.Config.CraneGameDifficulty);
}
