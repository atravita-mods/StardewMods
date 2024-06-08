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

using StardewModdingAPI.Utilities;

using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;

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

    /// <summary>
    /// Attempts to get the inventory that museum overflow items are storied in.
    /// </summary>
    /// <param name="inventory">The inventory.</param>
    /// <returns>true if found, false otherwise.</returns>
    internal static bool TryGetInventory([NotNullWhen(true)] out Inventory? inventory)
    {
        if (Game1.player.team.globalInventories.TryGetValue(INVENTORY_NAME, out inventory))
        {
            inventory.RemoveEmptySlots();
            if (inventory.Count > 0)
            {
                return true;
            }

            inventory = null;
            return false;
        }
        return false;
    }

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MuseumMenu), "cleanupBeforeExit")]
    private static void PrefixCleanup(MuseumMenu __instance)
    {
        menu.Value = null;
        if (MuseumOverflowMenu._holdingMuseumPieceGetter.Value(__instance) && __instance.heldItem is { } item && item.Stack == 1)
        {
            Game1.showGlobalMessage(I18n.MuseumReturned(item.DisplayName));
            Game1.player.team.GetOrCreateGlobalInventory(INVENTORY_NAME).Add(item);
            __instance.heldItem = null;
        }
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
    [HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.HasDonatedArtifacts))]
    private static void PostfixHasDonatedItems(ref bool __result)
    {
        if (!__result && TryGetInventory(out Inventory? storage))
        {
            if (storage.Count > 0)
            {
                __result = true;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LibraryMuseum), nameof(LibraryMuseum.GetDonatedByContextTag))]
    private static void PostfixMuseumItems(Dictionary<string, int> __result)
    {
        try
        {
            if (!TryGetInventory(out Inventory? storage))
            {
                return;
            }

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
                    if (__result.TryGetValue(tag, out int value))
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
        if (!__result && itemId is not null && TryGetInventory(out Inventory? inventory))
        {
            string qualified = ItemRegistry.ManuallyQualifyItemId(itemId, ItemRegistry.type_object);
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

    private static int FromDrawerQueries(string[] query, int filterIndex)
    {
        if (!TryGetInventory(out Inventory? inventory))
        {
            return 0;
        }

        if (query.Length <= filterIndex)
        {
            return inventory.Count;
        }

        int count = 0;
        foreach (Item? item in inventory)
        {
            ParsedItemData data = ItemRegistry.GetDataOrErrorItem(item.ItemId);
            if (data.IsErrorItem || data?.ObjectType is null)
            {
                continue;
            }
            for (int i = filterIndex; i < query.Length; i++)
            {
                if (data.ObjectType == query[i])
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameStateQuery.DefaultResolvers), nameof(GameStateQuery.DefaultResolvers.MUSEUM_DONATIONS))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Preference.")]
    private static IEnumerable<CodeInstruction>? TranspileGSQ(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext([
                OpCodes.Ldc_I4_3,
                new(SpecialCodeInstructionCases.StLoc),
            ])
            .Advance(1);

            CodeInstruction filterIndex = helper.CurrentInstruction.ToLdLoc();

            helper.FindLast([
                new(SpecialCodeInstructionCases.LdLoc),
                new(SpecialCodeInstructionCases.LdLoc),
                OpCodes.Blt_S,
            ]);

            CodeInstruction ldCount = helper.CurrentInstruction.Clone();
            CodeInstruction stCount = helper.CurrentInstruction.ToStLoc();

            helper.GetLabels(out IList<Label>? labelsToMove)
                .Insert([
                new(OpCodes.Ldarg_0), // query
                filterIndex,
                new(OpCodes.Call, typeof(MuseumOverflowPatches).GetCachedMethod(nameof(FromDrawerQueries), ReflectionCache.FlagTypes.StaticFlags)),
                ldCount,
                new(OpCodes.Add),
                stCount,
            ], withLabels: labelsToMove);

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