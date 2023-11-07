using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GrowableGiantCrops.HarmonyPatches.Compat;

/// <summary>
/// Compat for Slime Produce.
/// </summary>
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class SlimeProduceCompat
{
    /// <summary>
    /// The moddata string to mark slimeballs from when they're picked up to when they go back to the ground.
    /// </summary>
    internal const string SlimeBall = "atravita.PickedUpSlimeball";

    /// <summary>
    /// The qualified item id of a slime ball.
    /// </summary>
    internal const string SlimeBallQualId = "(BC)56";

    /// <summary>
    /// Applies these patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatches(Harmony harmony)
    {
        Type? slimeProduce = AccessTools.TypeByName("SlimeProduce.SlimeBall");

        if (slimeProduce is null)
        {
            ModEntry.ModMonitor.Log($"Failed to find SlimeProduce, you may see extraneous incorrect item drops.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(
                original: slimeProduce.GetCachedConstructor<SObject>(ReflectionCache.FlagTypes.InstanceFlags),
                postfix: new(typeof(SlimeProduceCompat).StaticMethodNamed(nameof(Postfix))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("patching Slime Produce", ex);
        }

        Type? slimeEntry = AccessTools.TypeByName("SlimeProduce.ModEntry");

        if (slimeEntry is null)
        {
            ModEntry.ModMonitor.Log($"Failed to find SlimeProduce, you may see extraneous incorrect item drops.", LogLevel.Error);
            return;
        }

        try
        {
            harmony.Patch(
                original: slimeEntry.GetCachedMethod("OnObjectListChanged", ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new(typeof(SlimeProduceCompat).StaticMethodNamed(nameof(Transpiler))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("patching Slime Produce", ex);
        }

        try
        {
            harmony.Patch(
                original: typeof(SObject).GetCachedMethod(
                    nameof(SObject.drawInMenu),
                    ReflectionCache.FlagTypes.UnflattenedInstanceFlags,
                    new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }
            ),
                prefix: new(typeof(SlimeProduceCompat).StaticMethodNamed(nameof(PrefixDraw))));

            harmony.Patch(
                original: typeof(SObject).GetCachedMethod(
                    nameof(SObject.drawWhenHeld),
                    ReflectionCache.FlagTypes.UnflattenedInstanceFlags
            ),
                transpiler: new(typeof(SlimeProduceCompat).StaticMethodNamed(nameof(TranspileSObjectDrawWhenHeld))));
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("patching SObject.drawInMenu for SlimeProduce", ex);
        }
    }

    /// <summary>
    /// Replace the color for a Slime Ball if needed.
    /// </summary>
    /// <param name="prevColor">Previous color.</param>
    /// <param name="item">Item to look at.</param>
    /// <returns>New color.</returns>
    [MethodImpl(TKConstants.Hot)]
    internal static Color ReplaceDrawColorForSlimeEgg(Color prevColor, SObject item)
    {
        if (prevColor == Color.White && item.QualifiedItemId == SlimeBallQualId && item.orderData?.Value is not null
            && uint.TryParse(item.orderData.Value.GetNthChunk('/'), out uint packed))
        {
            return new(packed);
        }
        return prevColor;
    }

    [MethodImpl(TKConstants.Hot)]
    private static void PrefixDraw(SObject __instance, ref Color color)
    {
        color = ReplaceDrawColorForSlimeEgg(color, __instance);
    }

    private static void Postfix(object __instance, SObject slimeBall)
    {
        try
        {
            if (slimeBall.modData?.GetBool(SlimeBall) == true)
            {
                new Traverse(__instance).Field("Valid").SetValue(false);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Failed while preventing SlimeProduce from dropping extra drops. Please report bugs to me, not them!", LogLevel.Error);
            ModEntry.ModMonitor.Log(ex.ToString());
        }
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            {
                (OpCodes.Isinst, typeof(SlimeHutch)),
            })
            .Remove(1);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    private static IEnumerable<CodeInstruction>? TranspileSObjectDrawWhenHeld(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            // lots of places things are drawn here.
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            // first one is the bigcraftable, second one is the nonbigcraftable
            helper.FindNext(new CodeInstructionWrapper[]
            {
                    (OpCodes.Call, typeof(Color).GetCachedProperty(nameof(Color.White), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
            })
            .Advance(1)
            .Insert(new CodeInstruction[]
            {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Call, typeof(SlimeProduceCompat).GetCachedMethod(nameof(ReplaceDrawColorForSlimeEgg), ReflectionCache.FlagTypes.StaticFlags)),
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