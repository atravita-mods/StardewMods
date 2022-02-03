using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewValley.Monsters;

namespace ExpFromMonsterKillsOnFarm;

/// <summary>
/// Patches on the GameLocation class.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal class GameLocationPatches
{
    /// <summary>
    /// Appends EXP gain to monsterDrop.
    /// </summary>
    /// <param name="__instance">Game location.</param>
    /// <param name="__0">Monster killed.</param>
    /// <param name="__1">X location of monster killed.</param>
    /// <param name="__2">Y location of monster killed.</param>
    /// <param name="__3">Farmer who killed monster.</param>
    /// <remarks>This function is always called when a monster dies.</remarks>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameLocation.monsterDrop))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    public static void AppendMonsterDrop(GameLocation __instance, Monster __0, int __1, int __2, Farmer __3)
    {
        try
        {
            if (__instance.IsFarm && __3 is not null)
            {
                __3.gainExperience(Farmer.combatSkill, __0.ExperienceGained);
                ModEntry.ModMonitor.Log($"Granting {__3.displayName} {__0.ExperienceGained} combat XP for monster kill on farm");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed in granting combat xp on farm\n\n{ex}", LogLevel.Error);
        }
    }
}