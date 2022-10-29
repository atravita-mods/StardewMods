using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches.EverlastingFertilizer;

/// <summary>
/// Removes the season check if the Everlasting Fertilizer is down.
/// </summary>
[HarmonyPatch(typeof(HoeDirt))]
internal static class RemoveSeasonCheck
{
    private static int TempusGlobeID => ModEntry.JsonAssetsAPI?.GetBigCraftableId("Tempus Globe") ?? -1;

    [MethodImpl(TKConstants.Hot)]
    private static bool IsInEverlasting(HoeDirt dirt, Crop crop)
        => ModEntry.EverlastingFertilizerID != -1 && dirt.fertilizer?.Value == ModEntry.EverlastingFertilizerID;

    // checks to see if the tile is covered by our fertilizer or is covered by a tempus globe.
    [MethodImpl(TKConstants.Hot)]
    private static bool IsInEverlastingWithTempusGlobe(Crop crop, HoeDirt dirt, int tileX, int tileY, GameLocation location)
    {
        if (IsInEverlasting(dirt, crop))
        {
            return true;
        }

        if (TempusGlobeID == -1)
        {
            ModEntry.ModMonitor.Log("Tempus globe not found?");
            return false;
        }

        // replicate the Tempus Globe's check.
        for (int x = tileX - 2; x <= tileX + 2; x++)
        {
            for (int y = tileY - 2; y <= tileY + 2; y++)
            {
                if (location.Objects.TryGetValue(new Vector2(x, y), out var obj)
                    && obj.bigCraftable.Value && obj.ParentSheetIndex == TempusGlobeID)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [HarmonyPatch(nameof(HoeDirt.plant))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldarg_3,
                (OpCodes.Newobj, typeof(Crop).GetCachedConstructor<int, int, int>(ReflectionCache.FlagTypes.InstanceFlags)),
                SpecialCodeInstructionCases.StLoc,
            })
            .Advance(4);

            var crop = helper.CurrentInstruction.ToLdLoc();

            helper.FindLast(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdArg,
                (OpCodes.Callvirt, typeof(Character).GetCachedProperty(nameof(Character.currentLocation), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                (OpCodes.Callvirt, typeof(GameLocation).GetCachedProperty(nameof(GameLocation.IsGreenhouse), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                OpCodes.Brtrue_S,
            })
            .Push()
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out var jumppoint)
            .Pop()
            .GetLabels(out var labels);

            // if Theft Of the Winter Star is not installed.
            helper.Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                crop,
                new(OpCodes.Call, typeof(RemoveSeasonCheck).GetCachedMethod(nameof(IsInEverlasting), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brtrue_S, jumppoint),
            }, withLabels: labels);

# warning - todo: the version where Theft of the Winter Star is installed. (Also remove casey's prefix).

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling Hoedirt.Draw:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}