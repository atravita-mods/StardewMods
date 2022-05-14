using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.Framework;

/// <summary>
/// Class that handles placement of special fertilizers.
/// </summary>
public sealed class CanPlaceHandler : IMoreFertilizersAPI
{
    /// <summary>
    /// ModData string for the organic fertilizer.
    /// </summary>
    public const string Organic = "atravita.MoreFertilizer.Organic";

    /// <summary>
    /// ModData string for the Fruit Tree Fertilizers.
    /// </summary>
    public const string FruitTreeFertilizer = "atravita.MoreFertilizer.FruitTree";

    /// <summary>
    /// ModData string for the Fish Food fertilizers.
    /// </summary>
    public const string FishFood = "atravita.MoreFertilizer.FishFood";

    /// <summary>
    /// ModData string for the Domesticated Fish Food.
    /// </summary>
    public const string DomesticatedFishFood = "atravita.MoreFertilizer.DomesticatedFishFood";

    /// <summary>
    /// ModData string for joja crops.
    /// </summary>
    public const string Joja = "atravita.MoreFertilizer.Joja";

    /// <summary>
    /// ModData string to track trees fertilized with tree fertilizers.
    /// </summary>
    public const string TreeFertilizer = "atravita.MoreFertilizer.TreeFertilizer";

    /// <summary>
    /// ModData string to track trees fertilized with the tree tapper fertilizer.
    /// </summary>
    public const string TreeTapperFertilizer = "atravita.MoreFertilizer.TreeTapper";

    /// <summary>
    /// ModData string for the Bountiful Bush fertilizer.
    /// </summary>
    public const string BountifulBush = "atravita.MoreFertilizer.BountifulBush";

    /// <summary>
    /// ModData string for the Rapid Bush fertilizer.
    /// </summary>
    public const string RapidBush = "atravita.MoreFertilizer.RapidBush";

    /// <summary>
    /// ModData string marking fertilized mushroom boxen.
    /// </summary>
    public const string MushroomFertilizer = "atravita.MoreFertilizer.MushroomFertilizer";

    /// <summary>
    /// ModData string marking miraculous beverages.
    /// </summary>
    public const string MiraculousBeverages = "atravita.MoreFertilizer.MiraculousBeverages";

    /// <inheritdoc />
    public bool CanPlaceFertilizer(SObject obj, GameLocation loc, Vector2 tile)
    {
        if (obj.ParentSheetIndex == -1 || obj.bigCraftable.Value || Utility.isPlacementForbiddenHere(loc) || !Context.IsPlayerFree)
        {
            return false;
        }

        if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain))
        {
            if (terrain is FruitTree fruitTree
                && (obj.ParentSheetIndex == ModEntry.FruitTreeFertilizerID || obj.ParentSheetIndex == ModEntry.DeluxeFruitTreeFertilizerID
                || obj.ParentSheetIndex == ModEntry.MiraculousBeveragesID))
            {
                return !fruitTree.modData.ContainsKey(FruitTreeFertilizer) && !fruitTree.modData.ContainsKey(MiraculousBeverages);
            }
            else if (terrain is Bush bush && (bush.size.Value == Bush.greenTeaBush || bush.size.Value == Bush.mediumBush) && !bush.townBush.Value
                && ((obj.ParentSheetIndex == ModEntry.RapidBushFertilizerID && bush.size.Value == Bush.greenTeaBush) || obj.ParentSheetIndex == ModEntry.BountifulBushID
                    || (obj.ParentSheetIndex == ModEntry.MiraculousBeveragesID && bush.size.Value == Bush.greenTeaBush)))
            {
                return !bush.modData.ContainsKey(BountifulBush) && !bush.modData.ContainsKey(RapidBush) && !bush.modData.ContainsKey(MiraculousBeverages);
            }
            else if (terrain is Tree tree
                && (obj.ParentSheetIndex == ModEntry.TreeTapperFertilizerID))
            {
                return !tree.modData.ContainsKey(TreeFertilizer) && !tree.modData.ContainsKey(TreeTapperFertilizer);
            }
        }

        if (loc.Objects.TryGetValue(tile, out SObject @object) && @object is IndoorPot pot && pot.bush?.Value is Bush pottedBush && pottedBush.size?.Value == Bush.greenTeaBush
            && (obj.ParentSheetIndex == ModEntry.RapidBushFertilizerID || obj.ParentSheetIndex == ModEntry.BountifulBushID || obj.ParentSheetIndex == ModEntry.MiraculousBeveragesID ))
        {
            return !pottedBush.modData.ContainsKey(BountifulBush) && !pottedBush.modData.ContainsKey(RapidBush) && !pottedBush.modData.ContainsKey(MiraculousBeverages);
        }

        if (obj.ParentSheetIndex == ModEntry.BountifulBushID)
        {
            Rectangle pos = new((int)tile.X * 64, (int)tile.Y * 64, 16, 16);
            foreach (LargeTerrainFeature largeterrainfeature in loc.largeTerrainFeatures)
            {
                if (largeterrainfeature is Bush bigBush && !bigBush.townBush.Value && bigBush.getBoundingBox().Intersects(pos))
                {
                    return !bigBush.modData.ContainsKey(BountifulBush) && !bigBush.modData.ContainsKey(RapidBush) && !bigBush.modData.ContainsKey(MiraculousBeverages);
                }
            }
        }

        if (loc.canFishHere() && loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") is not null
            && (obj.ParentSheetIndex == ModEntry.FishFoodID || obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID))
        {
            return !loc.modData.ContainsKey(FishFood);
        }

        if(loc is BuildableGameLocation buildableLoc && obj.ParentSheetIndex == ModEntry.DomesticatedFishFoodID)
        {
            foreach(Building b in buildableLoc.buildings)
            {
                if (b is FishPond && b.occupiesTile(tile))
                {
                    return !b.modData.ContainsKey(DomesticatedFishFood);
                }
            }
        }
        return false;
    }

    /// <inheritdoc />
    public bool TryPlaceFertilizer(SObject obj, GameLocation loc, Vector2 tile)
    {
        if (!this.CanPlaceFertilizer(obj, loc, tile))
        {
            return false;
        }
        if (obj.ParentSheetIndex == ModEntry.FishFoodID || obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID)
        {
            loc.modData?.SetInt(FishFood, obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID ? 3 : 1);
            if (loc is MineShaft or VolcanoDungeon)
            {
                FishFoodHandler.UnsavedLocHandler.FishFoodLocationMap[Game1.currentLocation.NameOrUniqueName] = obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID ? 3 : 1;
                FishFoodHandler.BroadcastHandler(ModEntry.MultiplayerHelper);
            }
            return true;
        }
        if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain))
        {
            if (terrain is FruitTree fruitTree)
            {
                if (obj.ParentSheetIndex == ModEntry.FruitTreeFertilizerID || obj.ParentSheetIndex == ModEntry.DeluxeFruitTreeFertilizerID)
                {
                    fruitTree.modData?.SetInt(FruitTreeFertilizer, obj.ParentSheetIndex == ModEntry.DeluxeFruitTreeFertilizerID ? 2 : 1);
                    return true;
                }
                if (obj.ParentSheetIndex == ModEntry.MiraculousBeveragesID)
                {
                    fruitTree.modData?.SetBool(MiraculousBeverages, true);
                    return true;
                }
            }
            if (terrain is Bush bush)
            {
                if (obj.ParentSheetIndex == ModEntry.RapidBushFertilizerID && bush.size.Value == Bush.greenTeaBush)
                {
                    bush.modData?.SetBool(RapidBush, true);
                    return true;
                }
                else if (obj.ParentSheetIndex == ModEntry.BountifulBushID)
                {
                    bush.modData?.SetBool(BountifulBush, true);
                    return true;
                }
                else if (obj.ParentSheetIndex == ModEntry.MiraculousBeveragesID && bush.size.Value == Bush.greenTeaBush)
                {
                    bush.modData?.SetBool(MiraculousBeverages, true);
                    return true;
                }
            }
            if (terrain is Tree tree
                && (obj.ParentSheetIndex == ModEntry.TreeTapperFertilizerID))
            {
                tree.modData?.SetBool(TreeTapperFertilizer, true);
                return true;
            }
        }

        if (loc.Objects.TryGetValue(tile, out SObject @object) && @object is IndoorPot pot && pot.bush?.Value is Bush pottedBush && pottedBush.size?.Value == Bush.greenTeaBush)
        {
            if (obj.ParentSheetIndex == ModEntry.BountifulBushID)
            {
                pottedBush.modData?.SetBool(BountifulBush, true);
                return true;
            }
            if (obj.ParentSheetIndex == ModEntry.RapidBushFertilizerID)
            {
                pottedBush.modData?.SetBool(RapidBush, true);
                return true;
            }
            if (obj.ParentSheetIndex == ModEntry.MiraculousBeveragesID)
            {
                pottedBush.modData?.SetBool(MiraculousBeverages, true);
                return true;
            }
        }

        if (obj.ParentSheetIndex == ModEntry.BountifulBushID)
        {
            Rectangle pos = new((int)tile.X * 64, (int)tile.Y * 64, 16, 16);
            foreach (LargeTerrainFeature largeterrainfeature in loc.largeTerrainFeatures)
            {
                if (largeterrainfeature is Bush bigBush && bigBush.size.Value == Bush.mediumBush && bigBush.getBoundingBox().Intersects(pos))
                {
                    bigBush.modData?.SetBool(BountifulBush, true);
                    return true;
                }
            }
        }

        if (loc is BuildableGameLocation buildableLoc && obj.ParentSheetIndex == ModEntry.DomesticatedFishFoodID)
        {
            foreach (Building b in buildableLoc.buildings)
            {
                if (b is FishPond && b.occupiesTile(tile))
                {
                    b.modData?.SetBool(DomesticatedFishFood, true);
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void AnimateFertilizer(StardewValley.Object obj, GameLocation loc, Vector2 tile)
    {
        if (obj.ParentSheetIndex == ModEntry.FishFoodID || obj.ParentSheetIndex == ModEntry.DeluxeFishFoodID || obj.ParentSheetIndex == ModEntry.DomesticatedFishFoodID)
        {
            Vector2 placementtile = (tile * 64f) + new Vector2(32f, 32f);
            if (obj.ParentSheetIndex == ModEntry.DomesticatedFishFoodID && Game1.currentLocation is BuildableGameLocation buildable)
            {
                foreach (Building b in buildable.buildings)
                {
                    if (b is FishPond pond && b.occupiesTile(tile))
                    {
                        placementtile = pond.GetCenterTile() * 64f;
                        break;
                    }
                }
            }

            Game1.playSound("throwDownITem");

            float deltaY = -140f;
            float gravity = 0.0025f;
            float velocity = -0.08f - MathF.Sqrt(2 * 60f * gravity);
            float time = (MathF.Sqrt((velocity * velocity) - (gravity * deltaY * 2f)) / gravity) - (velocity / gravity);

            Multiplayer mp = MultiplayerHelpers.GetMultiplayer();
            mp.broadcastSprites(
                Game1.currentLocation,
                new TemporaryAnimatedSprite(
                    textureName: Game1.objectSpriteSheetName,
                    sourceRect: Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16),
                    position: placementtile + new Vector2(0, deltaY),
                    flipped: false,
                    alphaFade: 0f,
                    color: Color.White)
                {
                    scale = Game1.pixelZoom,
                    layerDepth = 1f,
                    totalNumberOfLoops = 1,
                    interval = time,
                    acceleration = new Vector2(0f, gravity),
                    motion = new Vector2(0f, velocity),
                    timeBasedMotion = true,
                });

            GameLocationUtils.DrawWaterSplash(Game1.currentLocation, placementtile, mp, (int)time);
            DelayedAction.playSoundAfterDelay("waterSlosh", (int)time, Game1.player.currentLocation);
            if (obj.ParentSheetIndex != ModEntry.DomesticatedFishFoodID)
            {
                DelayedAction.functionAfterDelay(
                    () => Game1.currentLocation.waterColor.Value = ModEntry.Config.WaterOverlayColor,
                    (int)time);
            }
        }
    }
}