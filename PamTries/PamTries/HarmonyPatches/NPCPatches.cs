using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace PamTries.HarmonyPatches;

/// <summary>
/// Class that holds patches against NPC so Pam can fish.
/// </summary>
[HarmonyPatch(typeof(NPC))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class NPCPatches
{
    /// <summary>
    /// Set Pam's sprite to fish.
    /// </summary>
    /// <param name="__instance">NPC.</param>
    /// <param name="__0">animation_description.</param>
    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch("startRouteBehavior")]
    private static void StartFishBehavior(NPC __instance, string __0)
    {
        try
        {
            if (__0.Equals("pam_fish", StringComparison.OrdinalIgnoreCase))
            {
                __instance.extendSourceRect(0, 32);
                __instance.Sprite.tempSpriteHeight = 64;
                __instance.drawOffset = new Vector2(0f, 96f);
                __instance.Sprite.ignoreSourceRectUpdates = false;
                if (Utility.isOnScreen(Utility.Vector2ToPoint(__instance.Position), 64, __instance.currentLocation))
                {
                    __instance.currentLocation.playSound("slosh", __instance.Tile);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjusting startRouteBehavior for Pam", ex);
        }
    }

    /// <summary>
    /// Reset Pam's fishing sprite when done fishing.
    /// </summary>
    /// <param name="__instance">NPC.</param>
    /// <param name="__0">animation_description.</param>
    [UsedImplicitly]
    [HarmonyPostfix]
    [HarmonyPatch("finishRouteBehavior")]
    private static void EndFishBehavior(NPC __instance, string __0)
    {
        try
        {
            if (__0.Equals("pam_fish", StringComparison.OrdinalIgnoreCase))
            {
                __instance.reloadSprite();
                __instance.Sprite.SpriteWidth = 16;
                __instance.Sprite.SpriteHeight = 32;
                __instance.Sprite.UpdateSourceRect();
                __instance.drawOffset = Vector2.Zero;
                __instance.Halt();
                __instance.movementPause = 1;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjusting finishRouteBehavior for Pam", ex);
        }
    }
}