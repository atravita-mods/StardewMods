using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Extensions;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;
using StardewValley.GameData.GiantCrops;
using StardewValley.TerrainFeatures;

namespace GiantCropFertilizer.HarmonyPatches;

/// <summary>
/// Handles the transpiler for Crop.newDay.
/// </summary>
[HarmonyPatch(typeof(Crop))]
internal static class CropTranspiler
{
    /// <summary>
    /// Gets the chance for a big crop based on the fertilizer.
    /// </summary>
    /// <param name="chance">The previous change value.</param>
    /// <param name="crop">The crop to check.</param>
    /// <param name="tilePosition">The tile position of the crop.</param>
    /// <returns>chance.</returns>
    private static double GetChanceForFertilizer(double chance, Crop crop, Vector2 tilePosition)
    {
        string? fertilizer = crop?.currentLocation.terrainFeatures.TryGetValue(tilePosition, out TerrainFeature? dirt) == true
            ? (dirt as HoeDirt)?.fertilizer.Value
            : null;
        ModEntry.ModMonitor.DebugOnlyLog(fertilizer is not null or "0", $"Testing fertilizer {fertilizer} with {ModEntry.GiantCropFertilizerID}", LogLevel.Info);
        return ModEntry.IsGiantCropFertilizer(fertilizer) ? ModEntry.Config.GiantCropChance : chance;
    }

    /// <summary>
    /// Removes the big crop fertilizer after a big crop was made.
    /// </summary>
    /// <param name="dirt">HoeDirt instance.</param>
    private static void RemoveFertilizer(HoeDirt? dirt)
    {
        if (dirt is not null && dirt.fertilizer.Value == ModEntry.GiantCropFertilizerID)
        {
            ModEntry.ModMonitor.DebugOnlyLog("Successfully created giant crop, now removing fertilizer", LogLevel.Info);
            dirt.fertilizer.Value = null;
        }
    }

    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyPatch(nameof(Crop.newDay))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // random.NextBool(giantCrop.Chance);
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Ldfld, typeof(GiantCropData).GetCachedField(nameof(GiantCropData.Chance), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call, typeof(RandomExtensions).GetCachedMethod<Random, float>(nameof(RandomExtensions.NextBool), ReflectionCache.FlagTypes.StaticFlags)),
                OpCodes.Brfalse,
            })
            .Advance(2)
            .Insert(new CodeInstruction[]
            { // And replace the hard-coded number if necessary.
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Crop).GetCachedField("tilePosition", ReflectionCache.FlagTypes.InstanceFlags)), // sigh, gotta get the tile position now.
                new(OpCodes.Call, typeof(CropTranspiler).GetCachedMethod(nameof(GetChanceForFertilizer), ReflectionCache.FlagTypes.StaticFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            { // Locate the code that deletes the crop after a giant crop is created.
                SpecialCodeInstructionCases.LdLoc,
                new(OpCodes.Ldfld, typeof(GameLocation).GetCachedField(nameof(GameLocation.terrainFeatures), ReflectionCache.FlagTypes.InstanceFlags)),
                new(SpecialCodeInstructionCases.Wildcard),
                new(OpCodes.Callvirt),
                new(OpCodes.Castclass, typeof(HoeDirt)),
                new(OpCodes.Ldnull),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldnull),
            })
            .Insert(new CodeInstruction[]
            { // Insert a call that removes the fertilizer as well.
                new(OpCodes.Dup),
                new(OpCodes.Call, typeof(CropTranspiler).GetCachedMethod(nameof(RemoveFertilizer), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}