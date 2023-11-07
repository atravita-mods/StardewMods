namespace ExpFromMonsterKillsOnFarm;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Monsters;
using StardewValley.SpecialOrders;

/// <summary>
/// Patches on the GameLocation class.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal class GameLocationPatches
{
    /// <summary>
    /// Appends EXP gain to monsterDrop.
    /// </summary>
    /// <param name="__instance">Game location.</param>
    /// <param name="monster">Monster killed.</param>
    /// <param name="who">Farmer who killed monster.</param>
    /// <remarks>This function is always called when a monster dies.</remarks>
    [HarmonyPostfix]
    [HarmonyPatch("onMonsterKilled")]
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
                who.gainExperience(Farmer.combatSkill, monster.ExperienceGained);
                ModEntry.ModMonitor.DebugOnlyLog($"Granting {who.Name} {monster.ExperienceGained} combat XP for monster kill on farm");
            }
            if (ModEntry.Config.QuestCompletion)
            {
                who.checkForQuestComplete(null, 1, 1, null, monster.Name, 4);
                ModEntry.ModMonitor.DebugOnlyLog($"Granting {who.Name} one kill of {monster.Name} towards billboard.");
            }
            if (ModEntry.Config.SpecialOrderCompletion && Game1.player.team.specialOrders is not null)
            {
                foreach (SpecialOrder order in Game1.player.team.specialOrders)
                {
                    if (order.onMonsterSlain is not null)
                    {
                        order.onMonsterSlain(Game1.player, monster);
                        ModEntry.ModMonitor.DebugOnlyLog($"Granting {who.Name} one kill of {monster.Name} towards special order {order.questKey}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("granting combat ex on farm", ex);
        }
    }
}