using HarmonyLib;

using Microsoft.Xna.Framework;

using PrismaticSlime.Framework;

using StardewValley.Menus;

namespace PrismaticSlime.HarmonyPatches.SlimeToastPatches;

[HarmonyPatch(typeof(Buff))]
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
                scale: 4f));
        }
    }
}
