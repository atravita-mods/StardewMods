namespace ExpFromMonsterKillsOnFarm;

using MiniAtraShared.Extensions;

using HarmonyLib;

using StardewValley.Monsters;

/// <summary>
/// Patches on the GameLocation class.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Named for Harmony")]
internal class GameLocationPatches
{
    internal static void ApplyPatch(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(GameLocation), "onMonsterKilled"),
            postfix: new HarmonyMethod(typeof(GameLocationPatches), nameof(AppendMonsterDrop)));
    }

    /// <summary>
    /// Appends EXP gain to monsterDrop.
    /// </summary>
    /// <param name="__instance">Game location.</param>
    /// <param name="monster">Monster killed.</param>
    /// <param name="who">Farmer who killed monster.</param>
    /// <remarks>This function is always called when a monster dies.</remarks>
    private static void AppendMonsterDrop(GameLocation __instance, Monster monster, Farmer who)
    {
        try
        {
            if (who is null || !__instance.IsFarm)
            {
                return;
            }
            if (ModEntry.Config.GainExp)
            {
                int amount = monster.ExperienceGained - Math.Max(1, monster.ExperienceGained / 3);
                who.gainExperience(Farmer.combatSkill, amount);
                ModEntry.ModMonitor.DebugOnlyLog($"Granting {who.Name} {amount} combat XP for monster kill on farm");
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("granting combat ex on farm", ex);
        }
    }
}