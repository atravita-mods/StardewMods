using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Integrations;
using AtraShared.Integrations.GMCMAttributes;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewModdingAPI.Events;
using StardewValley.Locations;

namespace SleepInWedding.cs;

/// <summary>
/// The config class for this mod.
/// </summary>
internal sealed class ModConfig
{
    /// <summary>
    /// Gets or sets when the wedding should begin.
    /// </summary>
    [GMCMInterval(10)]
    [GMCMRange(600, 2600)]
    public int WeddingTime { get; set; } = 800;
}

/// <inheritdoc />
[HarmonyPatch(typeof(GameLocation))]
internal sealed class ModEntry : Mod
{
    internal static ModConfig Config { get; private set; } = null!;

    internal static IMonitor ModMonitor { get; private set; } = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoad;
    }

    [MethodImpl(TKConstants.Hot)]
    private static int GetWeddingTime() => Config.WeddingTime;

    [HarmonyPatch(nameof(GameLocation.checkForEvents))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Reviewed.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.weddingsToday), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(List<int>).GetCachedProperty(nameof(List<int>.Count), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                new(OpCodes.Ldc_I4_0),
                new(SpecialCodeInstructionCases.Wildcard, (instr) => instr.opcode == OpCodes.Ble || instr.opcode == OpCodes.Ble_S),
            })
            .GetLabels(out var labelsToMove, clear: true)
            .DefineAndAttachLabel(out var skip)
            .Push()
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out var bypassWedding)
            .Pop()
            .Insert(new CodeInstruction[]
            { // if (Config.WeddingTime > Game1.timeOfDay) && (Game1.currentLocation is not Town), skip wedding for now.
                new(OpCodes.Call, typeof(ModEntry).GetCachedMethod(nameof(GetWeddingTime), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.timeOfDay), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ble, skip),
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedProperty(nameof(Game1.currentLocation), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                new(OpCodes.Isinst, typeof(Town)),
                new(OpCodes.Brfalse, bypassWedding),
            }, withLabels: labelsToMove);

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into error transpiling {original.FullDescription()}.\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        GMCMHelper helper = new(this.Monitor, this.Helper.Translation, this.Helper.ModRegistry, this.ModManifest);
        if (helper.TryGetAPI())
        {
            helper.Register(
                reset: static () => Config = new(),
                save: () => this.Helper.AsyncWriteConfig(this.Monitor, Config))
            .GenerateDefaultGMCM(static () => Config);
        }
    }

    /// <summary>
    /// calls queueWeddingsForToday just after save is loaded.
    /// Game doesn't seem to call it.
    /// </summary>
    /// <param name="sender">SMAPI.</param>
    /// <param name="e">Event args.</param>
    private void OnSaveLoad(object? sender, SaveLoadedEventArgs e)
        => Game1.queueWeddingsForToday();
}