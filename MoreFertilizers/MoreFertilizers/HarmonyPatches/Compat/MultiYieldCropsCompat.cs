using System.Reflection;
using System.Reflection.Emit;

using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using MoreFertilizers.Framework;

using StardewValley.Characters;

namespace MoreFertilizers.HarmonyPatches.Compat;

/// <summary>
/// Class that holds a patch against MultiYieldCrops to apply my fertilizers.
/// </summary>
internal static class MultiYieldCropsCompat
{
    /// <summary>
    /// Applies the patch against MultiYieldCrops.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    /// <exception cref="MethodNotFoundException">Some method was not found.</exception>
    internal static void ApplyPatches(Harmony harmony)
    {
        try
        {
            Type multi = AccessTools.TypeByName("MultiYieldCrop.MultiYieldCrops")
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<Type>("Multi Yield Crops");
            harmony.Patch(
                original: multi.InstanceMethodNamed("SpawnHarvest"),
                transpiler: new HarmonyMethod(typeof(MultiYieldCropsCompat).StaticMethodNamed(nameof(Transpiler))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("transpiling MultiYieldCrops", ex);
        }
    }

    /// <summary>
    /// Adjusts the output from MultiYieldCrops to handle the Joja and Organic fertilizers.
    /// </summary>
    /// <param name="item">The item to adjust.</param>
    /// <param name="fertilizer">The fertilizer on that square.</param>
    /// <returns>The adjusted item.</returns>
    internal static Item? AdjustItem(Item? item, int fertilizer)
    {
        if (item is not SObject obj || obj.bigCraftable.Value || fertilizer == -1)
        {
            return item;
        }

        try
        {
            if (fertilizer == ModEntry.OrganicFertilizerID && !obj.Name.Contains("Joja", StringComparison.OrdinalIgnoreCase))
            {
                obj.modData?.SetBool(CanPlaceHandler.Organic, true);
                obj.Price = (int)(obj.Price * 1.1);
                obj.Name += " (Organic)";
                obj.MarkContextTagsDirty();
            }
            else if (fertilizer == ModEntry.JojaFertilizerID)
            {
                obj.Quality = 1;
                obj.modData?.SetBool(CanPlaceHandler.Joja, true);
                obj.MarkContextTagsDirty();
            }
            else if (fertilizer == ModEntry.DeluxeJojaFertilizerID)
            {
                obj.Quality = Random.Shared.OfChance(0.2) ? 2 : 1;
                obj.modData?.SetBool(CanPlaceHandler.Joja, true);
                obj.MarkContextTagsDirty();
            }
            else if (fertilizer == ModEntry.SecretJojaFertilizerID)
            {
                obj.Quality = 0;
                obj.modData?.SetBool(CanPlaceHandler.Joja, true);
                obj.MarkContextTagsDirty();
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adjusting MultiYieldCrop or PFMAutomate item", ex);
        }
        return obj;
    }

    private static bool IsBountifulFertilizer(int fertilizer)
        => fertilizer != -1 && fertilizer == ModEntry.BountifulFertilizerID && Random.Shared.OfChance(0.1);

#pragma warning disable SA1116 // Split parameters should start on line after declaration
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            { // Find the creation of the Item and where it's stored.
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(IEnumerator<Item>).GetCachedProperty("Current", ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(SpecialCodeInstructionCases.StLoc, typeof(Item)),
            })
            .Advance(2)
            .Insert(new CodeInstruction[]
            { // Stick in our function that adjusts it for organic and joja.
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(MultiYieldCropsCompat).GetCachedMethod(nameof(AdjustItem), ReflectionCache.FlagTypes.StaticFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            { // find the creation of debris
                new(SpecialCodeInstructionCases.LdLoc, typeof(Item)),
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Ldnull),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Call, typeof(Game1).GetCachedMethod(nameof(Game1.createItemDebris), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Pop),
            });

            CodeInstruction itemLocal = helper.CurrentInstruction.Clone();
            helper.Advance(1);
            CodeInstruction locationLocal = helper.CurrentInstruction.Clone();
            helper.Advance(-1);

            helper.GetLabels(out IList<Label> labelsToMove, clear: true)
            .DefineAndAttachLabel(out Label label)
            .Insert(new CodeInstruction[]
            { // Insert a second debris creation just before if our check passes.
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(MultiYieldCropsCompat).GetCachedMethod(nameof(IsBountifulFertilizer), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brfalse_S, label),
                itemLocal,
                locationLocal,
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ldnull),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Call, typeof(Game1).GetCachedMethod(nameof(Game1.createItemDebris), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Pop),
            }, withLabels: labelsToMove)
            .FindNext(new CodeInstructionWrapper[]
            { // find just before adding the item to the hut.
                new(SpecialCodeInstructionCases.LdArg),
                new(SpecialCodeInstructionCases.LdLoc, typeof(Item)),
                new(OpCodes.Callvirt, typeof(JunimoHarvester).GetCachedMethod(nameof(JunimoHarvester.tryToAddItemToHut), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(2)
            .DefineAndAttachLabel(out Label juminoLabel)
            .Insert(new CodeInstruction[]
            { // Increase the stack to 2 if our check passes.
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(MultiYieldCropsCompat).GetCachedMethod(nameof(IsBountifulFertilizer), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brfalse_S, juminoLabel),
                new(OpCodes.Dup),
                new(OpCodes.Ldc_I4_2),
                new(OpCodes.Callvirt, typeof(Item).GetCachedProperty(nameof(Item.Stack), ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
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
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}