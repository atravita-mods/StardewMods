// Ignore Spelling: Craftable loc Api Hoedirt LECLAIR

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore;
using AtraCore.Framework.Caches;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;

using GrowableGiantCrops.Framework.Assets;
using GrowableGiantCrops.Framework.InventoryModels;
using GrowableGiantCrops.HarmonyPatches.Compat;
using GrowableGiantCrops.HarmonyPatches.GrassPatches;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A shovel.
/// </summary>
[XmlType("Mods_atravita_Shovel")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Like methods are grouped together.")]
public class ShovelTool : Tool
{
    /// <summary>
    /// The mod-data string used to mark GiantCropTweaks protected hoedirt.
    /// </summary>
    protected internal const string LECLAIR_PROTECT = "leclair.giantcroptweaks/UnderCrop";

    /// <summary>
    /// The API instance.
    /// </summary>
    protected static readonly Api Api = new();

    #region delegates

    /// <summary>
    /// Gets the mine rock count on a specific mineshaft level.
    /// </summary>
    protected internal static readonly Lazy<Func<MineShaft, int>> MineRockCountGetter = new(() =>
        (typeof(MineShaft).GetCachedProperty("stonesLeftOnThisLevel", ReflectionCache.FlagTypes.InstanceFlags)
             .GetGetMethod(nonPublic: true) ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("stonesLeftOnThisLevelGetter"))
             .CreateDelegate<Func<MineShaft, int>>());

    /// <summary>
    /// Sets the mine rock count on a specific mineshaft level.
    /// </summary>
    protected internal static readonly Lazy<Action<MineShaft, int>> MineRockCountSetter = new(() =>
        (typeof(MineShaft).GetCachedProperty("stonesLeftOnThisLevel", ReflectionCache.FlagTypes.InstanceFlags)
             .GetSetMethod(nonPublic: true) ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("stonesLeftOnThisLevelSetter"))
             .CreateDelegate<Action<MineShaft, int>>());

    /// <summary>
    /// Gets whether or not a ladder has appeared on a specific mine level.
    /// </summary>
    protected static readonly Lazy<Func<MineShaft, bool>> HasLadderSpawnedGetter = new(() =>
        typeof(MineShaft).GetCachedField("ladderHasSpawned", ReflectionCache.FlagTypes.InstanceFlags)
             .GetInstanceFieldGetter<MineShaft, bool>());

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ShovelTool"/> class.
    /// </summary>
    public ShovelTool()
        : base(
            name: I18n.Shovel_Name(),
            upgradeLevel: 0,
            initialParentTileIndex: 0,
            indexOfMenuItemView: 0,
            stackable: false,
            numAttachmentSlots: 0)
    {
    }

    /// <inheritdoc />
    protected override Item GetOneNew() => new ShovelTool();

    #region functionality

    /// <inheritdoc />
    public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
    {
        // use the watering can arms.
        who.jitterStrength = 0.25f;
        who.FarmerSprite.setCurrentFrame(who.FacingDirection switch
        {
            Game1.down => 164,
            Game1.right => 172,
            Game1.up => 180,
            _ => 188,
        });
        this.Update(who.FacingDirection, 0, who);
        return false;
    }

    /// <inheritdoc />
    public override void endUsing(GameLocation location, Farmer who)
    {
        who.stopJittering();
        who.canReleaseTool = false;

        // use the watering can arms.
        int animation = who.FacingDirection switch
        {
            Game1.down => 164,
            Game1.right => 172,
            Game1.up => 180,
            _ => 188,
        };

        if (who.Sprite is not FarmerSprite sprite)
        {
            return;
        }

        sprite.animateOnce(whichAnimation: animation, animationInterval: 150f, numberOfFrames: 3);

        // for some reason, the watering can doesn't respect animationInterval.
        // We're just going to manually edit the animation timings if needed for Swift.
        // god this is bad.
        if (this.AnimationSpeedModifier >= 1f)
        {
            return;
        }

        lock (sprite.currentAnimation)
        {
            Span<FarmerSprite.AnimationFrame> asSpan = CollectionsMarshal.AsSpan(sprite.currentAnimation);
            for (int i = 0; i < asSpan.Length; i++)
            {
                ref FarmerSprite.AnimationFrame temp = ref asSpan[i];
                temp.milliseconds = (int)(temp.milliseconds * this.AnimationSpeedModifier);
            }
        }
    }

    /// <summary>
    /// Does the actual tool function.
    /// </summary>
    /// <param name="location">The game location.</param>
    /// <param name="x">pixel x.</param>
    /// <param name="y">pixel y.</param>
    /// <param name="power">The power level of the tool.</param>
    /// <param name="who">Last farmer to use.</param>
    public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
    {
        try
        {
            base.DoFunction(location, x, y, power, who);
            Vector2 pickupTile = new(x / Game1.tileSize, y / Game1.tileSize);

            if (LocationTileHandler.ApplyShovelToMap(this, who, location, pickupTile))
            {
                return;
            }

            location.performToolAction(this, x / Game1.tileSize, y / Game1.tileSize);

            // allow modders to block the shovel.
            if (location.doesTileHaveProperty(x, y, "atravita.ShovelForbidden", "Back") is string message)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    Game1.showRedMessage(I18n.FruitTree_Forbidden());
                }
                else if (message.TrySplitOnce(':', out ReadOnlySpan<char> first, out ReadOnlySpan<char> second))
                {
                    string name = first.Trim().ToString();
                    NPC? npc = NPCCache.GetByVillagerName(name);
                    if (npc is null)
                    {
                        try
                        {
                            npc = new NPC(
                                sprite: null,
                                position: Vector2.Zero,
                                defaultMap: string.Empty,
                                facingDirection: 0,
                                name,
                                datable: false,
                                portrait: Game1.temporaryContent.Load<Texture2D>("Portraits\\" + name));
                        }
                        catch (Exception ex)
                        {
                            ModEntry.ModMonitor.LogError($"creating NPC {name}", ex);
                            return;
                        }
                    }
                    Game1.DrawDialogue(new(npc, null, second.Trim().ToString()));
                }
                else
                {
                    Game1.drawObjectDialogue(message.Trim());
                }
                return;
            }

            GGCUtils.GetLargeObjectAtLocation(location, x, y, false)?.performToolAction(this, 0, pickupTile);

            int bigItemEnergy = this.IsEfficient ? 0 : ModEntry.Config.ShovelEnergy;

            // Handle bushes.
            if (ModEntry.GrowableBushesAPI?.TryPickUpBush(location, pickupTile, ModEntry.Config.PlacedOnly) is SObject bush)
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Picking up bush {bush.Name}", LogLevel.Info);
                this.GiveItemOrMakeDebris(location, who, bush);
                ModEntry.GrowableBushesAPI.DrawPickUpGraphics(bush, location, bush.TileLocation);
                who.Stamina -= bigItemEnergy;
                return;
            }

            // Handle clumps and giant crops.
            if (Api.TryPickUpClumpOrGiantCrop(location, pickupTile, ModEntry.Config.PlacedOnly) is SObject inventoryClump)
            {
                // Must remember to remove Khloe's protection markers.
                if (inventoryClump is InventoryGiantCrop crop)
                {
                    RemoveProtectionFromHoedirt(location, pickupTile, crop);
                }
                ModEntry.ModMonitor.DebugOnlyLog($"Picking up {inventoryClump.Name}.", LogLevel.Info);
                who.Stamina -= bigItemEnergy;
                this.GiveItemOrMakeDebris(location, who, inventoryClump);
                Api.DrawPickUpGraphics(inventoryClump, location, inventoryClump.TileLocation);
                return;
            }

            // for small things we take only one energy, at most.
            int smallItemEnergy = Math.Min(bigItemEnergy, 1);

            // objects go before terrain so tappers are removed before trees/fruit trees.
            if (location.objects.TryGetValue(pickupTile, out SObject? @object))
            {
                // special case terrain stuff.
                if (!@object.bigCraftable.Value && @object.GetType() == typeof(SObject))
                {
                    if (@object.ParentSheetIndex >= 0 &&
                        (@object.Name == "Stone" || @object.Name.Contains("Weeds")
                        || @object.Name.Contains("Twig") || @object.Name == "SupplyCrate"))
                    {
                        if (this.HandleTerrainObject(location, who, pickupTile, smallItemEnergy, @object))
                        {
                            return;
                        }
                    }
                }

                // special case, ignore indoor pots with stuff in them.
                if (@object is IndoorPot pot && (pot.hoeDirt?.Value?.crop is not null || pot.bush?.Value is not null))
                {
                    pot.shakeTimer = 100;
                    return;
                }

                // special case: shovel pushes full chests.
                if (@object is Chest chest && !chest.isEmpty() && chest.playerChest.Value)
                {
                    if (this.PushChest(location, who, pickupTile, smallItemEnergy, chest))
                    {
                        return;
                    }
                }

                // special cases: Mushroom boxes, slime balls
                if (@object.bigCraftable.Value && @object.GetType() == typeof(SObject))
                {
                    switch (@object.Name)
                    {
                        case "Mushroom Box":
                        {
                            if (this.HandleMushroomBox(location, who, pickupTile, smallItemEnergy, @object))
                            {
                                return;
                            }
                            break;
                        }
                        case "Slime Ball":
                        {
                            if (this.HandleBigCraftable(location, who, pickupTile, smallItemEnergy, @object))
                            {
                                @object.modData?.SetBool(SlimeProduceCompat.SlimeBall, true);
                                return;
                            }
                            break;
                        }
                        case "Slime Incubator":
                        {
                            if (this.HandleSlimeIncubator(location, who, pickupTile, smallItemEnergy, @object))
                            {
                                return;
                            }
                            break;
                        }
                        case "Boulder":
                        {
                            if (this.HandleBigCraftable(location, who, pickupTile, smallItemEnergy, @object))
                            {
                                @object.Fragility = SObject.fragility_Removable;
                                return;
                            }
                            break;
                        }
                    }
                }

                if (@object.performToolAction(this))
                {
                    who.Stamina -= smallItemEnergy;
                    if (FTMArtifactSpotPatch.IsBuriedItem?.Invoke(@object) != true)
                    {
                        this.GiveItemOrMakeDebris(location, who, @object);
                        AddAnimations(
                            loc: location,
                            tile: pickupTile,
                            texturePath: @object.bigCraftable.Value ? Game1.bigCraftableSpriteSheetName : Game1.objectSpriteSheetName,
                            sourceRect: @object.bigCraftable.Value ? SObject.getSourceRectForBigCraftable(@object.ParentSheetIndex) : GameLocation.getSourceRectForObject(@object.ParentSheetIndex),
                            new Point(1, 1));
                    }
                    location.objects.Remove(pickupTile);
                    return;
                }
            }

            if (location.terrainFeatures.TryGetValue(pickupTile, out TerrainFeature? terrain))
            {
                // block subclasses like Cosmetic Plant, which currently cannot be safely moved.
                if (terrain is Grass grass &&
                    (terrain.GetType() == typeof(Grass) || SObjectPatches.IsMoreGrassGrass?.Invoke(grass) == true))
                {
                    if (this.HandleGrass(location, who, pickupTile, smallItemEnergy, grass))
                    {
                        return;
                    }
                }

                if (terrain is Tree tree && terrain.GetType() == typeof(Tree))
                {
                    int effectiveStage = Math.Clamp(tree.growthStage.Value, 0, 5);
                    if (effectiveStage == 4)
                    {
                        effectiveStage = 3;
                    }
                    if (effectiveStage <= ModEntry.Config.MaxTreeStageInternal && this.HandleTree(location, who, pickupTile, bigItemEnergy, tree))
                    {
                        return;
                    }
                    InventoryTree.TreeShakeMethod(tree, pickupTile, true, location);
                }

                if (terrain is FruitTree fruitTree && terrain.GetType() == typeof(FruitTree))
                {
                    // this is for East Scarp. We'll prevent people from stealing their trees.
                    // also the ones at the deep woods entry.
                    if (fruitTree.modData?.ContainsKey(InventoryFruitTree.ModDataKey) != true
                        && (location.doesTileHaveProperty(x, y, "FruitTree", "Back") is not null
                            || location.NameOrUniqueName == "DeepWoods"
                            || location.NameOrUniqueName == "Custom_Ridgeside_RidgesideVillage"))
                    {
                        Game1.showRedMessage(I18n.FruitTree_Forbidden());
                        fruitTree.shake(pickupTile, true);
                        return;
                    }

                    if (fruitTree.growthStage.Value <= ModEntry.Config.MaxFruitTreeStageInternal && this.HandleFruitTree(location, who, pickupTile, bigItemEnergy))
                    {
                        return;
                    }
                    fruitTree.shake(pickupTile, true);
                }

                if (terrain.performToolAction(this, 0, pickupTile))
                {
                    who.Stamina -= smallItemEnergy;
                    location.terrainFeatures.Remove(pickupTile);
                    return;
                }
            }

            if (location.CanCreateHoedirtAt(pickupTile))
            {
                this.MakeHoeDirt(location, who, pickupTile, smallItemEnergy);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("using shovel", ex);
        }
    }

    /// <inheritdoc />
    public override bool onRelease(GameLocation location, int x, int y, Farmer who) => false;

    #endregion

    #region display

    /// <inheritdoc />
    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        spriteBatch.Draw(
            texture: this.GetTexture(),
            position: location + new Vector2(32f, 32f),
            new Rectangle(96, 16, 16, 16),
            color: color * transparency,
            rotation: 0f,
            new Vector2(8f, 8f),
            scale: Game1.pixelZoom * scaleSize,
            effects: SpriteEffects.None,
            layerDepth);
    }

    /// <inheritdoc />
    protected override string loadDisplayName() => I18n.Shovel_Name();

    /// <inheritdoc />
    protected override string loadDescription() => I18n.Shovel_Description();

    /// <summary>
    /// Gets the texture for the tool.
    /// </summary>
    /// <returns>Texture.</returns>
    public virtual Texture2D GetTexture() => AssetManager.ToolTexture;

    #endregion

    #region misc

    /// <inheritdoc />
    /// <remarks>disallow stacking.</remarks>
    public override int maximumStackSize() => -1;

    /// <inheritdoc />
    /// <remarks>nop this.</remarks>
    public override void actionWhenClaimed()
    {
    }

    /// <inheritdoc />
    public override bool actionWhenPurchased(string shopId) => false;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override int attachmentSlots() => 0;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override bool canThisBeAttached(SObject o) => false;

    /// <inheritdoc />
    /// <remarks>forbid attachments.</remarks>
    public override SObject attach(SObject o) => o;

    /// <inheritdoc />
    /// <remarks>Always allow the player to trash this if necessary.</remarks>
    public override bool canBeTrashed() => true;

    #endregion

    #region helpers

    /// <summary>
    /// Removes the protection markers from under a giant crop.
    /// </summary>
    /// <param name="location">Game location to grab from.</param>
    /// <param name="pickupTile">Tile to pick up from.</param>
    /// <param name="crop">Crop grabbed.</param>
    /// <remarks>Kept alone as a separate method in case other mods need to patch it.</remarks>
    /// <returns>The number of protection markers removed.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    protected static int RemoveProtectionFromHoedirt(GameLocation location, Vector2 pickupTile, InventoryGiantCrop crop)
    {
        Point size = crop.TileSize;
        int start_x = (int)pickupTile.X;
        int start_y = (int)pickupTile.Y;
        int count = 0;
        for (int hx = start_x; hx < start_x + size.X; hx++)
        {
            for (int hy = start_y; hy < start_y + size.Y; hy++)
            {
                if (location.terrainFeatures.TryGetValue(new(hx, hy), out TerrainFeature? terrainFeature) && terrainFeature is HoeDirt hoeDirt)
                {
                    if (hoeDirt.modData?.Remove(LECLAIR_PROTECT) == true)
                    {
                        count++;
                    }
                }
            }
        }
        if (count > 0)
        {
            ModEntry.ModMonitor.Log($"Removed {count} Giant Crop Tweaks protection markers for {crop.Name}");
        }

        return count;
    }

    /// <inheritdoc cref="IGrowableGiantCropsAPI.DrawAnimations(GameLocation, Vector2, string?, Rectangle, Point)"/>
    protected internal static void AddAnimations(GameLocation loc, Vector2 tile, string? texturePath, Rectangle sourceRect, Point tileSize, Color? color = null)
    {
        if (texturePath is null)
        {
            return;
        }

        Multiplayer mp = Game1.Multiplayer;

        float deltaY = -50f - (sourceRect.Height * 2);
        const float gravity = 0.0025f;

        float velocity = -0.7f - MathF.Sqrt(2 * 60f * gravity);
        float time = (MathF.Sqrt((velocity * velocity) - (gravity * deltaY * 2f)) / gravity) - (velocity / gravity);

        Vector2 landingPos = new Vector2(tile.X + (tileSize.X / 2f) - 1, tile.Y + tileSize.Y - 1) * 64f;

        TemporaryAnimatedSprite objTas = new(
            textureName: texturePath,
            sourceRect: sourceRect,
            position: tile * 64f,
            flipped: false,
            alphaFade: 0f,
            color: color ?? Color.White)
        {
            totalNumberOfLoops = 1,
            interval = time,
            acceleration = new Vector2(0f, gravity),
            motion = new Vector2(0f, velocity),
            scale = Game1.pixelZoom,
            timeBasedMotion = true,
            rotation = 0.1f,
            rotationChange = 0.1f,
            scaleChange = -0.0015f * (Math.Max(3, tileSize.X) / 3),
            layerDepth = (landingPos.Y + 32f) / 10000f,
        };

        TemporaryAnimatedSprite? dustTas = new(
            textureName: Game1.mouseCursorsName,
            sourceRect: new Rectangle(464, 1792, 16, 16),
            animationInterval: 120f,
            animationLength: 5,
            numberOfLoops: 0,
            position: landingPos,
            flicker: false,
            flipped: Random.Shared.OfChance(0.5),
            layerDepth: (landingPos.Y + 40f) / 10000f,
            alphaFade: 0.01f,
            color: Color.White,
            scale: Game1.pixelZoom,
            scaleChange: 0.02f,
            rotation: 0f,
            rotationChange: 0f)
        {
            light = true,
            delayBeforeAnimationStart = Math.Max((int)time - 10, 0),
        };

        int damage = (sourceRect.Height / 16) * (sourceRect.Width / 16);

        // if you somehow manage to hit a monster with the animation.....
        if (ModEntry.Config.ShovelDoesDamage)
        {
            DelayedAction.functionAfterDelay(
                () => loc.damageMonster(new Rectangle((int)landingPos.X, (int)landingPos.Y, 64, 64), damage, damage * 3, false, Game1.player),
                (int)time);
        }

        mp.broadcastSprites(loc, objTas, dustTas);
    }

    /// <summary>
    /// Tries to add an item to the player's inventory, dropping it at their feet if we can't.
    /// </summary>
    /// <param name="location">relevant location.</param>
    /// <param name="who">farmer to add to.</param>
    /// <param name="item">item to add.</param>
    protected virtual void GiveItemOrMakeDebris(GameLocation location, Farmer who, Item item)
    {
        if (!who.addItemToInventoryBool(item))
        {
            location.debris.Add(new Debris(item, who.Position));
        }
    }

    /// <summary>
    /// Handles a terrain object.
    /// </summary>
    /// <param name="location">GameLocation.</param>
    /// <param name="who">Farmer.</param>
    /// <param name="pickupTile">Tile picked up from.</param>
    /// <param name="energy">Energy used.</param>
    /// <param name="object">The object affected.</param>
    /// <returns>True if handled, false otherwise.</returns>
    protected virtual bool HandleTerrainObject(GameLocation location, Farmer who, Vector2 pickupTile, int energy, SObject @object)
    {
        who.Stamina -= energy;
        this.GiveItemOrMakeDebris(location, who, @object);
        AddAnimations(
            loc: location,
            tile: pickupTile,
            texturePath: Game1.objectSpriteSheetName,
            sourceRect: GameLocation.getSourceRectForObject(@object.ParentSheetIndex),
            new Point(1, 1));
        location.Objects.Remove(pickupTile);

        if (location is MineShaft shaft && @object.Name == "Stone")
        {
            int stonesLeft = MineRockCountGetter.Value(shaft);
            stonesLeft--;
            ModEntry.ModMonitor.DebugOnlyLog($"{stonesLeft} stones left on floor {shaft.mineLevel}", LogLevel.Info);
            if (stonesLeft <= 0 && !HasLadderSpawnedGetter.Value(shaft))
            {
                ModEntry.ModMonitor.DebugOnlyLog($"Last rock on {shaft.mineLevel}, creating ladder.", LogLevel.Info);
                shaft.createLadderDown((int)pickupTile.X, (int)pickupTile.Y);
            }
            MineRockCountSetter.Value(shaft, stonesLeft);
        }
        return true;
    }

    /// <summary>
    /// Handles a grass.
    /// </summary>
    /// <param name="location">the game location.</param>
    /// <param name="who">the relevant farmer.</param>
    /// <param name="pickupTile">the tile picked up from.</param>
    /// <param name="energy">how much energy to take.</param>
    /// <param name="grass">The grass instance.</param>
    /// <returns>True if handled, false otherwise.</returns>
    protected virtual bool HandleGrass(GameLocation location, Farmer who, Vector2 pickupTile, int energy, Grass grass)
    {
        who.Stamina -= energy;
        SObject starter = Api.GetMatchingStarter(grass);
        this.GiveItemOrMakeDebris(location, who, starter);

        ParsedItemData? grassData = ItemRegistry.GetData(SObjectPatches.GrassStarterQualId);
        if (grassData is not null)
        {
            AddAnimations(
                loc: location,
                tile: pickupTile,
                texturePath: grassData.GetTextureName(),
                sourceRect: grassData.GetSourceRect(),
                new Point(1, 1));
        }
        location.terrainFeatures.Remove(pickupTile);

        return true;
    }

    /// <summary>
    /// Pushes a chest back.
    /// </summary>
    /// <param name="location">Game location.</param>
    /// <param name="who">Farmer.</param>
    /// <param name="pickupTile">Tile affected.</param>
    /// <param name="energy">Amount of energy to use.</param>
    /// <param name="chest">Chest to move.</param>
    /// <returns>True if successful, false if nothing happened.</returns>
    protected virtual bool PushChest(GameLocation location, Farmer who, Vector2 pickupTile, int energy, Chest chest)
    {
        // skip marketday chests
        if (chest.modData?.ContainsKey("ceruleandeep.MarketDay/GrangeDisplay") == true)
        {
            return false;
        }
        chest.GetMutex().RequestLock(
            acquired: () =>
            {
                location.playSound("hammer");
                chest.shakeTimer = 100;
                if (chest.TileLocation.X == 0f && chest.TileLocation.Y == 0f && location.getObjectAtTile((int)pickupTile.X, (int)pickupTile.Y) == chest)
                {
                    chest.TileLocation = pickupTile;
                }
                chest.TryMoveToSafePosition(who.FacingDirection);
                who.Stamina -= energy;
                return;
            },
            failed: () => ModEntry.ModMonitor.Log($"Chest at {chest.TileLocation}: lock not acquired, skipping"));

        return true;
    }

    /// <summary>
    /// Handles picking up a mushroom box.
    /// </summary>
    /// <param name="location">Location to pick up the mushroom box from.</param>
    /// <param name="who">The farmer to use.</param>
    /// <param name="pickupTile">The tile to pick up from.</param>
    /// <param name="energy">The amount of energy to take.</param>
    /// <param name="object">The actual instance to pick up.</param>
    /// <returns>True if handled, false otherwise.</returns>
    protected virtual bool HandleMushroomBox(GameLocation location, Farmer who, Vector2 pickupTile, int energy, SObject @object)
    {
        if (@object.readyForHarvest.Value)
        {
            location.debris.Add(new Debris(@object.heldObject.Value, who.Position));
        }
        @object.Fragility = SObject.fragility_Removable;
        return this.HandleBigCraftable(location, who, pickupTile, energy, @object);
    }

    /// <summary>
    /// Handles picking up a slime incubator.
    /// </summary>
    /// <param name="location">Game location to pick up from.</param>
    /// <param name="who">Farmer doing the pickup.</param>
    /// <param name="pickupTile">Tile to pick up from.</param>
    /// <param name="energy">Amount of energy to deduct.</param>
    /// <param name="object">The object instance to pick up.</param>
    /// <returns>True if handled, false otherwise.</returns>
    protected virtual bool HandleSlimeIncubator(GameLocation location, Farmer who, Vector2 pickupTile, int energy, SObject @object)
    {
        if (@object.MinutesUntilReady <= 0)
        {
            location.debris.Add(new Debris(@object.heldObject.Value, who.Position));
        }
        @object.Fragility = SObject.fragility_Removable;
        return this.HandleBigCraftable(location, who, pickupTile, energy, @object);
    }

#warning - 1.6 has nice methods for this.

    /// <summary>
    /// Handles picking up a big craftable.
    /// </summary>
    /// <param name="location">The game location to pick up from.</param>
    /// <param name="who">The farmer doing pickup.</param>
    /// <param name="pickupTile">The tile being picked up from.</param>
    /// <param name="energy">The amount of energy to deduct.</param>
    /// <param name="object">The object to pick up.</param>
    /// <param name="idx">The parent sheet index to set to.</param>
    /// <returns>true if handled, false otherwise.</returns>
    protected virtual bool HandleBigCraftable(GameLocation location, Farmer who, Vector2 pickupTile, int energy, SObject @object)
    {
        who.Stamina -= energy;
        @object.ResetParentSheetIndex();
        @object.heldObject.Value = null;
        @object.performRemoveAction();
        this.GiveItemOrMakeDebris(location, who, @object);

        ParsedItemData? data = ItemRegistry.GetData(@object.QualifiedItemId);
        if (data is not null)
        {
            AddAnimations(
                loc: location,
                tile: pickupTile - Vector2.UnitY,
                texturePath: data.GetTextureName(),
                sourceRect: data.GetSourceRect(),
                new Point(1, 2),
                color: SlimeProduceCompat.ReplaceDrawColorForSlimeEgg(Color.White, @object));
        }
        location.objects.Remove(pickupTile);

        return true;
    }

    /// <summary>
    /// Handles picking up a tree.
    /// </summary>
    /// <param name="location">The game location to pick up from.</param>
    /// <param name="who">The farmer doing the picking up.</param>
    /// <param name="pickupTile">The tile from which pickup is done.</param>
    /// <param name="energy">How much energy to subtract from the player.</param>
    /// <param name="tree">The tree instance.</param>
    /// <returns>True if handled, false otherwise.</returns>
    protected virtual bool HandleTree(GameLocation location, Farmer who, Vector2 pickupTile, int energy, Tree tree)
    {
#warning - ahhhh trees
        if (tree.growthStage.Value == 0)
        {
            who.Stamina -= energy;
            location.playSound("woodyHit");
            location.playSound("axchop");
            InventoryTree.SeedDestoryMethod(tree, this, pickupTile, location);
            location.terrainFeatures.Remove(pickupTile);
            return true;
        }
        else if (Api.TryPickUpTree(location, pickupTile, ModEntry.Config.PlacedOnly) is InventoryTree inventoryTree)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Picking up {inventoryTree.Name}.", LogLevel.Info);
            who.Stamina -= ModEntry.Config.ShovelEnergy;
            this.GiveItemOrMakeDebris(location, who, inventoryTree);
            Api.DrawPickUpGraphics(inventoryTree, location, inventoryTree.TileLocation);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles removing a fruit tree.
    /// </summary>
    /// <param name="location">The game location to grab the fruit tree from.</param>
    /// <param name="who">The farmer in question.</param>
    /// <param name="pickupTile">The tile to pick up from.</param>
    /// <param name="energy">The amount of energy to deduct.</param>
    /// <returns>True if handled, false otherwise.</returns>
    protected virtual bool HandleFruitTree(GameLocation location, Farmer who, Vector2 pickupTile, int energy)
    {
        if (Api.TryPickUpFruitTree(location, pickupTile, ModEntry.Config.PlacedOnly) is InventoryFruitTree inventoryFruitTree)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"Picking up {inventoryFruitTree.Name}.", LogLevel.Info);
            who.Stamina -= energy;
            this.GiveItemOrMakeDebris(location, who, inventoryFruitTree);
            Api.DrawPickUpGraphics(inventoryFruitTree, location, inventoryFruitTree.TileLocation);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Makes a hoe dirt at a specific location.
    /// </summary>
    /// <param name="location">Game location to make the hoedirt.</param>
    /// <param name="who">Farmer who is doing the action.</param>
    /// <param name="pickupTile">Tile to make the hoedirt.</param>
    /// <param name="energy">Amount of energy to take.</param>
    /// <returns>true if handled, false otherwise.</returns>
    protected virtual bool MakeHoeDirt(GameLocation location, Farmer who, Vector2 pickupTile, int energy)
    {
        who.Stamina -= energy;
        location.makeHoeDirt(pickupTile, ignoreChecks: false);
        location.playSound("hoeHit");
        location.checkForBuriedItem((int)pickupTile.X, (int)pickupTile.Y, explosion: false, detectOnly: false, who);
        Game1.Multiplayer.broadcastSprites(location, new TemporaryAnimatedSprite(
            rowInAnimationTexture: 12,
            position: pickupTile * Game1.tileSize,
            color: Color.White,
            animationLength: 8,
            flipped: Random.Shared.OfChance(0.5),
            animationInterval: 50f));

        return true;
    }

    #endregion
}
