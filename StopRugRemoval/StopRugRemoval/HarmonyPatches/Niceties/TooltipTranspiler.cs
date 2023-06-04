using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using AtraShared.Wrappers;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Utilities;

using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Transpiles the tooltip to add the buff duration.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class TooltipTranspiler
{
    private static string? GetTooltipDuration(Item? hoveredItem)
    {
        if (hoveredItem is SObject obj && Game1Wrappers.ObjectInfo.TryGetValue(obj.ParentSheetIndex, out string? data)
            && int.TryParse(data.GetNthChunk('/', 8), out int minutesDuration) && minutesDuration > 0)
        {
            TimeSpan ts = BuffExtensions.ActualTime(minutesDuration);
            return $"{I18n.Duration()}: {(int)ts.TotalMinutes}:{ts.Seconds:D2}";
        }
        return null;
    }

    private static int GetAdditionalSpaceIfNeeded(string? duration) => duration is not null ? 34 : 0;

    [MethodImpl(TKConstants.Hot)]
    private static void DrawDuration(SpriteBatch b, SpriteFont font, int x, int y, string? duration)
    {
        if (duration is not null)
        {
            Utility.drawTextWithShadow(b, duration, font, new Vector2(x + 16, y + 16), Game1.textColor);
        }
    }

    // SpriteBatch b, StringBuilder text, SpriteFont font, int xOffset = 0, int yOffset = 0, int moneyAmountToDisplayAtBottom = -1, string boldTitleText = null, int healAmountToDisplay = -1, string[] buffIconsToDisplay = null, Item hoveredItem = null, int currencySymbol = 0, int extraItemToShowIndex = -1, int extraItemToShowAmount = -1, int overrideX = -1, int overrideY = -1, float alpha = 1f, CraftingRecipe craftingIngredients = null, IList<Item> additional_craft_materials = null
    [HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.drawHoverText), new[] { typeof(SpriteBatch), typeof(StringBuilder), typeof(SpriteFont), typeof(int), typeof(int), typeof(int), typeof(string), typeof(int), typeof(string[]), typeof(Item), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(CraftingRecipe), typeof(IList<Item>) })]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = StyleCopConstants.SplitParametersIntentional)]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.DeclareLocal(typeof(string), out var duration);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ret,
            })
            .Advance(1)
            .GetLabels(out var labels)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg, 9), // HoveredItem
                new(OpCodes.Call, typeof(TooltipTranspiler).GetCachedMethod(nameof(GetTooltipDuration), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Stloc, duration),
            }, withLabels: labels)
            .FindNext(new CodeInstructionWrapper[]
            { // first block for (if buffIconsToDisplay is not null)
                (OpCodes.Ldarg_S, 8), // buffIconsToDisplay
                OpCodes.Brfalse_S,
            })
            .Advance(1)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .FindPrev(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdLoc,
                OpCodes.Ldc_I4_4,
                OpCodes.Add,
            })
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
                helper.FindNext(new CodeInstructionWrapper[]
                {
                    (OpCodes.Ldarg_S, 8), // buffIconsToDisplay
                    new (SpecialCodeInstructionCases.Wildcard, static (instr) => instr.opcode == OpCodes.Brfalse_S || instr.opcode == OpCodes.Brfalse),
                })
                .Advance(1)
                .StoreBranchDest()
                .AdvanceToStoredLabel();
            }

            // we need to dig out the locals x and y here.
            // ahh this function is weird.
            helper.Push()
            .FindPrev(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Ldc_I4_S, 34),
                OpCodes.Add,
            })
            .Copy(4, out IEnumerable<CodeInstruction>? incrementY);

            CodeInstruction y = helper.CurrentInstruction.Clone();

            helper.FindPrev(new CodeInstructionWrapper[]
            {
                SpecialCodeInstructionCases.LdLoc,
                (OpCodes.Ldc_I4_S, 16),
                OpCodes.Add,
                (OpCodes.Ldc_I4_S, 34),
            });

            CodeInstruction x = helper.CurrentInstruction.Clone();
            helper.Pop()
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0), // spritebatch
                new(OpCodes.Ldarg_2), // font
                x,
                y,
                new(OpCodes.Ldloc, duration),
                new(OpCodes.Call, typeof(TooltipTranspiler).GetCachedMethod(nameof(DrawDuration), ReflectionCache.FlagTypes.StaticFlags)),
            })
            .Insert(incrementY.ToArray());

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}
