using System.Runtime.CompilerServices;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Netcode;

using StardewValley.Characters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Projectiles;

namespace ExperimentalLagReduction.HarmonyPatches;

/// <summary>
/// Patches to disable the draw functions when the target isn't even close to being on screen.
/// </summary>
[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DrawCulling
{
    #region delegates
    private static Lazy<Func<Furniture, NetVector2>> _getDrawPosition = new(() =>
        typeof(Furniture).GetCachedField("drawPosition", ReflectionCache.FlagTypes.InstanceFlags)
                         .GetInstanceFieldGetter<Furniture, NetVector2>());

    private static Lazy<Func<Projectile, NetPosition>> _getProjectilePosition = new(() =>
        typeof(Projectile).GetCachedField("position", ReflectionCache.FlagTypes.InstanceFlags)
                          .GetInstanceFieldGetter<Projectile, NetPosition>());
    #endregion

    [HarmonyPrefix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Furniture), nameof(Furniture.draw))]
    private static bool PrefixFurnitureDraw(Furniture __instance)
    {
        if (!Furniture.isDrawingLocationFurniture || !ModEntry.Config.CullDraws)
        {
            return true;
        }
        Vector2 pos = _getDrawPosition.Value(__instance).Value;
        Rectangle box = __instance.boundingBox.Value;

        return Utility.isOnScreen(pos, 128)
            || Utility.isOnScreen(new Vector2(box.X, box.Y), 128)
            || Utility.isOnScreen(new Vector2(box.Right, box.Bottom), 128);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(FarmAnimal), nameof(FarmAnimal.draw))]
    private static bool PrefixFarmAnimalDraw(FarmAnimal __instance)
        => !ModEntry.Config.CullDraws || Utility.isOnScreen(__instance.Position, 256);

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(JunimoHarvester), nameof(JunimoHarvester.draw))]
    private static bool PrefixJunimoDraw(JunimoHarvester __instance)
        => !ModEntry.Config.CullDraws || Utility.isOnScreen(__instance.Position, 256);

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Character), nameof(Character.draw), new[] { typeof(SpriteBatch), typeof(float) })]
    private static bool PrefixCharacterDraw(Character __instance, float alpha)
        => !ModEntry.Config.CullDraws || (alpha > 0f && Utility.isOnScreen(__instance.Position, 256));

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.draw), new[] { typeof(SpriteBatch)})]
    private static bool PrefixProjectileDraw(Projectile __instance)
        => !ModEntry.Config.CullDraws
            || Utility.isOnScreen(_getProjectilePosition.Value(__instance).Value, 256);
}
