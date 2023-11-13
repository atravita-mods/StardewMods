using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;

using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewValley.Locations;
using StardewValley.Objects;

namespace StopRugRemoval.HarmonyPatches;

/// <summary>
/// Class that holds patch to allow jukeboxes to be played anywhere.
/// </summary>
[HarmonyPatch(typeof(MiniJukebox))]
internal static class JukeboxesEverywhere
{
    /// <summary>
    /// Whether or not jukeboxes should be playable at this location.
    /// </summary>
    /// <param name="location">Gamelocation.</param>
    /// <returns>whether the jukebox should be playable.</returns>
    public static bool ShouldPlayJukeBoxHere(GameLocation location)
        => ModEntry.Config.JukeboxesEverywhere
            || !string.IsNullOrWhiteSpace(location.miniJukeboxTrack.Value) // always allow turning the bloody thing off.
            || location is Cellar || location.IsFarm || location.IsGreenhouse || location is IslandWest;

    [HarmonyPatch(nameof(MiniJukebox.checkForAction))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
                {
                    SpecialCodeInstructionCases.LdLoc,
                    new (OpCodes.Callvirt, typeof(GameLocation).GetCachedProperty(nameof(GameLocation.IsFarm), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()),
                    new (OpCodes.Brtrue_S),
                })
                .Advance(1)
                .RemoveUntil(new CodeInstructionWrapper[]
                {
                    new (OpCodes.Brtrue_S),
                    new (OpCodes.Ldsfld),
                    new (OpCodes.Ldstr, "Strings\\UI:Mini_JukeBox_NotFarmPlay"),
                })
                .Insert(new CodeInstruction[]
                {
                    new (OpCodes.Call, typeof(JukeboxesEverywhere).StaticMethodNamed(nameof(JukeboxesEverywhere.ShouldPlayJukeBoxHere))),
                });

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