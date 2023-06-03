using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;
using AtraBase.Toolkit.StringHandler;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;
using Microsoft.Xna.Framework;

using Netcode;
using StardewModdingAPI.Utilities;

using StardewValley.Network;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// A patch to try to unfuck schedules.
/// I think this may be antisocial causing issues.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Reviewed.")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ScheduleErrorFixer
{
    #region delegates

    private static readonly Lazy<Func<NPC, NetLocationRef>> _getLocationRef = new(() =>
        typeof(NPC).GetCachedField("currentLocationRef", ReflectionCache.FlagTypes.InstanceFlags)
                   .GetInstanceFieldGetter<NPC, NetLocationRef>()
    );

    private static readonly Lazy<Action<NetLocationRef, bool>> _markDirty = new(() =>
        typeof(NetLocationRef).GetCachedField("_dirty", ReflectionCache.FlagTypes.InstanceFlags)
                              .GetInstanceFieldSetter<NetLocationRef, bool>()
    );

    private static readonly Lazy<Func<NetLocationRef, NetString>> _getLocationName = new(() =>
        typeof(NetLocationRef).GetCachedField("locationName", ReflectionCache.FlagTypes.InstanceFlags)
                              .GetInstanceFieldGetter<NetLocationRef, NetString>()
    );

    #endregion

    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(NPC), nameof(NPC.parseMasterSchedule))]
    private static void Prefix(string rawData, NPC __instance)
    {
        if (__instance.currentLocation is not null || !__instance.isVillager())
        {
            return;
        }

        ModEntry.ModMonitor.Log($"{__instance.Name} seems to have a null current location, attempting to fix.", LogLevel.Info);
        ModEntry.ModMonitor.Log($"Multiplayer: {Context.IsMultiplayer}? Host: {Context.IsMainPlayer}? The current day is {SDate.Now()}.", LogLevel.Info);

        NetLocationRef backing = _getLocationRef.Value(__instance);
        NetString expectedName = _getLocationName.Value(backing);
        if (!string.IsNullOrWhiteSpace(expectedName.Value))
        {
            ModEntry.ModMonitor.Log($"Location Ref has value {expectedName}, marking dirty.", LogLevel.Info);
            _markDirty.Value(backing, true);
        }

        if (__instance.currentLocation is not null)
        {
            ModEntry.ModMonitor.Log("Successfully restored location reference!", LogLevel.Info);
            return;
        }

        ModEntry.ModMonitor.Log($"Their attempted schedule string was {rawData}", LogLevel.Info);
        bool foundSchedule = ModEntry.UtilitySchedulingFunctions.TryFindGOTOschedule(__instance, SDate.Now(), rawData, out string? scheduleString);
        if (foundSchedule)
        {
            ModEntry.ModMonitor.Log($"\tThat schedule redirected to {scheduleString}.", LogLevel.Info);
        }

        if (__instance.Name is "Leo" && Game1.MasterPlayer.hasOrWillReceiveMail("leoMoved") && Game1.getLocationFromName("LeoTreeHouse") is GameLocation leohouse)
        {
            __instance.currentLocation = leohouse;
            __instance.DefaultPosition = new Vector2(5f, 4f) * 64f;
        }
        else if (__instance.DefaultMap is not null && Game1.getLocationFromName(__instance.DefaultMap) is GameLocation location)
        { // Attempt to first just assign their position from their default map.
            __instance.currentLocation = location;
        }
        else if (Game1.content.Load<Dictionary<string, string>>(@"Data\NPCDispositions").TryGetValue(__instance.Name, out string? dispo))
        { // Okay, if that didn't work, try getting from NPCDispositions.
            ReadOnlySpan<char> pos = dispo.GetNthChunk('/', 10);
            if (pos.Length != 0)
            {
                SpanSplit locParts = pos.SpanSplit(expectedCount: 3);
                string defaultMap = locParts[0].ToString();
                if (Game1.getLocationFromName(defaultMap) is GameLocation loc)
                {
                    __instance.DefaultMap = defaultMap;
                    __instance.currentLocation = loc;
                }
                if (locParts.TryGetAtIndex(1, out SpanSplitEntry strX) && int.TryParse(strX, out int x)
                    && locParts.TryGetAtIndex(2, out SpanSplitEntry strY) && int.TryParse(strY, out int y))
                {
                    __instance.DefaultPosition = new Vector2(x * 64, y * 64);
                    return;
                }
            }
        }

        // Still no go, let's try parsing from the first schedule entry.
        if (__instance.currentLocation is null && foundSchedule)
        {
            SpanSplit splits = scheduleString.SpanSplit(expectedCount: 3);
            if (splits.TryGetAtIndex(1, out SpanSplitEntry locName) && Game1.getLocationFromName(locName.ToString()) is GameLocation loc)
            {
                __instance.currentLocation = loc;
                if (splits.TryGetAtIndex(2, out SpanSplitEntry strX) && int.TryParse(strX, out int x)
                    && splits.TryGetAtIndex(3, out SpanSplitEntry strY) && int.TryParse(strY, out int y))
                {
                    __instance.DefaultPosition = new Vector2(x * 64, y * 64);
                }
                return;
            }
        }
    }

    /// <summary>
    /// Prevent characters from being warped to a null location.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game1), nameof(Game1.warpCharacter), new[] { typeof(NPC), typeof(GameLocation), typeof(Vector2) })]
    private static bool PrefixCharacterWarp(NPC character, GameLocation? targetLocation)
    {
        if (character is null)
        {
            // weird. Someone called Game1.warpCharacter with a null character, just let that explode.
            return true;
        }

        if (character.currentLocation is null)
        {
            NetLocationRef backing = _getLocationRef.Value(character);
            NetString currLoc = _getLocationName.Value(backing);
            ModEntry.ModMonitor.Log($"{character.Name} has null currentLocation while attempting to warp. NetLocationRef reports {currLoc}", LogLevel.Info);
            if (!string.IsNullOrEmpty(currLoc))
            {
                ModEntry.ModMonitor.Log($"Forcing refresh for backing NetLocationRef.", LogLevel.Info);
                _markDirty.Value(backing, true);
            }
        }

        if (targetLocation is null)
        {
            ModEntry.ModMonitor.Log($"Someone has requested {character.Name} warp to a null location at game time {Game1.timeOfDay}. Suppressing that.", LogLevel.Error);
            return false;
        }
        return true;
    }
}
