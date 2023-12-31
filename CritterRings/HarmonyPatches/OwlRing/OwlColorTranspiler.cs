using System.Reflection;
using System.Reflection.Emit;

using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using StardewValley.BellsAndWhistles;

namespace CritterRings.HarmonyPatches.OwlRing;

/// <summary>
/// Patches owls to try to see their pretty sprite in the day.
/// </summary>
[HarmonyPatch(typeof(Owl))]
internal static class OwlColorTranspiler
{
    [MethodImpl(TKConstants.Hot)]
    private static Color GetColorForTime(Color prevcolor)
    {
        if (Game1.isDarkOut(Game1.currentLocation))
        {
            return prevcolor;
        }
        if (Game1.isStartingToGetDarkOut(Game1.currentLocation))
        {
            return Color.LightSteelBlue;
        }
        return Color.White;
    }

    [HarmonyPatch(nameof(Owl.drawAboveFrontLayer))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(
            [
                (OpCodes.Call, typeof(Color).GetCachedProperty(nameof(Color.MediumBlue), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
            ])
            .Advance(1)
            .Insert(
            [
                new (OpCodes.Call, typeof(OwlColorTranspiler).GetCachedMethod(nameof(GetColorForTime), ReflectionCache.FlagTypes.StaticFlags)),
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
