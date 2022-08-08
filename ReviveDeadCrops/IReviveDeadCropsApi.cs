using Microsoft.Xna.Framework;

namespace ReviveDeadCrops;

/// <summary>
/// The api for Revive Dead Crops.
/// </summary>
public interface IReviveDeadCropsApi
{
    /// <summary>
    /// Whether or not fairy dust can be applied to this square to revive a dead plant.
    /// </summary>
    /// <param name="loc">Map to check.</param>
    /// <param name="tile">Tile to check.</param>
    /// <param name="obj">Object to check.</param>
    /// <returns>True if the obj is fairy dust and it can be applied to that square, false otherwise.</returns>
    public bool CanApplyDust(GameLocation loc, Vector2 tile, SObject obj);

    /// <summary>
    /// Tries to apply fairy dust to a square to revive a dead plant.
    /// </summary>
    /// <param name="loc">Map to check.</param>
    /// <param name="tile">Tile to check.</param>
    /// <param name="obj">Object to check.</param>
    /// <returns>True if successfully applied, false otherwise.</returns>
    public bool TryApplyDust(GameLocation loc, Vector2 tile, SObject obj);

    /// <summary>
    /// Revives a plant.
    /// </summary>
    /// <param name="crop">The crop to revive.</param>
    public void RevivePlant(Crop crop);

    /// <summary>
    /// Adds a little animation for the revival.
    /// </summary>
    /// <param name="loc">The game location.</param>
    /// <param name="tile">Animation to play.</param>
    public void AnimateRevival(GameLocation loc, Vector2 tile);
}
