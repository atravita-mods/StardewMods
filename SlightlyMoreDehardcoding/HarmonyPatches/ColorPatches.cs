
using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.Extensions;

namespace SlightlyMoreDehardcoding.HarmonyPatches;

internal class ColorPatches
{
    private const string colorCache = "atravita.ColorCache";

    /// <summary>
    /// Applies harmony patches for this class.
    /// </summary>
    /// <param name="harmony">Harmony instance.</param>
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(ItemContextTagManager), nameof(ItemContextTagManager.GetColorFromTags)),
            prefix: new(AccessTools.Method(typeof(ColorPatches), nameof(Prefix)), priority: Priority.VeryLow),
            postfix: new(AccessTools.Method(typeof(ColorPatches), nameof(Postfix)), priority: Priority.Last));
    }

    private static bool Prefix(Item item, ref Color? __result, out bool __state)
    {
        if (item.TryGetTempData(colorCache, out Color? color))
        {
            __result = color;
            __state = true;
            return false;
        }

        __state = false;
        return true;
    }

    private static void Postfix(Item item, ref Color? __result, bool __state)
    {
        if (!__state)
        {
            foreach (var tag in item.GetContextTags())
            {
                if (tag.StartsWithIgnoreCase("color_"))
                {
                    string trimmed = tag["color_".Length..];
                    if ((__result is null || trimmed.Any(char.IsDigit))
                        && Utility.StringToColor(trimmed) is Color c)
                    {
                        __result = c;
                    }
                }
            }
        }

        item.SetTempData(colorCache, __result);
    }
}