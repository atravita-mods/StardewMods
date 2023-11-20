namespace AtraCore.HarmonyPatches;

using AtraCore.Framework.Caches;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.SaveMigrations;

// [HarmonyPatch]
internal static class AddCharacterPatch
{
    private static bool AddingNPCs;

    // this is immediately after SaveGame.LoadDataToLocations but I don't risk being punted to a different thread.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.loadDataToLocations))]
    private static void PostfixLoading()
    {
        if (Game1.lastAppliedSaveFix < SaveFixes.AddNpcRemovalFlags)
        {
            AccessTools.TypeByName("SaveMigrator_1_6").GetMethod("AddNpcRemovalFlags", AccessTools.all)?.Invoke(null, null);
        }
        try
        {
            AddingNPCs = true;
            Game1.AddNPCs();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding missing NPCs at the right time", ex);
        }
        finally
        {
            AddingNPCs = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game1), nameof(Game1.AddCharacterIfNecessary))]
    private static void PostfixAddCharacter(string characterId, bool __result)
    {
        if (!__result || !AddingNPCs)
        {
            return;
        }

        ModEntry.ModMonitor.DebugOnlyLog($"Game added missing NPC {characterId}. Fixing them up.");
        if (NPCCache.GetByVillagerName(characterId, true) is not NPC npc)
        {
            ModEntry.ModMonitor.LogOnce($"Huh, game claims to have added {characterId} who could not be found.", LogLevel.Warn);
            return;
        }

        try
        {
            npc.dayUpdate(Game1.dayOfMonth);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"calling missing npc.DayUpdate for {characterId}", ex);
        }
    }
}
