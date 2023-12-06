namespace DresserMiniMenu.HarmonyPatches;

using System.Reflection;
using System.Reflection.Emit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.Menus;

/// <summary>
/// Makes it so the held item is set correctly.
/// </summary>
[HarmonyPatch]
internal static class FixHeldItemTranspiler
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return typeof(ShopMenu).GetCachedMethod(nameof(ShopMenu.receiveLeftClick), ReflectionCache.FlagTypes.InstanceFlags);
        yield return typeof(ShopMenu).GetCachedMethod(nameof(ShopMenu.receiveRightClick), ReflectionCache.FlagTypes.InstanceFlags);
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindLast([ // this._isStorageShop
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(ShopMenu).GetCachedField("_isStorageShop", ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Brtrue_S,
            ])
            .Push()
            .Advance(3)
            .DefineAndAttachLabel(out Label jumpPast)
            .Pop()
            .Insert(
            [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Call, typeof(DresserMenuDoll).GetCachedMethod<ShopMenu>(nameof(DresserMenuDoll.IsActive), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Brtrue_S, jumpPast),
            ]);

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
