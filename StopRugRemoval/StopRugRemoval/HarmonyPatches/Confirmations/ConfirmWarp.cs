using AtraBase.Toolkit.Reflection;
using AtraShared.Menuing;
using AtraShared.Utils.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Tools;
using StopRugRemoval.Configuration;
using xTile.Dimensions;

namespace StopRugRemoval.HarmonyPatches.Confirmations;

[HarmonyPatch]
internal static class ConfirmWarp
{
    internal static readonly PerScreen<bool> HaveConfirmed = new(createNewState: () => false);

    /// <summary>
    /// The location to warp to.
    /// </summary>
    internal enum WarpLocation
    {
        None = -1,
        Farm = 688,
        Mountain = 689,
        Beach = 690,
        Desert = 261,
        IslandSouth = 886,
    }

#warning - find DH's mod's uniqueID to exlude this patch, also patch the IslandWest obelisk
    internal static void ApplyWandPatches(Harmony harmony)
    {
        harmony.Patch(
            original: typeof(Wand).InstanceMethodNamed(nameof(Wand.DoFunction)),
            prefix: new HarmonyMethod(typeof(ConfirmWarp), nameof(PrefixWand)));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SObject), nameof(SObject.performUseAction))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    private static bool PrefixTotemWarp(SObject __instance, GameLocation location, ref bool __result)
    {
        if (!Enum.IsDefined((WarpLocation)__instance.ParentSheetIndex) || __instance.bigCraftable.Value)
        { // Not an attempt to warp.
            return true;
        }

        WarpLocation locationEnum = (WarpLocation)__instance.ParentSheetIndex;

        if (Game1.getLocationFromName(locationEnum.ToString()) is not GameLocation loc)
        { // Something went very wrong. I cannot find the location at all....
            return true;
        }

        if (loc.IsBeforeFestivalAtLocation(ModEntry.ModMonitor, alertPlayer: true))
        { // Festival. Can't warp anyways.
            __result = false;
            return false;
        }

        if (!HaveConfirmed.Value
            && (IsLocationConsideredDangerous(location) ? ModEntry.Config.WarpsInDangerousAreas : ModEntry.Config.WarpsInSafeAreas)
                .HasFlag(Context.IsMultiplayer ? ConfirmationEnum.InMultiplayerOnly : ConfirmationEnum.NotInMultiplayer))
        {
            List<Response> responses = new()
            {
                new Response("WarpsNo", I18n.No()).SetHotKey(Keys.Escape),
                new Response("WarpsYes", I18n.Yes()).SetHotKey(Keys.Y),
            };

            List<Action?> actions = new()
            {
                null,
                () =>
                {
                    HaveConfirmed.Value = true;
                    __instance.performUseAction(location);
                    Game1.player.reduceActiveItemByOne();
                    HaveConfirmed.Value = false;
                },
            };

            __result = false;
            Game1.activeClickableMenu = new DialogueAndAction(I18n.ConfirmWarps(), responses, actions);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Building), nameof(Building.doAction))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static bool PrefixBuildingAction(Building __instance, Vector2 tileLocation, Farmer who, ref bool __result)
    {
        if (__instance.isTilePassable(tileLocation) || !__instance.buildingType.Contains("Obelisk") || !who.IsLocalPlayer)
        {
            return true;
        }
        WarpLocation location = __instance.buildingType.Value switch
        {
            "Earth Obelisk" => WarpLocation.Mountain,
            "Water Obelisk" => WarpLocation.Beach,
            "Desert Obelisk" => WarpLocation.Desert,
            "Island Obelisk" => WarpLocation.IslandSouth,
            _ => WarpLocation.None,
        };

        if (location is WarpLocation.None || Game1.getLocationFromName(location.ToString()) is not GameLocation loc)
        { // Something went very wrong. I cannot find the location at all....
            return true;
        }

        if (loc.IsBeforeFestivalAtLocation(ModEntry.ModMonitor, alertPlayer: true))
        { // Festival. Can't warp anyways.
            __result = false;
            return false;
        }

        if (!HaveConfirmed.Value
            && (IsLocationConsideredDangerous(who.currentLocation) ? ModEntry.Config.WarpsInDangerousAreas : ModEntry.Config.WarpsInSafeAreas)
                .HasFlag(Context.IsMultiplayer ? ConfirmationEnum.InMultiplayerOnly : ConfirmationEnum.NotInMultiplayer))
        {
            List<Response> responses = new()
            {
                new Response("WarpsNo", I18n.No()).SetHotKey(Keys.Escape),
                new Response("WarpsYes", I18n.Yes()).SetHotKey(Keys.Y),
            };

            List<Action?> actions = new()
            {
                null,
                () =>
                {
                    HaveConfirmed.Value = true;
                    __instance.doAction(tileLocation, who);
                    HaveConfirmed.Value = false;
                },
            };

            __result = false;
            Game1.activeClickableMenu = new DialogueAndAction(I18n.ConfirmWarps(), responses, actions);
            return false;
        }
        return true;
    }

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static bool PrefixWand(Wand __instance, GameLocation location, int x, int y, int power, Farmer who)
    {
        if (!who.IsLocalPlayer)
        {
            return true;
        }
        if (!HaveConfirmed.Value
             && (IsLocationConsideredDangerous(location) ? ModEntry.Config.WarpsInDangerousAreas : ModEntry.Config.WarpsInSafeAreas)
                 .HasFlag(Context.IsMultiplayer ? ConfirmationEnum.InMultiplayerOnly : ConfirmationEnum.NotInMultiplayer))
        {
            List<Response> responses = new()
            {
                new Response("WarpsNo", I18n.No()).SetHotKey(Keys.Escape),
                new Response("WarpsYes", I18n.Yes()).SetHotKey(Keys.Y),
            };

            List<Action?> actions = new()
            {
                null,
                () =>
                {
                    HaveConfirmed.Value = true;
                    __instance.DoFunction(location, x, y, power, who);
                    HaveConfirmed.Value = false;
                },
            };
            Game1.activeClickableMenu = new DialogueAndAction(I18n.ConfirmWarps(), responses, actions);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(IslandWest), nameof(IslandWest.performAction))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static bool PrefixIslandWest(IslandWest __instance, string action, Farmer who, Location tileLocation)
    {
        if (action == "FarmObelisk" && !HaveConfirmed.Value
            && (IsLocationConsideredDangerous(__instance) ? ModEntry.Config.WarpsInDangerousAreas : ModEntry.Config.WarpsInSafeAreas)
                 .HasFlag(Context.IsMultiplayer ? ConfirmationEnum.InMultiplayerOnly : ConfirmationEnum.NotInMultiplayer))
        {
            List<Response> responses = new()
            {
                new Response("WarpsNo", I18n.No()).SetHotKey(Keys.Escape),
                new Response("WarpsYes", I18n.Yes()).SetHotKey(Keys.Y),
            };

            List<Action?> actions = new()
            {
                null,
                () =>
                {
                    HaveConfirmed.Value = true;
                    __instance.performAction(action, who, tileLocation);
                    HaveConfirmed.Value = false;
                },
            };
            Game1.activeClickableMenu = new DialogueAndAction(I18n.ConfirmWarps(), responses, actions);
            return false;
        }
        return true;
    }

    private static bool IsLocationConsideredDangerous(GameLocation location)
        => ModEntry.Config.SafeLocationMap.TryGetValue(location.NameOrUniqueName, out IsSafeLocationEnum val)
            ? (val == IsSafeLocationEnum.Dangerous) || (val == IsSafeLocationEnum.Dynamic && location.IsDangerousLocation())
            : location.IsDangerousLocation();
}