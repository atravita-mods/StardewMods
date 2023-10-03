using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using AtraCore;
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
        I18n.Init(helper.Translation);
        modMonitor = this.Monitor;
        helper.Events.GameLoop.DayEnding += static (_, _) => Cache.Clear();
        helper.Events.GameLoop.GameLaunched += (_, _) => this.ApplyPatches(new(this.ModManifest.UniqueID));

        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");
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
        => GetRandom(pig.myID.Value);

    [MethodImpl(TKConstants.Hot)]
    private static Random GetRandom(long id)
    {
        try
        {
            if (!Cache.TryGetValue(id, out Random? random))
            {
                unchecked
                {
                    modMonitor.DebugOnlyLog($"Cache miss: {id}", LogLevel.Info);
                    Cache[id] = random = RandomUtils.GetSeededRandom(2, (int)(id >> 1));
                }
            }
            return random;
        }
        catch (Exception ex)
        {
            modMonitor.LogError($"generating random for pig {id}", ex);
        }

        return Random.Shared;
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
                OpCodes.Callvirt,
                OpCodes.Conv_R8,
            })
            .Advance(1)
            .RemoveIncluding(new CodeInstructionWrapper[]
            {
                (OpCodes.Call, typeof(Utility).GetCachedMethod(nameof(Utility.CreateRandom), ReflectionCache.FlagTypes.StaticFlags)),
            })
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Call, typeof(ModEntry).GetCachedMethod<FarmAnimal>(nameof(GetRandom), ReflectionCache.FlagTypes.StaticFlags)),
            });

#if DEBUG
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldnull,
                (OpCodes.Callvirt, typeof(Netcode.NetFieldBase<string, Netcode.NetString>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
            })
            .Advance(2)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldsfld, typeof(ModEntry).GetCachedField(nameof(modMonitor), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ldstr, "Truffles Over"),
                new(OpCodes.Ldc_I4_1), // LogLevel.Debug
                new(OpCodes.Callvirt, typeof(IMonitor).GetCachedMethod(nameof(IMonitor.Log), ReflectionCache.FlagTypes.InstanceFlags)),
            });
#endif

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            modMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}