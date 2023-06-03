using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using GrowableGiantCrops.HarmonyPatches.GrassPatches;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.TerrainFeatures;

namespace GrowableGiantCrops.HarmonyPatches.Compat;

/// <summary>
/// Patches MoreGrassStarters.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class MoreGrassStartersCompat
{
    /// <summary>
    /// Applies the patches for this class.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    /// <param name="registry">The modregistery.</param>
    internal static void ApplyPatch(Harmony harmony, IModRegistry registry)
    {
        try
        {
            if (AccessTools.TypeByName("MoreGrassStarters.GrassStarterItem") is Type grassStarter)
            {
                harmony.Patch(
                    original: grassStarter.GetCachedMethod("placementAction", ReflectionCache.FlagTypes.InstanceFlags),
                    prefix: new HarmonyMethod(typeof(MoreGrassStartersCompat).StaticMethodNamed(nameof(Postfix))));
            }
            else
            {
                ModEntry.ModMonitor.Log($"MoreGrassStarter's GrassStarter item could not be found?.", LogLevel.Error);
            }

            if (registry.Get("spacechase0.MoreGrassStarters") is IModInfo modInfo)
            {
                if (modInfo.Manifest.Version.IsOlderThan("1.2.1"))
                {
                    if (AccessTools.TypeByName("MoreGrassStarters.Mod") is Type moreGrassStarters)
                    {
                        harmony.Patch(
                           original: moreGrassStarters.GetCachedMethod("OnDayStarted", ReflectionCache.FlagTypes.InstanceFlags),
                           transpiler: new HarmonyMethod(typeof(MoreGrassStartersCompat).StaticMethodNamed(nameof(Transpiler))));
                    }
                    else
                    {
                        ModEntry.ModMonitor.Log($"MoreGrassStarter's modentry class could not be found?.", LogLevel.Error);
                    }
                }
                else if (modInfo.Manifest.Version.IsOlderThan("1.2.2"))
                {
                    ModEntry.ModMonitor.Log($"Detected MoreGrassStarters version 1.2.1. You may see odd issues with placed grass. Please update that mod to 1.2.2 or above!", LogLevel.Warn);
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("patching More Grass Starters", ex);
        }
    }

    private static void Postfix(SObject __instance, GameLocation location, int x, int y, bool __result)
    {
        if (!__result || __instance?.modData?.GetBool(SObjectPatches.ModDataKey) != true)
        {
            return;
        }

        try
        {
            Vector2 tile = new(x / Game1.tileSize, y / Game1.tileSize);
            if (location.terrainFeatures?.TryGetValue(tile, out TerrainFeature? terrain) == true
                && terrain is Grass grass)
            {
                grass.numberOfWeeds.Value = 1;
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("overriding health of MGS grass", ex);
        }
    }

    private static bool ShouldSkipThisGrass(Grass? grass) => grass?.modData?.ContainsKey(SObjectPatches.ModDataKey) == true;

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // terrainFeature is Grass grass
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Isinst, typeof(Grass)),
                SpecialCodeInstructionCases.StLoc,
                SpecialCodeInstructionCases.LdLoc,
                OpCodes.Brfalse_S,
            })
            .Advance(3);

            CodeInstruction ldloc = helper.CurrentInstruction.Clone();
            helper.Push()
            .Advance(1)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label jumpPoint)
            .Pop()
            .GetLabels(out IList<Label>? labelsToMove)
            .Insert(new CodeInstruction[]
            {
                ldloc,
                new(OpCodes.Call, typeof(MoreGrassStartersCompat).GetCachedMethod(nameof(ShouldSkipThisGrass), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brtrue, jumpPoint),
            }, withLabels: labelsToMove);

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
