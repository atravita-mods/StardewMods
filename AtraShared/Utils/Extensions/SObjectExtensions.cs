// Ignore Spelling: Craftable Impl loc

using AtraBase.Toolkit.Extensions;

using AtraShared.Wrappers;

using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;

namespace AtraShared.Utils.Extensions;

/// <summary>
/// Extensions for SObject.
/// </summary>
public static class SObjectExtensions
{

    /// <summary>
    /// Creates a TAS that represents a parabolic arc.
    /// </summary>
    /// <param name="obj">Object to throw.</param>
    /// <param name="start">Start location.</param>
    /// <param name="end">End location.</param>
    /// <param name="mp">Multiplayer instance.</param>
    /// <param name="loc">GameLocation.</param>
    /// <returns>Total time the parabolic arc will take.</returns>
    public static float ParabolicThrowItem(this SObject obj, Vector2 start, Vector2 end, Multiplayer mp, GameLocation loc)
    {
        const float gravity = 0.0025f;

        float velocity = -0.08f;
        Vector2 delta = end - start;
        if (delta.Y < 40)
        {
            // Ensure the initial velocity is sufficiently fast to make it all the way up.
            velocity -= MathF.Sqrt(2 * MathF.Abs(delta.Y + 80) * gravity);
        }
        float time = (MathF.Sqrt(Math.Max((velocity * velocity) + (gravity * delta.Y * 2f), 0)) / gravity) - (velocity / gravity);

        ParsedItemData parsedItemDefintion = ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId);
        mp.broadcastSprites(
            loc,
            new TemporaryAnimatedSprite(
                textureName: parsedItemDefintion.GetTextureName(),
                sourceRect: parsedItemDefintion.GetSourceRect(0, obj.ParentSheetIndex),
                position: start,
                flipped: false,
                alphaFade: 0f,
                color: Color.White)
            {
                scale = Game1.pixelZoom,
                layerDepth = 1f,
                totalNumberOfLoops = 1,
                interval = time,
                acceleration = new Vector2(0f, gravity),
                motion = new Vector2(delta.X / time, velocity),
                timeBasedMotion = true,
            });
        return time;
    }

    /// <summary>
    /// Gets whether or not an SObject is a trash item.
    /// </summary>
    /// <param name="obj">SObject to check.</param>
    /// <returns>true if it's a trash item, false otherwise.</returns>
    public static bool IsTrashItem(this SObject obj)
        => obj.HasTypeObject() && Utility.IsLegacyIdBetween(obj.ItemId, 168, 172);

    /// <summary>
    /// Gets whether or not an SObject is a bomb.
    /// </summary>
    /// <param name="obj">SObject to check.</param>
    /// <returns>true if it's a bomb, false otherwise.</returns>
    public static bool IsBomb(this SObject obj)
        => obj.HasTypeObject() && Utility.IsLegacyIdBetween(obj.ItemId, 286, 288);

    /// <summary>
    /// Returns true for an item that would be considered alcohol. Taken from <see cref="SObject.GetFoodOrDrinkBuffs"/>.
    /// </summary>
    /// <param name="obj">SObject.</param>
    /// <returns>True if alcohol.</returns>
    public static bool IsAlcoholItem(this SObject obj)
        => obj.HasContextTag("alcohol_item") || obj.QualifiedItemId is "(O)346" or "(O)348" or "(O)459" or "(O)303"
            || Game1Wrappers.ObjectData.GetValueOrGetDefault(obj.ItemId)?.Buffs?.Any(static buff => buff.BuffId == "17") == true;

    /// <summary>
    /// Consumes a recipe by teaching the player the recipe.
    /// </summary>
    /// <param name="obj">The object instance.</param>
    /// <returns>True if the recipe was taught, false otherwise.</returns>
    public static bool ConsumeRecipeImpl(this SObject obj)
    {
        Guard.IsNotNull(obj);
        Guard.IsNotNull(Game1.player);

        if (obj.IsRecipe)
        {
            string recipeName = obj.Name;

            // vanilla removes the word "Recipe" from the end
            // because ???
            int idx = recipeName.LastIndexOf("Recipe");
            if (idx > 0)
            {
                recipeName = recipeName[.. (idx - 1)];
            }

            return obj.Category == SObject.CookingCategory
                ? Game1.player.cookingRecipes.TryAdd(recipeName, 0)
                : Game1.player.craftingRecipes.TryAdd(recipeName, 0);
        }
        return false;
    }

    /// <summary>
    /// Gets the speed multiplier associated with the tapper, or null if it's not a tapper.
    /// </summary>
    /// <param name="obj">Object to check.</param>
    /// <returns>Speed multiplier for a tapper, or null if not a tapper.</returns>
    /// <remarks>Derived from <see cref="Tree.UpdateTapperProduct"/>.</remarks>
    public static float? GetTapperMultiplier(this SObject obj)
    {
        if (obj.IsTapper())
        {
            const string tapperPrefix = "tapper_multiplier_";
            foreach (string contextTag in obj.GetContextTags())
            {
                if (contextTag.StartsWith(tapperPrefix) && float.TryParse(contextTag.AsSpan(tapperPrefix.Length), out float multiplier))
                {
                    return multiplier;
                }
            }
            return 1f;
        }
        return null;
    }
}