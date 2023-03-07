using HarmonyLib;

using Microsoft.Xna.Framework;

using PrismaticSlime.Framework;

using StardewValley.Menus;

namespace PrismaticSlime.HarmonyPatches.SlimeToastPatches;

/// <summary>
/// Holds a patch to give our buff a custom icon.
/// </summary>
[HarmonyPatch(typeof(Buff))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
internal static class BuffPatches
{
    [HarmonyPatch(nameof(Buff.getClickableComponents))]
    private static void Postfix(Buff __instance, List<ClickableTextureComponent> __result)
    {
        if (__instance.which == FarmerPatches.BuffId)
        {
            __result.Clear();
            __result.Add(new ClickableTextureComponent(
                name: string.Empty,
                bounds: Rectangle.Empty,
                label: null,
                hoverText: __instance.getDescription(__instance.which),
                texture: AssetManager.BuffTexture,
                new Rectangle(0, 0, 16, 16),
                scale: Game1.pixelZoom));
        }
    }
}
