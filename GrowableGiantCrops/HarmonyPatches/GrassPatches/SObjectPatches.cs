﻿using System.Reflection;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using FastExpressionCompiler.LightExpression;

using GrowableGiantCrops.Framework.Assets;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Locations;
using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.GrassPatches;

[HarmonyPatch(typeof(SObject))]
internal static class SObjectPatches
{
    /// <summary>
    /// A mod data key used to mark custom grass types.
    /// </summary>
    internal const string ModDataKey = "atravita.GrowableGiantCrop.GrassType";

    /// <summary>
    /// The ParentSheetIndex of a grass starter.
    /// </summary>
    internal const int GrassStarterIndex = 297;

    #region delegates

    private static Lazy<Func<SObject, bool>?> isMoreGrassStarter = new(() =>
    {
        Type? moreGrass = AccessTools.TypeByName("MoreGrassStarters.GrassStarterItem");
        if (moreGrass is null)
        {
            return null;
        }

        ParameterExpression? obj = Expression.ParameterOf<SObject>("obj");
        TypeBinaryExpression? express = Expression.TypeIs(obj, moreGrass);
        return Expression.Lambda<Func<SObject, bool>>(express, obj).CompileFast();
    });

    internal static Func<SObject, bool>? IsMoreGrassStarter => isMoreGrassStarter.Value;

    private static Lazy<Func<int, SObject>?> instantiateMoreGrassStarter = new(() =>
    {
        Type? moreGrass = AccessTools.TypeByName("MoreGrassStarters.GrassStarterItem");
        if (moreGrass is null)
        {
            return null;
        }

        ParameterExpression which = Expression.ParameterOf<int>("which");
        ConstructorInfo constructor = moreGrass.GetCachedConstructor<int>(ReflectionCache.FlagTypes.InstanceFlags);
        NewExpression newObj = Expression.New(constructor, which);
        return Expression.Lambda<Func<int, SObject>>(newObj, which).CompileFast();
    });

    internal static Func<int, SObject>? InstantiateMoreGrassStarter => instantiateMoreGrassStarter.Value;

    private static Lazy<Func<Grass, bool>?> isMoreGrassGrass = new(() =>
    {
        Type? moreGrass = AccessTools.TypeByName("MoreGrassStarters.CustomGrass");
        if (moreGrass is null)
        {
            return null;
        }

        ParameterExpression? obj = Expression.ParameterOf<Grass>("grass");
        TypeBinaryExpression? express = Expression.TypeIs(obj, moreGrass);
        return Expression.Lambda<Func<Grass, bool>>(express, obj).CompileFast();
    });

    internal static Func<Grass, bool>? IsMoreGrassGrass => isMoreGrassGrass.Value;

    private static Lazy<Func<int, Grass>?> instantiateMoreGrassGrass = new(() =>
    {
        Type? moreGrass = AccessTools.TypeByName("MoreGrassStarters.CustomGrass");
        if (moreGrass is null)
        {
            return null;
        }

        ParameterExpression which = Expression.ParameterOf<int>("which");
        ConstantExpression numberOfWeeds = Expression.ConstantInt(1);
        ConstructorInfo constructor = moreGrass.GetCachedConstructor<int, int>(ReflectionCache.FlagTypes.InstanceFlags);
        NewExpression newObj = Expression.New(constructor, which, numberOfWeeds);
        return Expression.Lambda<Func<int, Grass>>(newObj, which).CompileFast();
    });

    internal static Func<int, Grass>? InstantiateMoreGrassGrass => instantiateMoreGrassGrass.Value;

    #endregion

    #region draw patches

    private static AssetHolder? texture;
    private static Dictionary<string, int> offsets = new()
    {
        ["spring"] = 0,
        ["summer"] = 20,
        ["fall"] = 40,
        ["winter"] = 80, // remember that desert/indoors should use spring instead.
        ["2"] = 60,
        ["3"] = 80,
        ["4"] = 100,
        ["5"] = 120,
        ["6"] = 140,
    };

    #endregion

    [MethodImpl(TKConstants.Hot)]
    private static bool GetDrawParts(SObject obj, [NotNullWhen(true)] out Texture2D? tex, out int offset)
    {
        tex = null;
        offset = 0;
        if (obj.ParentSheetIndex != GrassStarterIndex || obj.modData?.TryGetValue(ModDataKey, out var idx) != true)
        {
            return false;
        }

        texture ??= AssetCache.Get("TerrainFeatures/grass");
        if (texture?.Get() is not Texture2D temptex)
        {
            return false;
        }
        tex = temptex;

        if (idx == "1")
        {
            GameLocation loc = Game1.currentLocation;
            idx = loc is not Desert && loc is not IslandLocation && loc.IsOutdoors ? Game1.GetSeasonForLocation(loc) : "spring";
        }

        return offsets.TryGetValue(idx, out offset);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) } )]
    private static bool PrefixDraw(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
    {
        if (!GetDrawParts(__instance, out Texture2D? tex, out int offset))
        {
            return true;
        }

        Vector2 position = Game1.GlobalToLocal(
            Game1.viewport,
            new Vector2(x * Game1.tileSize, y * Game1.tileSize));
        float draw_layer = Math.Max(
            0f,
            ((y * Game1.tileSize) + 40) / 10000f) + (x * 1E-05f);

        spriteBatch.Draw(
            texture: tex,
            position,
            sourceRectangle: new Rectangle(30, offset, 15, 20),
            color: Color.White * alpha,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: Vector2.One * Game1.pixelZoom,
            effects: SpriteEffects.None,
            draw_layer);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.drawWhenHeld))]
    private static bool PrefixDrawWhenHeld(SObject __instance, SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
    {
        if (!GetDrawParts(__instance, out Texture2D? tex, out int offset))
        {
            return true;
        }

        spriteBatch.Draw(
            texture: tex,
            position: objectPosition,
            sourceRectangle: new Rectangle(30, offset, 15, 20),
            color: Color.White,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: 4f,
            effects: SpriteEffects.None,
            layerDepth: Math.Max(0f, (f.getStandingY() + 3) / 10000f));

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SObject.drawInMenu))]
    private static bool PrefixDrawInMenu(SObject __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        if (!GetDrawParts(__instance, out Texture2D? tex, out int offset))
        {
            return true;
        }

        spriteBatch.Draw(
            texture: tex,
            position: location + new Vector2(32, 48),
            sourceRectangle: new Rectangle(30, offset, 15, 20),
            color: color * transparency,
            rotation: 0f,
            new Vector2(8f, 16f),
            scale: scaleSize * Game1.pixelZoom,
            effects: SpriteEffects.None,
            layerDepth);
        if (((drawStackNumber == StackDrawType.Draw && __instance.maximumStackSize() > 1 && __instance.Stack > 1) || drawStackNumber == StackDrawType.Draw_OneInclusive)
            && scaleSize > 0.3f && __instance.Stack != int.MaxValue)
        {
            Utility.drawTinyDigits(
                toDraw: __instance.Stack,
                b: spriteBatch,
                position: location + new Vector2(64 - Utility.getWidthOfTinyDigitString(__instance.Stack, 3f * scaleSize) + (3f * scaleSize), 64f - (18f * scaleSize) + 2f),
                scale: 3f * scaleSize,
                layerDepth: 1f,
                c: Color.White);
        }
        return false;
    }
}
