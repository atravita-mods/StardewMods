using AtraShared.ConstantsAndEnums;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

using StardewValley.Enchantments;
using StardewValley.Menus;
using StardewValley.Tools;

namespace ForgeMenuChoice.HarmonyPatches;

/// <summary>
/// Holds patches against the forge menu.
/// </summary>
/// <remarks>Also used to patch SpaceCore's forge menu.</remarks>
[HarmonyPatch(typeof(ForgeMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class ForgeMenuPatches
{
    private static readonly PerScreen<List<BaseEnchantment>> PossibleEnchantmentPerscreen = new(() => new());
    private static readonly PerScreen<ForgeSelectionMenu?> MenuPerscreen = new();
    private static readonly PerScreen<int> LastButtonPressTicks = new(() => 0);

    /// <summary>
    /// Gets the current selected enchantment from the menu, if the menu exists.
    /// </summary>
    public static BaseEnchantment? CurrentSelection
        => Menu?.CurrentSelectedOption;

    private static List<BaseEnchantment> PossibleEnchantments => PossibleEnchantmentPerscreen.Value;

    private static ForgeSelectionMenu? Menu
    {
        get => MenuPerscreen.Value;
        set => MenuPerscreen.Value = value;
    }

    /// <summary>
    /// Handles adjusting the menu in response to button presses.
    /// </summary>
    /// <param name="e">Buttons.</param>
    internal static void ApplyButtonPresses(ButtonsChangedEventArgs e)
    {
        if (Menu is null || LastButtonPressTicks.Value + 15 < Game1.ticks)
        {
            return;
        }
        LastButtonPressTicks.Value = Game1.ticks;
        if (ModEntry.Config.LeftArrow.JustPressed())
        {
            Menu.RetreatPosition(playSound: true);
            ModEntry.InputHelper.SuppressActiveKeybinds(ModEntry.Config.LeftArrow);
        }
        else if (ModEntry.Config.RightArrow.JustPressed())
        {
            Menu.AdvancePosition(playSound: true);
            ModEntry.InputHelper.SuppressActiveKeybinds(ModEntry.Config.RightArrow);
        }
    }

    /// <summary>
    /// Exits and trashes the minimenu.
    /// </summary>
    internal static void TrashMenu()
    {
        Menu?.exitThisMenu(false);
        Menu = null;
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
    internal static bool PrefixIsValidCraft(Item __0, Item __1, ref bool __result, ForgeMenu __instance)
    {
        try
        {
            // 74 - prismatic shard.
            if (__0 is Tool tool && __1.QualifiedItemId == "(O)74")
            {
                if (Menu is not null && !Menu.IsInnate && ReferenceEquals(Menu.Tool, tool))
                {
                    __result = true;
                    return false;
                }
                PossibleEnchantments.Clear();
                HashSet<Type> enchants = tool.enchantments.Select(static a => a.GetType()).ToHashSet();
                foreach (BaseEnchantment enchantment in BaseEnchantment.GetAvailableEnchantments())
                {
                    if (enchantment.CanApplyTo(tool) && !enchants.Contains(enchantment.GetType()))
                    {
                        PossibleEnchantments.Add(enchantment);
                    }
                }
                if (PossibleEnchantments.Count > 0)
                {
                    Menu = new(options: PossibleEnchantments, tool, false);
                    __result = true;
                    return false;
                }
            }
            else if (ModEntry.Config.OverrideInnateEnchantments && ReferenceEquals(__1, __instance.rightIngredientSpot.item)
                && __0 is MeleeWeapon weapon && weapon.getItemLevel() < 15 && __1.QualifiedItemId == "(O)852" && !weapon.Name.Contains("Galaxy"))
            {
                if (Menu is not null && Menu.IsInnate && ReferenceEquals(Menu.Tool, weapon))
                {
                    __result = true;
                    return false;
                }
                PossibleEnchantments.Clear();
                foreach (BaseEnchantment enchantment in weapon.GetInnateEnchantments())
                {
                    PossibleEnchantments.Add(enchantment);
                }
                if (PossibleEnchantments.Count > 0)
                {
                    Menu = new(PossibleEnchantments, weapon, true);
                    __result = true;
                    return false;
                }
            }
            TrashMenu();
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("postfixing IsValidCraft", ex);
        }
        return true;
    }

    // Derived from attemptAddRandomInnateEnchantment
    private static IEnumerable<BaseEnchantment> GetInnateEnchantments(this MeleeWeapon weapon)
    {
        int weaponLevel = weapon.getItemLevel();
        if (weaponLevel <= 10)
        {
            yield return new DefenseEnchantment()
            {
                Level = Math.Clamp((Random.Shared.Next(weaponLevel + 1) / 2) + 1, 1, 2),
            };
        }

        yield return new LightweightEnchantment()
        {
            Level = Random.Shared.Next(1, 6),
        };

        yield return new SlimeGathererEnchantment();

        yield return new AttackEnchantment()
        {
            Level = Math.Clamp((Random.Shared.Next(weaponLevel + 1) / 2) + 1, 1, 5),
        };

        yield return new CritEnchantment
        {
            Level = Math.Clamp(Random.Shared.Next(weaponLevel) / 3, 1, 3),
        };

        yield return new WeaponSpeedEnchantment
        {
            Level = Math.Max(1, Math.Min(Math.Max(1, 4 - weapon.speed.Value), Random.Shared.Next(weaponLevel))),
        };

        yield return new SlimeSlayerEnchantment();

        yield return new CritPowerEnchantment()
        {
            Level = Math.Clamp(Random.Shared.Next(weaponLevel) / 3, 1, 3),
        };
    }

    /// <summary>
    /// Postfixes the forge menu's draw call to draw the smol menu.
    /// </summary>
    /// <param name="b">Spritebatch to draw with.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.draw))]
    internal static void PostfixDraw(SpriteBatch b)
        => Menu?.draw(b);

    /// <summary>
    /// Postfixes the forge menu's left click to also process left clicks for the smol menu.
    /// </summary>
    /// <param name="x">X location clicked.</param>
    /// <param name="y">Y location clicked.</param>
    /// <param name="playSound">Whether or not to play sounds.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.receiveLeftClick))]
    internal static void PostFixLeftClick(int x, int y, bool playSound)
        => Menu?.receiveLeftClick(x, y, playSound);

    /// <summary>
    /// Postfixes the forge menu's right click to also process right clicks for the smol menu.
    /// </summary>
    /// <param name="x">X location clicked.</param>
    /// <param name="y">Y location clicked.</param>
    /// <param name="playSound">Whether or not to play sounds.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.receiveRightClick))]
    internal static void PostfixRightClick(int x, int y, bool playSound)
        => Menu?.receiveRightClick(x, y, playSound);

    /// <summary>
    /// Postfixes the forge menu's resizing to also move the smol menu.
    /// </summary>
    /// <param name="oldBounds">Old boundaries.</param>
    /// <param name="newBounds">New boundaries.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.gameWindowSizeChanged))]
    internal static void PostfixGameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        => Menu?.gameWindowSizeChanged(oldBounds, newBounds);

    /// <summary>
    /// Postfixes the forge menu's hovering to handle hovering in the smol menu.
    /// </summary>
    /// <param name="x">Pixel hovered over (X).</param>
    /// <param name="y">Pixel hovered over (Y).</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ForgeMenu.performHoverAction))]
    internal static void PostfixPerformHoverAction(int x, int y)
        => Menu?.performHoverAction(x, y);
}