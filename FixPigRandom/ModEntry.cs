using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

namespace FixPigRandom;

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed.")]
internal sealed class ModEntry : Mod
{
    private static readonly Dictionary<long, Random> Cache = new();

    private static IMonitor modMonitor = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        modMonitor = this.Monitor;
        helper.Events.GameLoop.DayEnding += static (_, _) => Cache.Clear();

        this.ApplyPatches(new(this.ModManifest.UniqueID));
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.Patch(
                original: typeof(FarmAnimal).GetCachedMethod("findTruffle", ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new(typeof(ModEntry).GetCachedMethod(nameof(Transpiler), ReflectionCache.FlagTypes.StaticFlags)));
        }
        catch (Exception ex)
        {
            modMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    [MethodImpl(TKConstants.Hot)]
    private static Random GetRandom(FarmAnimal pig)
    {
        if (!Cache.TryGetValue(pig.myID.Value, out Random? random))
        {
            modMonitor.DebugOnlyLog($"Cache hit: {pig.myID.Value}");
            random = RandomUtils.GetSeededRandom(2, (int)(pig.myID.Value >> 1));
            Cache[pig.myID.Value] = random;
        }

        return random;
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, modMonitor, gen);

            helper.FindNext(new CodeInstructionWrapper[]
            { // find the creation of the random and replace it with our own.
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

#if DEBUG
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldc_I4_M1,
                (OpCodes.Callvirt, typeof(Netcode.NetFieldBase<int, Netcode.NetInt>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
            })
            .Advance(2)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldsfld, typeof(ModEntry).GetCachedField(nameof(modMonitor), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ldstr, "Truffles Over"),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Callvirt, typeof(IMonitor).GetCachedMethod(nameof(IMonitor.Log), ReflectionCache.FlagTypes.InstanceFlags)),
            });
#endif

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            modMonitor.Log($"Ran into error transpiling {original.FullDescription()}\n\n{ex}", LogLevel.Error);
            original.Snitch(modMonitor);
        }
        return null;
    }
}