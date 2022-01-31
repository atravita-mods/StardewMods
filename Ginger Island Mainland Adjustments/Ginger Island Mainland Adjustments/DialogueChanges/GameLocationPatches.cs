using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.DialogueChanges;

/// <summary>
/// Patches on the GameLocation class, to handle override dialogue.
/// </summary>
[HarmonyPatch(typeof(GameLocation))]
internal class GameLocationPatches
{
    private const string ANTISOCIAL = "Resort_Antisocial";
    private const string ISLANDNORTH = "Resort_IslandNorth";

    /// <summary>
    /// Postfix on HasLocationOverrideDialogue.
    /// </summary>
    /// <param name="__instance">The game location.</param>
    /// <param name="character">The NPC talking.</param>
    /// <param name="__result">The return value of HasLocationDialogue.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameLocation.HasLocationOverrideDialogue))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    public static void PostfixHasLocationDialogue(GameLocation __instance, NPC character, ref bool __result)
    {
        try
        {
            if (__instance is (IslandNorth or IslandSouthEast))
            {
                if (Game1.player.friendshipData.TryGetValue(character.Name, out Friendship? friendship) && friendship.IsDivorced())
                {
                    __result = false;
                }
                else if (__instance is IslandNorth)
                {
                    __result = Game1.IsVisitingIslandToday(character.Name) && character.Dialogue.ContainsKey(ISLANDNORTH);
                }
                else if (__instance is IslandSouthEast)
                {
                    __result = Game1.IsVisitingIslandToday(character.Name) && character.Dialogue.ContainsKey(ANTISOCIAL);
                }
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Ran into errors in postfix for HasLocationOverrideDialogue\n\n{ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Patches GetLocationOverrideDialogue to yield specific dialogue.
    /// </summary>
    /// <param name="__instance">The game location.</param>
    /// <param name="character">The speaking NPC.</param>
    /// <param name="__result">The dialogue key to use instead.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameLocation.GetLocationOverrideDialogue))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    public static void PostfixGetLocationOverrideDialogue(GameLocation __instance, NPC character, ref string? __result)
    {
        try
        {
            if (__instance is (IslandNorth or IslandSouthEast))
            {
                if (__instance is IslandNorth)
                {
                    __result = $"Characters\\Dialogue\\{character.Name}:{ISLANDNORTH}";
                }
                else if (__instance is IslandSouthEast)
                {
                    __result = $"Characters\\Dialogue\\{character.Name}:{ANTISOCIAL}";
                }
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Ran into errors in postfix for GetLocationOverrideDialogue\n\n{ex}", LogLevel.Error);
        }
    }
}