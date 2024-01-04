namespace StopRugRemoval.HarmonyPatches.Niceties;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using AtraShared.Wrappers;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.Menus;


/// <summary>
/// Transpiles the tooltip to add the buff duration.
/// </summary>
[HarmonyPatch]
internal static class TooltipTranspiler
{
    [MethodImpl(TKConstants.Hot)]
    private static string? GetTooltipDuration(Item? hoveredItem)
    {
        if (hoveredItem is SObject obj && obj.HasTypeObject() && Game1Wrappers.ObjectData.TryGetValue(obj.ItemId, out ObjectData? data)
            && data?.Buff?.Duration is > 0)
        {
            int minutesDuration = data.Buff.Duration;
            if (obj.Quality != 0)
            {
                minutesDuration += minutesDuration / 2;
            }
            TimeSpan ts = BuffExtensions.ActualTime(minutesDuration);
            return $"{I18n.Duration()}: {(int)ts.TotalMinutes}:{ts.Seconds:D2}";
        }
        return null;
    }

    [MethodImpl(TKConstants.Hot)]
    private static int GetAdditionalSpaceIfNeeded(string? duration) => duration is not null ? 34 : 0;

    [MethodImpl(TKConstants.Hot)]
    private static void DrawDuration(SpriteBatch b, SpriteFont font, int x, int y, string? duration)
    {
        if (duration is not null)
        {
            Utility.drawTextWithShadow(b, duration, font, new Vector2(x + 16, y + 16), Game1.textColor);
        }
    }

    [HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawHoverText), new[] { typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(string), typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe), typeof(IList<Item>), typeof(Texture2D), typeof(Rectangle?), typeof(Color?), typeof(Color?), typeof(float), typeof(int), typeof(int) })]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = StyleCopConstants.SplitParametersIntentional)]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.DeclareLocal(typeof(string), out LocalBuilder? duration);
            helper.FindNext(
            [
                OpCodes.Ret,
            ])
            .Advance(1)
            .GetLabels(out IList<Label>? labels)
            .Insert(
            [
                new(OpCodes.Ldarg, 9), // HoveredItem
                new(OpCodes.Call, typeof(TooltipTranspiler).GetCachedMethod(nameof(GetTooltipDuration), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Stloc, duration),
            ], withLabels: labels)
            .FindNext(
            [ // first block for (if buffIconsToDisplay is not null)
                (OpCodes.Ldarg_S, 8), // buffIconsToDisplay
                OpCodes.Brfalse_S,
            ])
            .Advance(1)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .FindPrev(
            [
                SpecialCodeInstructionCases.LdLoc,
                OpCodes.Ldc_I4_4,
                OpCodes.Add,
            ])
            .Copy(4, out IEnumerable<CodeInstruction>? codes);

            // this part will increment the required height if needed.
            CodeInstruction[] codesArray = codes.ToArray();
            Array.Resize(ref codesArray, codesArray.Length + 1);
            for (int i = codesArray.Length - 1; i > 2; i--)
            {
                codesArray[i] = codesArray[i - 1];
            }
            codesArray[1] = new CodeInstruction(OpCodes.Ldloc, duration);
            codesArray[2] = new CodeInstruction(OpCodes.Call, typeof(TooltipTranspiler).GetCachedMethod(nameof(GetAdditionalSpaceIfNeeded), ReflectionCache.FlagTypes.StaticFlags));

            helper.GetLabels(out IList<Label>? labelsToMove)
                  .Insert(codesArray, labelsToMove);

            // now we want the third block for (if buffIconsToDisplay is not null)
            for (int i = 0; i < 2; i++)
            {
                helper.FindNext(
                [
                    (OpCodes.Ldarg_S, 8), // buffIconsToDisplay
                    new (SpecialCodeInstructionCases.Wildcard, static (instr) => instr.opcode == OpCodes.Brfalse_S || instr.opcode == OpCodes.Brfalse),
                ])
                .Advance(1)
                .StoreBranchDest()
                .AdvanceToStoredLabel();
            }

            // we need to dig out the locals x and y here.
            // ahh this function is weird.
            _ = helper.Push()
            .FindPrev(
            [
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Ldc_I4_S, 34),
                OpCodes.Add,
            ])
            .Copy(4, out IEnumerable<CodeInstruction>? incrementY);

            CodeInstruction y = helper.CurrentInstruction.Clone();

            helper.FindPrev(
            [
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Ldc_I4_S, 16),
                OpCodes.Add,
                (OpCodes.Ldc_I4_S, 34),
            ]);

            CodeInstruction x = helper.CurrentInstruction.Clone();
            helper.Pop()
            .Insert(
            [
                new(OpCodes.Ldarg_0), // spritebatch
                new(OpCodes.Ldarg_2), // font
                x,
                y,
                new(OpCodes.Ldloc, duration),
                new(OpCodes.Call, typeof(TooltipTranspiler).GetCachedMethod(nameof(DrawDuration), ReflectionCache.FlagTypes.StaticFlags)),
            ])
            .Insert(incrementY.ToArray());

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
