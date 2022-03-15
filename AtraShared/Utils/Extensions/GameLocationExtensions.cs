using StardewValley.Locations;
using StardewValley.Monsters;

namespace AtraShared.Utils.Extensions;

internal static class GameLocationExtensions
{
    public static bool IsDangerousLocation(this GameLocation location)
        => !location.IsFarm && !location.IsGreenhouse && location is not SlimeHutch && location is not Town && location is not IslandWest
            && (location is MineShaft or VolcanoDungeon || location.characters.Any((character) => character is Monster));
}