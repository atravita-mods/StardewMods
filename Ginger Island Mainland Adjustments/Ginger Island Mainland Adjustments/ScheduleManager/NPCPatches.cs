using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Handles patches on the NPC class to allow beach fishing.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class NPCPatches
{
    /// <summary>
    /// Keep a list of fishers to reset their sprites at day end.
    /// Using a weakref because I don't care if the NPC vanishes.
    /// </summary>
    private static readonly List<WeakReference<NPC>> Fishers = new();

    /// <summary>
    /// resets the sprites of all people who went fishing.
    /// </summary>
    /// <remarks>Call at DayEnding.</remarks>
    internal static void ResetAllFishers()
    {
        if (Fishers.Count == 0)
        {
            return;
        }

        int count = 0;
        int skipped = 0;
        foreach (WeakReference<NPC>? npcRef in Fishers)
        {
            if (npcRef.TryGetTarget(out NPC? npc))
            {
                StardewValley.GameData.Characters.CharacterData? data = npc.GetData();
                npc.Sprite.SpriteHeight = data?.Size.Y ?? 32;
                npc.Sprite.SpriteWidth = data?.Size.X ?? 16;
                npc.Sprite.ignoreSourceRectUpdates = false;
                npc.Sprite.UpdateSourceRect();
                npc.drawOffset = Vector2.Zero;
                count++;
            }
            else
            {
                skipped++;
            }
        }

        Globals.ModMonitor.Log($"Reset sprite for {count} NPCs - {skipped} skipped", LogLevel.Trace);
        Fishers.Clear();
    }

    /// <summary>
    /// Extends sprite to allow for fishing sprites, which are 64px tall.
    /// </summary>
    /// <param name="__instance">NPC.</param>
    /// <param name="__0">animation key.</param>
    [HarmonyPostfix]
    [HarmonyPatch("startRouteBehavior")]
    private static void StartFishBehavior(NPC __instance, string __0)
    {
        try
        {
            if (__instance.currentLocation is IslandLocation && IsBeachFishAnimation(__instance, __0))
            {
                __instance.extendSourceRect(0, 32);
                __instance.Sprite.tempSpriteHeight = 64;
                __instance.drawOffset = new Vector2(0f, 96f);
                __instance.Sprite.ignoreSourceRectUpdates = false;
                if (Utility.isOnScreen(Utility.Vector2ToPoint(__instance.Position), 64, __instance.currentLocation))
                {
                    __instance.currentLocation.playSound("slosh", __instance.Tile);
                }
                Fishers.Add(new WeakReference<NPC>(__instance));
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError("postfixing startRouteBehavior", ex);
        }
    }

    /// <summary>
    /// Resets sprite when NPCs are done fishing.
    /// </summary>
    /// <param name="__instance">NPC.</param>
    /// <param name="__0">animation key.</param>
    /// <remarks>Force the reset no matter which map the NPC is currently on.</remarks>
    [HarmonyPostfix]
    [HarmonyPatch("finishRouteBehavior")]
    private static void EndFishBehavior(NPC __instance, string __0)
    {
        try
        {
            if (IsBeachFishAnimation(__instance, __0))
            {
                __instance.reloadSprite();
                StardewValley.GameData.Characters.CharacterData? data = __instance.GetData();
                __instance.Sprite.SpriteWidth = data?.Size.X ?? 16;
                __instance.Sprite.SpriteHeight = data?.Size.Y ?? 32;
                __instance.Sprite.UpdateSourceRect();
                __instance.drawOffset = Vector2.Zero;
                __instance.Halt();
                __instance.movementPause = 1;
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.LogError("postfixing finishRouteBehavior", ex);
        }
    }

    [MethodImpl(TKConstants.Hot)]
    private static bool IsBeachFishAnimation(NPC npc, string animationKey)
    {
        if (animationKey.TrySplitOnce('_', out ReadOnlySpan<char> name, out ReadOnlySpan<char> key))
        {
            return name.Equals(npc.Name, StringComparison.OrdinalIgnoreCase) && key.Equals("beach_fish", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}