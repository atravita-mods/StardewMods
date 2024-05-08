using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Menus;

namespace ShopTabs.HarmonyPatches;

[HarmonyPatch(typeof(ShopMenu))]
internal static class ShopMenuPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ShopMenu.draw))]
    private static IEnumerable<CodeInstruction>? TranspileDraw(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindLast([
                (OpCodes.Call, typeof(Game1).GetCachedProperty(nameof(Game1.options), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                (OpCodes.Ldfld, typeof(Options).GetCachedField(nameof(Options.showMerchantPortraits),ReflectionCache.FlagTypes.InstanceFlags)),
            ])
            .FindPrev([
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(IClickableMenu).GetCachedField(nameof(IClickableMenu.xPositionOnScreen), ReflectionCache.FlagTypes.InstanceFlags)),
                (OpCodes.Ldc_I4, 320)
            ])
            .Advance(3)
            .Insert([
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(ShopMenuPatches).GetCachedMethod(nameof(IncreasedTabOffset), ReflectionCache.FlagTypes.StaticFlags)),
                new CodeInstruction(OpCodes.Add),
            ])
            .FindNext([
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(ShopMenu).GetCachedField(nameof(ShopMenu.potraitPersonDialogue), ReflectionCache.FlagTypes.InstanceFlags))
            ])
            .FindNext([
                OpCodes.Conv_I4,
                OpCodes.Sub,
                new CodeInstructionWrapper(SpecialCodeInstructionCases.Wildcard, static instr => instr.LoadsConstant(64))
            ])
            .Advance(3)
            .Insert([
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(ShopMenuPatches).GetCachedMethod(nameof(IncreasedTabOffset), ReflectionCache.FlagTypes.StaticFlags)),
                new CodeInstruction(OpCodes.Add),
            ]);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    private static int IncreasedTabOffset(ShopMenu menu) => menu.tabButtons?.Count is > 0 ? 48 : 0;
}