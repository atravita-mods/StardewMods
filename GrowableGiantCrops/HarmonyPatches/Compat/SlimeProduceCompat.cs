using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GrowableGiantCrops.HarmonyPatches.Compat;
internal static class SlimeProduceCompat
{
    /// <summary>
    /// The moddata string to mark slimeballs from when they're picked up to when they go back to the ground.
    /// </summary>
    internal const string SlimeBall = "atravita.PickedUpSlimeball";

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
            ModEntry.ModMonitor.Log($"Failed to patch Slime Produce.\n\n{ex}", LogLevel.Error);
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
            ModEntry.ModMonitor.Log($"Failed to patch Slime Produce.\n\n{ex}", LogLevel.Error);
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
            ModEntry.ModMonitor.Log($"Failed to patch SObject.drawInMenu for SlimeProduce.\n\n{ex}", LogLevel.Error);
        }
    }

    [MethodImpl(TKConstants.Hot)]
    private static void PrefixDraw(SObject __instance, ref Color color)
    {
        color = ReplaceDrawColorForSlimeEgg(color, __instance);
    }

    private static Color ReplaceDrawColorForSlimeEgg(Color prevColor, SObject item)
    {
        if (prevColor == Color.White && item.Name == "Slime Ball" && item.orderData?.Value is not null
            && uint.TryParse(item.orderData.Value.GetNthChunk('/'), out var packed))
        {
            return new(packed);
        }
        return prevColor;
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
            ModEntry.ModMonitor.Log($"Failed while prevent SlimeProduce from dropping extra drops. Please report bugs to me, not them!", LogLevel.Error);
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

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
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
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.GetFullName()}\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}

