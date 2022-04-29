using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Tools;

namespace AvoidLosingScepter;

/// <inheritdoc />
internal class ModEntry : Mod
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// Gets the logger for this mod.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        ModMonitor = this.Monitor;
        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));
    }

    /// <summary>
    /// Applies the patches for this mod.
    /// </summary>
    /// <param name="harmony">This mod's harmony instance.</param>
    /// <remarks>Delay until GameLaunched in order to patch other mods....</remarks>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            HarmonyMethod transpiler = new(typeof(ModEntry), nameof(Transpiler));
            harmony.Patch(
                original: typeof(Event).InstanceMethodNamed(nameof(Event.command_minedeath)),
                transpiler: transpiler);
            harmony.Patch(
                original: typeof(Event).InstanceMethodNamed(nameof(Event.command_hospitaldeath)),
                transpiler: transpiler);
        }
        catch (Exception ex)
        {
            ModMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Call, typeof(Game1).StaticPropertyNamed(nameof(Game1.player)).GetGetMethod()),
                new(OpCodes.Callvirt),
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(IList<Item>).InstancePropertyNamed("Item").GetGetMethod()),
                new(OpCodes.Isinst, typeof(MeleeWeapon)),
                new(OpCodes.Callvirt),
                new(OpCodes.Ldc_I4_S, 47),
                new(OpCodes.Beq),
            });

            int startindex = helper.Pointer;

            helper.FindNext(new CodeInstructionWrapper[]
            {
                new(OpCodes.Beq),
            })
            .StoreBranchDest()
            .Push()
            .AdvanceToStoredLabel()
            .DefineAndAttachLabel(out Label label)
            .Pop();

            int endindex = helper.Pointer;

            List<CodeInstruction>? copylist = new();
            foreach (CodeInstruction? code in helper.Codes.GetRange(startindex, endindex - startindex - 2))
            {
                copylist.Add(code.Clone());
            }
            copylist[^1].operand = typeof(Wand);
            copylist.Add(new(OpCodes.Brtrue_S, label));
            CodeInstruction[]? copy = copylist.ToArray();

            helper.Advance(1)
            .Insert(copy);

            return helper.Render();
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Mod crashed while transpiling mine death methods:\n\n{ex}", LogLevel.Error);
        }
        return null;
    }
}
