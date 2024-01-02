using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

using StardewValley.GameData.WildTrees;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.Framework.Assets;

/// <summary>
/// Manages loading and editing assets for this mod.
/// </summary>
internal static class AssetManager
{
    /// <summary>
    /// The const string that starts for running JA/MGC textures through the content pipeline.
    /// </summary>
    internal const string GiantCropPrefix = "Mods/atravita.GrowableGiantCrops/";

    private static Lazy<Texture2D> toolTex = new(() => Game1.content.Load<Texture2D>(ToolTextureName!.BaseName));

    private static IAssetName wildTrees = null!;

    /// <summary>
    /// Gets the tool texture.
    /// </summary>
    internal static Texture2D ToolTexture => toolTex.Value;

    /// <summary>
    /// Gets the IAssetName corresponding to the shovel's texture.
    /// </summary>
    internal static IAssetName ToolTextureName { get; private set; } = null!;

    /// <summary>
    /// Gets the IAssetName corresponding to the shop graphics.
    /// </summary>
    internal static IAssetName ShopGraphics { get; private set; } = null!;

    #region palm trees

    /// <summary>
    /// Gets the asset path of the winter big palm tree.
    /// </summary>
    internal static IAssetName WinterBigPalm { get; private set; } = null!;

    /// <summary>
    /// Gets the asset path of the winter small palm tree.
    /// </summary>
    internal static IAssetName WinterPalm { get; private set; } = null!;

    /// <summary>
    /// Gets the asset path of the fall big palm tree.
    /// </summary>
    internal static IAssetName FallBigPalm { get; private set; } = null!;

    /// <summary>
    /// Gets the asset path of the fall small palm tree.
    /// </summary>
    internal static IAssetName FallPalm { get; private set; } = null!;

    #endregion

    /// <summary>
    /// Initializes the AssetManager.
    /// </summary>
    /// <param name="parser">GameContent helper.</param>
    internal static void Initialize(IGameContentHelper parser)
    {
        ToolTextureName = parser.ParseAssetName($"{GiantCropPrefix}Shovel");
        ShopGraphics = parser.ParseAssetName($"{GiantCropPrefix}Shop");

        WinterBigPalm = parser.ParseAssetName($"{GiantCropPrefix}WinterBigPalm");
        WinterPalm = parser.ParseAssetName($"{GiantCropPrefix}WinterPalm");

        FallBigPalm = parser.ParseAssetName($"{GiantCropPrefix}FallBigPalm");
        FallPalm = parser.ParseAssetName($"{GiantCropPrefix}FallPalm");

        wildTrees = parser.ParseAssetName("Data/WildTrees");
    }

    /// <inheritdoc cref="IContentEvents.AssetsInvalidated" />
    internal static void Reset(IReadOnlySet<IAssetName>? assets = null)
    {
        if ((assets is null || assets.Contains(ToolTextureName)) && toolTex.IsValueCreated)
        {
            toolTex = new(() => Game1.content.Load<Texture2D>(ToolTextureName.BaseName));
        }
    }

    internal static void Invalidate(IGameContentHelper helper)
    {
        helper.InvalidateCacheAndLocalized(wildTrees.BaseName);
    }

    /// <inheritdoc cref="IContentEvents.AssetRequested" />
    internal static void OnAssetRequested(AssetRequestedEventArgs e)
    {
        if (ModEntry.Config.PalmTreeBehavior != PalmTreeBehavior.Default && e.NameWithoutLocale.IsEquivalentTo(wildTrees))
        {
            e.Edit(EditWildTrees);
        }

        if (!e.NameWithoutLocale.StartsWith(GiantCropPrefix, false, false))
        {
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo(ToolTextureName))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/shovel.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(ShopGraphics))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/void_grass_box.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(WinterBigPalm))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/winter_palm2.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(WinterPalm))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/winter_palm.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(FallBigPalm))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/fall_palm2.png", AssetLoadPriority.Exclusive);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(FallPalm))
        {
            e.LoadFromModFile<Texture2D>("assets/textures/fall_palm.png", AssetLoadPriority.Exclusive);
        }
    }

    private static void EditWildTrees(IAssetData data)
    {
        IDictionary<string, WildTreeData> editor = data.AsDictionary<string, WildTreeData>().Data;

        if (editor.TryGetValue(Tree.palmTree, out WildTreeData? valleyTree))
        {
            if (ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Seasonal))
            {
                WildTreeTextureData winterTex = new()
                {
                    Season = Season.Winter,
                    Texture = WinterPalm.BaseName,
                };

                WildTreeTextureData fallTex = new()
                {
                    Season = Season.Fall,
                    Texture = FallPalm.BaseName,
                };

                valleyTree.Textures.InsertRange(0, new[] { winterTex, fallTex });
            }

            if (ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Stump))
            {
                valleyTree.IsStumpDuringWinter = true;
            }
        }

        if (editor.TryGetValue(Tree.palmTree2, out WildTreeData? islandTree))
        {
            if (ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Seasonal))
            {
                WildTreeTextureData winterTex = new()
                {
                    Season = Season.Winter,
                    Texture = WinterBigPalm.BaseName,
                };

                WildTreeTextureData fallTex = new()
                {
                    Season = Season.Fall,
                    Texture = FallBigPalm.BaseName,
                };

                islandTree.Textures.InsertRange(0, new[] { winterTex, fallTex });
            }

            if (ModEntry.Config.PalmTreeBehavior.HasFlagFast(PalmTreeBehavior.Stump))
            {
                islandTree.IsStumpDuringWinter = true;
            }
        }
    }
}
