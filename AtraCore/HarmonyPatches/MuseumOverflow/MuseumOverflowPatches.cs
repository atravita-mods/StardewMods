using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewModdingAPI.Utilities;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;

namespace AtraCore.HarmonyPatches.MuseumOverflow;

/// <summary>
/// Holds patches to make museum overflow work.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class MuseumOverflowPatches
{
    /// <summary>
    /// The key for the museum inventory.
    /// </summary>
    private const string INVENTORY_NAME = "atravita.AtraCore.MuseumOverflow";

    private static readonly PerScreen<MuseumOverflowMenu?> menu = new();

    private static int GetDonatedLength => Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME).Count;

    #region menu patches

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), MethodType.Constructor, new[] { typeof(InventoryMenu.highlightThisItem) })]
    private static void PostfixMuseumConstruction(MuseumMenu __instance)
    {
        menu.Value = new(__instance, Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(InventoryMenu.draw))]
    private static void PrefixDraw(SpriteBatch b)
    {
        if (menu.Value is { } m)
        {
            m.draw(b);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), "cleanupBeforeExit")]
    private static void PostfixCleanup()
    {
        menu.Value = null;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.gameWindowSizeChanged))]
    private static void PostfixWindowSizeChange(Rectangle oldBounds, Rectangle newBounds)
    {
        if (menu.Value is { } m)
        {
            m.gameWindowSizeChanged(oldBounds, newBounds);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.update))]
    private static void PostfixUpdate(GameTime time)
    {
        if (menu.Value is { } m)
        {
            m.update(time);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.receiveLeftClick))]
    private static bool PrefixLeftClick(int x, int y, bool playSound)
    {
        try
        {
            if (menu.Value is { } m)
            {
                return !m.LeftClickImpl(x, y, playSound);
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError($"handling clicks for museum drawer", ex);
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.leftClickHeld))]
    private static void PostfixLeftClickHeld(IClickableMenu __instance, int x, int y)
    {
        if (__instance is MuseumMenu && menu.Value is { } m)
        {
            m.leftClickHeld(x, y);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(IClickableMenu), nameof(IClickableMenu.releaseLeftClick))]
    private static void ReleaseLeftClick(IClickableMenu __instance, int x, int y)
    {
        if (__instance is MuseumMenu && menu.Value is { } m)
        {
            m.releaseLeftClick(x, y);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MenuWithInventory), nameof(MenuWithInventory.performHoverAction))]
    private static void PostfixHoverAction(MenuWithInventory __instance, int x, int y)
    {
        if (__instance is MuseumMenu && menu.Value is { } m)
        {
            m.performHoverAction(x, y);
        }
    }

    #endregion

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.GetDonatedByContextTag))]
    private static void PostfixMuseumItems(Dictionary<string, int> __result)
    {
        try
        {
            Inventory storage = Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME);
            storage.RemoveEmptySlots();

            if (storage.Count == 0)
            {
                return;
            }

            ref int prev = ref CollectionsMarshal.GetValueRefOrAddDefault(__result, string.Empty, out _);
            prev += storage.Count;

            foreach (Item? item in storage)
            {
                foreach (string? tag in ItemContextTagManager.GetBaseContextTags(item.ItemId))
                {
                    if (__result.TryGetValue(tag, out var value))
                    {
                        __result[tag] = value + 1;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("adding storage items to museum", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.HasDonatedArtifact))]
    private static void PostfixHasDonated(string? itemId, ref bool __result)
    {
        if (!__result && itemId is not null)
        {
            var qualified = ItemRegistry.ManuallyQualifyItemId(itemId, ItemRegistry.type_object);
            var inventory = Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME);

            if (inventory.Any(item => item.QualifiedItemId == qualified))
            {
                __result = true;
                return;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MuseumMenu), nameof(MuseumMenu.receiveLeftClick))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext([
                // int length = museum.museumPieces.Length;
                new(SpecialCodeInstructionCases.LdLoc),
                (OpCodes.Callvirt, typeof(LibraryMuseum).GetCachedProperty(nameof(LibraryMuseum.museumPieces), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                OpCodes.Callvirt,
                new(SpecialCodeInstructionCases.StLoc),
            ])
            .Advance(3)
            .Insert([
                new(OpCodes.Call, typeof(MuseumOverflowPatches).GetCachedProperty(nameof(GetDonatedLength), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(true)),
                new(OpCodes.Add),
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

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Stats), nameof(Stats.checkForArchaeologyAchievements))]
    private static IEnumerable<CodeInstruction>? TranspileStats(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext([
                OpCodes.Dup,
                (OpCodes.Call, typeof(LibraryMuseum).GetCachedProperty(nameof(LibraryMuseum.totalArtifacts), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                OpCodes.Blt_S,
            ])
            .Advance(1)
            .Insert([
                new(OpCodes.Call, typeof(MuseumOverflowPatches).GetCachedProperty(nameof(GetDonatedLength), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod(true)),
                new(OpCodes.Add),
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