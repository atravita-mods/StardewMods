using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.QueuePlayerAlert;

using AtraShared.Caching;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Wrappers;

using Microsoft.Xna.Framework;

using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;

namespace CatGiftsRedux.Framework;

/// <summary>
/// A utility class for this mod.
/// </summary>
internal static class Utils
{
    private static readonly TickCache<bool> isQiQuestActive = new(() => Game1.player.team.SpecialOrderRuleActive("QI_BEANS"));
    private static readonly TickCache<bool> isPerfectFarm = new(() => Game1.MasterPlayer.mailReceived.Contains("Farm_Enternal"));
    private static readonly TickCache<bool> islandUnlocked = new(() => FarmerHelpers.HasAnyFarmerRecievedFlag("seenBoatJourney"));

    /// <summary>
    /// Gets a value indicating whether Qi's bean quest is active. Only checks once per four ticks.
    /// </summary>
    internal static bool IsQiQuestActive => isQiQuestActive.GetValue();

    /// <summary>
    /// Check if the object should not be given by a random picker. Basically, no golden walnuts (73), qi gems (858), Qi beans or fruit unless the special order is active.
    /// 289 = ostrich egg, 928 is a golden egg.
    /// Or something that doesn't exist in Data/Objects.
    /// Or quest items.
    /// </summary>
    /// <param name="itemID">itemID of the item to check.</param>
    /// <returns>true to forbid it.</returns>
    internal static bool ForbiddenFromRandomPicking(string? itemID)
    {
        if (itemID is null)
        {
            return true;
        }
        switch (itemID)
        {
            case "73":
            case "858":
                return true;
            case "289":
            case "928":
                return !isPerfectFarm.GetValue();
            case "69":
            case "91":
            case "829":
            case "835":
            case "886":
            case "903":
                return !islandUnlocked.GetValue();
        }

        if (!Game1Wrappers.ObjectData.TryGetValue(itemID, out ObjectData? data))
        {
            return true;
        }

        if (data.Type == "Quest")
        {
            return true;
        }

        return !isQiQuestActive.GetValue() && data.Name.Contains("Qi", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a random empty tile on a map.
    /// </summary>
    /// <param name="location">The game location to get a random tile from.</param>
    /// <param name="tries">How many times to try.</param>
    /// <returns>Empty tile, or null to indicate failure.</returns>
    internal static Vector2? GetRandomTileImpl(this GameLocation location, int tries = 10)
    {
        do
        {
            Vector2 tile = location.getRandomTile();
            if (location.isWaterTile((int)tile.X, (int)tile.Y))
            {
                continue;
            }

            List<Vector2>? options = Utility.recursiveFindOpenTiles(location, tile, 1);
            if (options.Count > 0)
            {
                return options[0];
            }
        }
        while (tries-- > 0);

        return null;
    }

    /// <summary>
    /// Places the item at the specified tile, and alerts the player.
    /// </summary>
    /// <param name="location">Map.</param>
    /// <param name="tile">Tile to attempt.</param>
    /// <param name="item">Item to place.</param>
    /// <param name="pet">Pet to credit.</param>
    internal static void PlaceItem(this GameLocation location, Vector2 tile, Item item, Pet pet)
    {
        ModEntry.ModMonitor.DebugOnlyLog($"Placing {item.DisplayName} at {location.NameOrUniqueName} - {tile}");

        PlayerAlertHandler.AddMessage(
            message: new PetHudMessage(I18n.PetMessage(pet.Name, item.DisplayName), 2000, true, item, pet),
            soundCue: pet.GetPetData()?.ContentSound ?? "Cowboy_Footstep");

        if (item.HasTypeObject() && !location.Objects.ContainsKey(tile))
        {
            SObject obj = (item as SObject)!;
            if (!obj.bigCraftable.Value)
            {
                obj.IsSpawnedObject = true;
            }

            location.Objects[tile] = obj;

            if (pet.petType.Value == Pet.type_dog)
            {
                location.makeHoeDirt(tile, ignoreChecks: false);
            }
        }
        else
        {
            Debris? debris = new(item, tile * 64f);
            location.debris.Add(debris);
        }
    }
}