using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using DresserMiniMenu.Framework;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Utilities;

using StardewValley.Menus;

namespace DresserMiniMenu.HarmonyPatches;

/// <summary>
/// Holds patches against ShopMenu to make minimenu a thing.
/// </summary>
[HarmonyPatch(typeof(ShopMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DresserMenuDoll
{
    private static readonly PerScreen<MiniFarmerMenu?> MiniMenu = new();

    [MethodImpl(TKConstants.Hot)]
    private static bool IsActive(ShopMenu instance, [NotNullWhen(true)] out MiniFarmerMenu? current)
    {
        if (MiniMenu.Value?.ShopMenu is not null && ReferenceEquals(instance, MiniMenu.Value?.ShopMenu))
        {
            current = MiniMenu.Value;
            return true;
        }
        current = null;
        return false;
    }

    private static bool IsActive(ShopMenu instance) => IsActive(instance, out _);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.setUpStoreForContext))]
    private static void PostfixSetup(ShopMenu __instance)
    {
        try
        {
            if (!ModEntry.Config.DresserDressup)
            {
                MiniMenu.Value = null;
            }
            else if (__instance.storeContext == "Dresser")
            {
                MiniMenu.Value = new(__instance);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("setting up mini dresser menu", ex);
        }
    }

    private static void DrawMenu(ShopMenu __instance, SpriteBatch b)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini))
        {
            try
            {
                mini.draw(b);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("drawing mini farmer menu", ex);
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ShopMenu.draw))]
    private static IEnumerable<CodeInstruction>? TranspileDraw(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(ShopMenu).GetCachedField(nameof(ShopMenu.inventory), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Ldarg_1,
                (OpCodes.Callvirt, typeof(IClickableMenu).GetCachedMethod<SpriteBatch>(nameof(IClickableMenu.draw), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .Advance(4)
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldarg_1),
                new(OpCodes.Call, typeof(DresserMenuDoll).GetCachedMethod(nameof(DrawMenu), ReflectionCache.FlagTypes.StaticFlags)),
            });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.gameWindowSizeChanged))]
    private static void PostfixResize(ShopMenu __instance, Rectangle oldBounds, Rectangle newBounds)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini))
        {
            try
            {
                mini.gameWindowSizeChanged(oldBounds, newBounds);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("changing window size for mini farmer menu", ex);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShopMenu.performHoverAction))]
    private static void PostfixHover(ShopMenu __instance, int x, int y)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini))
        {
            try
            {
                mini.performHoverAction(x, y);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("hovering for mini menu", ex);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch("cleanupBeforeExit")]
    private static void PrefixShutdown(ShopMenu __instance)
    {
        if (MiniMenu.Value?.ShopMenu is not null && ReferenceEquals(__instance, MiniMenu.Value.ShopMenu))
        {
            try
            {
                MiniMenu.Value.exitThisMenu();
                MiniMenu.Value = null;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.LogError("cleaning up smol menu", ex);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(ShopMenu.receiveLeftClick))]
    private static bool PrefixRecieveClick(ShopMenu __instance, int x, int y, bool playSound)
    {
        if (IsActive(__instance, out MiniFarmerMenu? mini) && mini.isWithinBounds(x, y))
        {
            mini.receiveLeftClick(x, y, playSound);
            return false;
        }
        return true;
    }
}
