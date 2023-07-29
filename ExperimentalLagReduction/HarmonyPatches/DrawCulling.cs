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
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace ExperimentalLagReduction.HarmonyPatches;

[HarmonyPatch]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class DrawCulling
{
    #region delegates
    private static Lazy<Func<Furniture, NetVector2>> _getDrawPosition = new(() =>
        typeof(Furniture).GetCachedField("drawPosition", ReflectionCache.FlagTypes.InstanceFlags)
                         .GetInstanceFieldGetter<Furniture, NetVector2>());
    #endregion

    [HarmonyPrefix]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Furniture), nameof(Furniture.draw))]
    private static bool PrefixFurnitureDraw(Furniture __instance, int x, int y)
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
    [HarmonyPatch(typeof(Bush), nameof(Bush.draw), new[] { typeof(SpriteBatch), typeof(Vector2) } )]
    private static bool PrefixBushDraw(Bush __instance, Vector2 tileLocation)
    {
        if (!ModEntry.Config.CullDraws)
        {
            return true;
        }

        Vector2 effectivePosition = tileLocation + (Vector2.UnitX * (__instance.size / 2f));

        return Utility.isOnScreen(effectivePosition, 256);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [MethodImpl(TKConstants.Hot)]
    [HarmonyPatch(typeof(Character), nameof(Character.draw), new[] { typeof(SpriteBatch), typeof(float) })]
    private static bool PrefixCharacterDraw(Character __instance, float alpha)
    {
        if (!ModEntry.Config.CullDraws)
        {
            return true;
        }
        return alpha > 0f && Utility.isOnScreen(__instance.Position, 256);
    }
}
