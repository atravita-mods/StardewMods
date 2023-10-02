using AtraCore;

using AtraShared.ConstantsAndEnums;
using AtraShared.Menuing;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using StardewModdingAPI.Utilities;

using StardewValley.Objects;
using StardewValley.Tools;

using StopRugRemoval.Configuration;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Patches against SObject.
/// </summary>
[HarmonyPatch(typeof(SObject))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SObjectPatches
{
    /// <summary>
    /// Whether or not bombs have been confirmed.
    /// </summary>
    internal static readonly PerScreen<bool> HaveConfirmedBomb = new(createNewState: () => false);

    /// <summary>
    /// Prefix to prevent planting of wild trees on rugs.
    /// </summary>
    /// <param name="location">Game location.</param>
    /// <param name="tile">Tile to look at.</param>
    /// <param name="__result">the replacement result.</param>
    /// <returns>True to continue to vanilla function, false otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch("canPlaceWildTreeSeed")]
    private static bool PrefixWildTrees(GameLocation location, Vector2 tile, ref bool __result)
    {
        try
        {
            if (!ModEntry.Config.PreventPlantingOnRugs)
            {
                return true;
            }

            int posX = ((int)tile.X * Game1.tileSize) + 32;
            int posY = ((int)tile.Y * Game1.tileSize) + 32;
            foreach (Furniture f in location.furniture)
            {
                if (f.furniture_type.Value == Furniture.rug && f.GetBoundingBox().Contains(posX, posY))
                {
                    __result = false;
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("preventing tree planting", ex);
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.onExplosion))]
    private static void PrefixOnExplosion(SObject __instance)
    {
        try
        {
            if (__instance.IsSpawnedObject && ModEntry.Config.SaveBombedForage && ModEntry.Config.Enabled)
            {
                __instance.Location.debris.Add(new Debris(__instance, __instance.TileLocation * 64));
                ModEntry.ModMonitor.DebugOnlyLog(__instance.DisplayName + ' ' + __instance.TileLocation.ToString());
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("creating debris", ex);
        }
    }

    /// <summary>
    /// Prevent hoes from lifting up the scarecrows.
    /// </summary>
    /// <param name="__instance">SObject instance.</param>
    /// <param name="t">tool used.</param>
    /// <param name="__result">Result of the function.</param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.performToolAction))]
    private static bool PrefixPerformToolAction(SObject __instance, Tool t, ref bool __result)
    {
        __result = t is not Hoe || !__instance.IsScarecrow();
        return __result;
    }

    /// <summary>
    /// Prefix on placement to prevent planting of fruit trees and tea saplings on rugs, hopefully.
    /// </summary>
    /// <param name="__instance">SObject instance to check.</param>
    /// <param name="location">Gamelocation being placed in.</param>
    /// <param name="x">X placement location in pixel coordinates.</param>
    /// <param name="y">Y placement location in pixel coordinates.</param>
    /// <param name="__result">Result of the function.</param>
    /// <returns>True to continue to vanilla function, false otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.placementAction))]
    private static bool PrefixPlacementAction(SObject __instance, GameLocation location, int x, int y, ref bool __result)
    {
        if (!ReferenceEquals(__instance, Game1.player.ActiveObject) || !ModEntry.Config.Enabled)
        {
            return true;
        }
        try
        {
            if (ModEntry.Config.PreventPlantingOnRugs && __instance.isSapling())
            {
                foreach (Furniture f in location.furniture)
                {
                    if (f.GetBoundingBox().Contains(x, y))
                    {
                        Game1.showRedMessage(I18n.RugPlantingMessage());
                        __result = false;
                        return false;
                    }
                }
            }
            if (!HaveConfirmedBomb.Value && __instance.IsBomb()
                && (IsLocationConsideredDangerous(location) ? ModEntry.Config.BombsInDangerousAreas : ModEntry.Config.BombsInSafeAreas)
                    .HasFlag(Context.IsMultiplayer ? ConfirmationEnum.InMultiplayerOnly : ConfirmationEnum.NotInMultiplayer))
            {
                // handle the case where a bomb has already been placed.
                Vector2 loc = new(x, y);
                foreach (TemporaryAnimatedSprite tas in location.temporarySprites)
                {
                    if (tas.position.Equals(loc))
                    {
                        __result = false;
                        return false;
                    }
                }

                Response[] responses = new[]
                {
                    new Response("BombsYes", I18n.YesOne()).SetHotKey(Keys.Y),
                    new Response("BombsArea", I18n.YesArea()),
                    new Response("BombsNo", I18n.No()).SetHotKey(Keys.Escape),
                };

                Action?[] actions = new[]
                {
                    () =>
                    {
                        if (Game1.player.ActiveObject.IsBomb())
                        {
                            Game1.player.reduceActiveItemByOne();
                        }
                        GameLocationUtils.ExplodeBomb(Game1.player.currentLocation, __instance.ParentSheetIndex, loc, Game1.Multiplayer);
                    },
                    () =>
                    {
                        HaveConfirmedBomb.Value = true;
                        if (Game1.player.ActiveObject.IsBomb())
                        {
                            Game1.player.reduceActiveItemByOne();
                        }
                        GameLocationUtils.ExplodeBomb(Game1.player.currentLocation, __instance.ParentSheetIndex, loc, Game1.Multiplayer);
                    },
                };

                __result = false;

                Game1.activeClickableMenu = new DialogueAndAction(I18n.ConfirmBombs(), responses, actions, ModEntry.InputHelper);
                return false;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("prefixing SObject.placementAction", ex);
        }
        return true;
    }

    private static bool IsLocationConsideredDangerous(GameLocation location)
        => ModEntry.Config.SafeLocationMap.TryGetValue(location.Name, out IsSafeLocationEnum val)
            ? (val == IsSafeLocationEnum.Dangerous) || (val == IsSafeLocationEnum.Dynamic && location.IsDangerousLocation())
            : location.IsDangerousLocation();
}