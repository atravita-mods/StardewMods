using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace ForgeMenuChoice.HarmonyPatches;

/// <summary>
/// Holds patches against the forge menu.
/// </summary>
/// <remarks>Also used to patch SpaceCore's forge menu.</remarks>
[HarmonyPatch(typeof(ForgeMenu))]
internal static class ForgeMenuPatches
{
    private static readonly List<BaseEnchantment> PossibleEnchantments = new();

    private static ForgeSelectionMenu? menu;

    /// <summary>
    /// Gets the current selected enchantment from the menu, if the menu exists.
    /// </summary>
    public static BaseEnchantment? CurrentSelection
        => menu?.CurrentSelectedOption;

    /// <summary>
    /// Exits and trashes the minimenu.
    /// </summary>
    internal static void TrashMenu()
    {
        menu?.exitThisMenu(false);
        menu = null;
    }

    /// <summary>
    /// Prefix before exiting menu - this closes the minimenu.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("cleanupBeforeExit")]
    internal static void PrefixBeforeExit() => TrashMenu();

    /// <summary>
    /// Prefixes IsValidCraft to gather possible enchantments.
    /// </summary>
    /// <param name="__0">Left item (tool slot).</param>
    /// <param name="__1">Right item (possibly prismatic).</param>
    /// <param name="__result">Result to feed to original function.</param>
    /// <returns>True to continue to original function, false otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.High)]
    [HarmonyPatch(nameof(ForgeMenu.IsValidCraft))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention")]
    internal static bool PrefixIsValidCraft(Item __0, Item __1, ref bool __result)
    {
        try
        {
            // 74 - prismatic shard.
            if (__0 is Tool tool && Utility.IsNormalObjectAtParentSheetIndex(__1, 74))
            {
                PossibleEnchantments.Clear();
                foreach (BaseEnchantment enchantment in BaseEnchantment.GetAvailableEnchantments())
                {
                    if (enchantment.CanApplyTo(tool) && !tool.enchantments.Contains(enchantment))
                    {
                        PossibleEnchantments.Add(enchantment);
                    }
                }
                if (PossibleEnchantments.Count > 0)
                {
                    menu ??= new(options: PossibleEnchantments);
                    __result = true;
                    return false;
                }
            }
            TrashMenu();
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Error in postfixing IsValidCraft:\n{ex}", LogLevel.Error);
        }
        return true;
    }

    /// <summary>
    /// Postfixes the forge menu's draw call to draw the smol menu.
    /// </summary>
    /// <param name="b">Spritebatch to draw with.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.draw))]
    internal static void PostfixDraw(SpriteBatch b)
    {
        try
        {
            menu?.draw(b);
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Ran into errors drawing the choice menu\n{ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Postfixes the forge menu's left click to also process left clicks for the smol menu.
    /// </summary>
    /// <param name="x">X location clicked.</param>
    /// <param name="y">Y location clicked.</param>
    /// <param name="playSound">Whether or not to play sounds.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.receiveLeftClick))]
    internal static void PostFixLeftClick(int x, int y, bool playSound)
        => menu?.receiveLeftClick(x, y, playSound);

    /// <summary>
    /// Postfixes the forge menu's right click to also process right clicks for the smol menu.
    /// </summary>
    /// <param name="x">X location clicked.</param>
    /// <param name="y">Y location clicked.</param>
    /// <param name="playSound">Whether or not to play sounds.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.receiveRightClick))]
    internal static void PostfixRightClick(int x, int y, bool playSound)
        => menu?.receiveRightClick(x, y, playSound);

    /// <summary>
    /// Postfixes the forge menu's resizing to also move the smol menu.
    /// </summary>
    /// <param name="oldBounds">Old boundaries.</param>
    /// <param name="newBounds">New boundaries.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.gameWindowSizeChanged))]
    internal static void PostfixGameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        => menu?.gameWindowSizeChanged(oldBounds, newBounds);
}