// Ignore Spelling: Api loc

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

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

    private const string FAIRY_DUST = "(O)872";

    private static readonly Lazy<Action<FruitTree, float>> ShakeTimerSetter = new(
    () => typeof(FruitTree)
        .GetCachedField("shakeTimer", ReflectionCache.FlagTypes.InstanceFlags)
        .GetInstanceFieldSetter<FruitTree, float>());

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:Static readonly fields should begin with upper-case letter", Justification = "Reviewed.")]
    private static readonly ReviveDeadCropsApi instance = new();

    /// <summary>
    /// Gets the API instance for this mod.
    /// </summary>
    internal static ReviveDeadCropsApi Instance { get; } = instance;

    /// <summary>
    /// Gets or sets a value indicating whether or not any crops have been revived.
    /// </summary>
    internal bool Changed { get; set; } = false;

    /// <inheritdoc />
    public bool CanApplyDust(GameLocation loc, Vector2 tile, SObject obj)
    {
        if (obj.QualifiedItemId != FAIRY_DUST)
        {
            return false;
        }
        if (loc.GetHoeDirtAtTile(tile) is HoeDirt dirt)
        {
            return dirt.crop?.dead?.Value == true;
        }
        if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain)
            && terrain is FruitTree tree)
        {
            return tree.stump.Value || tree.struckByLightningCountdown.Value > 0;
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
                this.Changed = true;
                return true;
            }

            if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain)
                && terrain is FruitTree tree)
            {
                this.AnimateRevival(loc, tile);
                DelayedAction.functionAfterDelay(
                    () => this.ReviveFruitTree(tree),
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

        // make sure that the raised value is set again.
        if (crop.GetData()?.IsRaised == true)
        {
            crop.raisedSeeds.Value = true;
        }

        crop.growCompletely();
    }

    /// <inheritdoc />
    public void ReviveFruitTree(FruitTree tree)
    {
        if (tree.struckByLightningCountdown.Value > 0)
        {
            tree.performUseAction(tree.Tile);
            tree.struckByLightningCountdown.Value = 0;
        }
        if (tree.stump.Value)
        {
            tree.stump.Value = false;
        }
        ShakeTimerSetter.Value(tree, 100f);
        tree.shakeLeft.Value = true;
        tree.health.Value = 10f;
    }

    /// <inheritdoc />
    public void AnimateRevival(GameLocation loc, Vector2 tile)
    {
        // borrowing a Prairie King TAS for this.
        TemporaryAnimatedSprite? tas = new(
            textureName: Game1.mouseCursorsName,
            sourceRect: new Rectangle(464, 1792, 16, 16),
            animationInterval: 120f,
            animationLength: 5,
            numberOfLoops: 0,
            position: tile * 64f,
            flicker: false,
            flipped: Random.Shared.OfChance(0.5),
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
        Game1.Multiplayer.broadcastSprites(loc, tas);
    }
}
