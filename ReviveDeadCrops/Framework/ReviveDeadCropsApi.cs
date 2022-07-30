using AtraBase.Toolkit.Extensions;
using AtraCore.Utilities;
using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace ReviveDeadCrops.Framework;

/// <inheritdoc />
public class ReviveDeadCropsApi : IReviveDeadCropsApi
{
    /// <summary>
    /// The moddata key to mark plants that got revived.
    /// </summary>
    internal const string REVIVED_PLANT_MARKER = "atravita.RevivedPlant";

    private const int FAIRY_DUST = 872;

    /// <inheritdoc />
    public bool CanApplyDust(GameLocation loc, Vector2 tile, SObject obj)
    {
        if (!Utility.IsNormalObjectAtParentSheetIndex(obj, FAIRY_DUST))
        {
            return false;
        }
        if (loc.GetHoeDirtAtTile(tile) is HoeDirt dirt)
        {
            return dirt.crop?.dead?.Value == true;
        }
        return false;
    }

    /// <inheritdoc />
    public bool TryApplyDust(GameLocation loc, Vector2 tile, SObject obj)
    {
        if (this.CanApplyDust(loc, tile, obj))
        {
            if (loc.GetHoeDirtAtTile(tile) is HoeDirt dirt && dirt.crop is Crop crop)
            {
                if (!loc.SeedsIgnoreSeasonsHere())
                {
                    dirt.modData?.SetBool(REVIVED_PLANT_MARKER, true);
                }

                this.AnimateRevival(loc, tile);
                DelayedAction.functionAfterDelay(
                    () => this.RevivePlant(crop),
                    120);
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void AnimateRevival(GameLocation loc, Vector2 tile)
    {
        // borrowing a Prarie King TAS for this.
        TemporaryAnimatedSprite? tas = new(
            textureName: Game1.mouseCursorsName,
            sourceRect: new Rectangle(464, 1792, 16, 16),
            animationInterval: 120f,
            animationLength: 5,
            numberOfLoops: 0,
            position: tile * 64f,
            flicker: false,
            flipped: Game1.random.NextDouble() < 0.5,
            layerDepth: 1f,
            alphaFade: 0.01f,
            color: Color.White,
            scale: Game1.pixelZoom,
            scaleChange: 0.01f,
            rotation: 0f,
            rotationChange: 0f)
        {
            light = true,
        };
        MultiplayerHelpers.GetMultiplayer().broadcastSprites(loc, tas);
    }
}
