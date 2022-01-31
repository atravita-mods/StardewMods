using HarmonyLib;
using Microsoft.Xna.Framework;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Handles patches on the NPC class to allow beach fishing.
/// </summary>
internal class NPCPatches
{
    private static readonly List<NPC> Fishers = new();

    /// <summary>
    /// resets the sprites of all people who went fishing.
    /// </summary>
    public static void ResetAllFishers()
    {
        foreach (NPC npc in Fishers)
        {
            npc.Sprite.SpriteHeight = 32;
            npc.Sprite.SpriteWidth = 16;
            npc.Sprite.ignoreSourceRectUpdates = false;
            npc.Sprite.UpdateSourceRect();
            npc.drawOffset.Value = Vector2.Zero;
        }
        Fishers.Clear();
    }

    [HarmonyPostfix]
    [HarmonyPatch("startRouteBehavior")]
    public static void StartFishBehavior(NPC __instance, string __0)
    {
        try
        {
            if (__0.Equals(__instance.Name.ToLowerInvariant() + "_beach_fish"))
            {
                __instance.extendSourceRect(0, 32);
                __instance.Sprite.tempSpriteHeight = 64;
                __instance.drawOffset.Value = new Vector2(0f, 96f);
                __instance.Sprite.ignoreSourceRectUpdates = false;
                if (Utility.isOnScreen(Utility.Vector2ToPoint(__instance.Position), 64, __instance.currentLocation))
                {
                    __instance.currentLocation.playSoundAt("slosh", __instance.getTileLocation());
                }
                Fishers.Add(__instance);
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Ran into errors in postfix for startRouteBehavior\n\n{ex}", LogLevel.Error);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("endRouteBehavior")]
    public static void EndFishBehavior(NPC __instance, string __0)
    {
        try
        {
            if (__0.Equals(__instance.Name.ToLowerInvariant() + "_beach_fish"))
            {
                __instance.reloadSprite();
                __instance.Sprite.SpriteWidth = 16;
                __instance.Sprite.SpriteHeight = 32;
                __instance.Sprite.UpdateSourceRect();
                __instance.drawOffset.Value = Vector2.Zero;
                __instance.Halt();
                __instance.movementPause = 1;
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"Ran into errors in postfix for startRouteBehavior\n\n{ex}", LogLevel.Error);
        }
    }
}