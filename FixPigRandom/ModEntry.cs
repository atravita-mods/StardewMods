﻿using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace FixPigRandom;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private static readonly ConditionalWeakTable<FarmAnimal, Random> Cache = new();

    private static IMonitor modMonitor = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        modMonitor = this.Monitor;
        helper.Events.GameLoop.DayEnding += static (_, _) => Cache.Clear();

        Harmony harmony = new(this.ModManifest.UniqueID);

        harmony.Patch(
            original: typeof(FarmAnimal).GetCachedMethod("findTruffle", ReflectionCache.FlagTypes.InstanceFlags),
            transpiler: new(typeof(ModEntry).GetCachedMethod(nameof(Transpiler), ReflectionCache.FlagTypes.StaticFlags)));
    }

    [MethodImpl(TKConstants.Hot)]
    private static Random GetRandom(FarmAnimal pig)
    {
        if (!Cache.TryGetValue(pig, out Random? random))
        {
            random = RandomUtils.GetSeededRandom(2, (int)(pig.myID.Value >> 1));
            Cache.AddOrUpdate(pig, random);
        }

        return random;
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.modMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(FarmAnimal).GetCachedField(nameof(FarmAnimal.myID), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Call, // this is an op_Impl
                OpCodes.Conv_I4,
            })
            .Advance(1)
            .RemoveUntil(new CodeInstructionWrapper[]
            {
                (OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.NextDouble), ReflectionCache.FlagTypes.InstanceFlags, Type.EmptyTypes)),
            })
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Call, typeof(ModEntry).GetCachedMethod(nameof(GetRandom), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            modMonitor.Log($"Ran into error transpiling {original.FullDescription()}\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.modMonitor);
        }
        return null;
    }
}