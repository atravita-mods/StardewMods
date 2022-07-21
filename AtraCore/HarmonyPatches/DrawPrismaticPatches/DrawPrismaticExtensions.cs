using System.Reflection.Emit;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.HarmonyHelper;
using Microsoft.Xna.Framework;

namespace AtraCore.HarmonyPatches.DrawPrismaticPatches;

internal static class DrawPrismaticExtensions
{
    internal static void AdjustUtilityTextColor(this ILHelper helper)
    {
        helper.ForEachMatch(
            new CodeInstructionWrapper[]
            {
                new (SpecialCodeInstructionCases.LdArg),
                new (OpCodes.Call, typeof(Utility).GetCachedMethod(nameof(Utility.drawTinyDigits), ReflectionCache.FlagTypes.StaticFlags)),
            },
            transformer: (helper) =>
            {
                helper.ReplaceInstruction(
                    opcode: OpCodes.Call,
                    operand: typeof(Color).GetCachedProperty(nameof(Color.White), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(),
                    keepLabels: true);
                return true;
            });
    }
}
