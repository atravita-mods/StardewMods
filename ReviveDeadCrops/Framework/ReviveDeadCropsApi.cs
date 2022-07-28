using AtraBase.Toolkit.Extensions;
using AtraCore.Utilities;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace ReviveDeadCrops.Framework;

/// <inheritdoc />
public class ReviveDeadCropsApi : IReviveDeadCropsApi
{
    private const int FAIRY_DUST = 872;

    /// <inheritdoc />
    public bool CanApplyDust(GameLocation loc, Vector2 tile, SObject obj)
    {
        if (!Utility.IsNormalObjectAtParentSheetIndex(obj, FAIRY_DUST))
        {
            return false;
        }
        if (loc.terrainFeatures.TryGetValue(tile, out var terrain) && terrain is HoeDirt dirt)
        {
            return dirt.crop?.dead?.Value == true;
        }
        return false;
    }

    public bool TryApplyDust(GameLocation loc, Vector2 tile, SObject obj)
    {
        if (this.CanApplyDust(loc, tile, obj))
        {
            if (loc.terrainFeatures.TryGetValue(tile, out var terrain)
                && terrain is HoeDirt dirt && dirt.crop is Crop crop)
            {
                this.RevivePlant(crop);
                return true;
            }
        }
        return false;
    }

    public void RevivePlant(Crop crop)
    {
        // it's alive!
        crop.dead.Value = false;
        if (crop.rowInSpriteSheet.Value != Crop.rowOfWildSeeds && crop.netSeedIndex.Value == -1)
        {
            crop.InferSeedIndex();
        }

        int seedIndex = crop.rowInSpriteSheet.Value != Crop.rowOfWildSeeds ? crop.netSeedIndex.Value : crop.whichForageCrop.Value;

        Dictionary<int, string> cropData = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");

        // make sure that the raised value is set again.
        if (cropData.TryGetValue(seedIndex, out string? data))
        {
            ReadOnlySpan<char> segment = data.GetNthChunk('/', 7);
            if (segment.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                crop.raisedSeeds.Value = true;
            }
        }

        // grow it completely.
        crop.growCompletely();
    }

    public void AnimateRevival(GameLocation loc, Vector2 tile)
    {
        // borrowing a Prarie King TAS for this.
        TemporaryAnimatedSprite? tas = new TemporaryAnimatedSprite(
            textureName: Game1.mouseCursorsName,
            sourceRect: new Rectangle(464, 1808, 16, 16),
            animationInterval: 120f,
            animationLength: 5,
            numberOfLoops: 0,
            position: (tile * 64f) + new Vector2(32f, 32f),
            flicker: false,
            flipped: false,
            layerDepth: (tile.Y / 10000f) + 0.01f,
            alphaFade: 0f,
            color: Color.White * 0.8f,
            scale: 3f,
            scaleChange: 0f,
            rotation: 0f,
            rotationChange: 0f);
        MultiplayerHelpers.GetMultiplayer().broadcastSprites(loc, tas);
    }
}
