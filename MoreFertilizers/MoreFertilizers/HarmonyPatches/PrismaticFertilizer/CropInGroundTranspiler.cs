﻿using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using MoreFertilizers.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.PrismaticFertilizer;

/// <summary>
/// Transpiles a crop in the ground to draw a prismatic color.
/// </summary>
[HarmonyPatch(typeof(Crop))]
internal static class CropInGroundTranspiler
{
    [MethodImpl(TKConstants.Hot)]
    private static Color GetPrismaticColor(Color prevcolor, Vector2 tileLocation)
    {
        if (prevcolor != Color.White && Game1.currentLocation?.terrainFeatures?.TryGetValue(tileLocation, out TerrainFeature? terrain) == true && terrain is HoeDirt dirt
            && dirt.modData?.GetBool(CanPlaceHandler.PrismaticFertilizer) == true)
        {
            return Utility.GetPrismaticColor((int)(tileLocation.X + tileLocation.Y), 1);
        }
        return prevcolor;
    }

    [HarmonyPatch(nameof(Crop.draw))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindLast(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.tintColor), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Callvirt,
                SpecialCodeInstructionCases.StLoc,
            })
            .Advance(3)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_2),
                new (OpCodes.Call, typeof(CropInGroundTranspiler).GetCachedMethod(nameof(GetPrismaticColor), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}