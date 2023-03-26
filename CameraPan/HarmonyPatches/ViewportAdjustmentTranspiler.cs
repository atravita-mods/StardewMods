using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using StardewValley.TerrainFeatures;
using AtraShared.Utils.Extensions;
using Microsoft.Xna.Framework;

namespace CameraPan.HarmonyPatches;

/// <summary>
/// Adjusts the viewport based on the offset vector.
/// </summary>
internal static class ViewportAdjustmentTranspiler
{
    private static bool IsInEvent()
        => Game1.CurrentEvent is Event evt && (evt.farmer is not null && !evt.isFestival);



    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.FullDescription()}:\n\n{ex}", LogLevel.Error);
            original.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
}
