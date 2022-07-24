using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.StringHandler;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Niceties;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;
internal static class FixBirthdayGifts
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony convention.")]
    internal static Exception? FinalizeGiftSelection(Exception __exception, ref SObject? __result, NPC __instance)
    {
        if (__exception is not null)
        {
            ModEntry.ModMonitor.Log($"{__instance.Name}'s birthday gift seems invalid, original exception {__exception}", LogLevel.Trace);
            if (Game1.NPCGiftTastes.TryGetValue(__instance.Name, out string? likes))
            {
                ReadOnlySpan<char> loves = likes.GetNthChunk('/', 1);
                foreach (var seg in loves.StreamSplit(options:StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (int.TryParse(seg, out var val) && val > 0)
                    {
                        __result = new SObject(val, 1);
                        return null;
                    }
                }
            }

            ModEntry.ModMonitor.Log($"Failed to find replacement gift for {__instance.Name}, surpressing original exception {__exception}.", LogLevel.Error);
            __result = null;
        }
        return null;
    }
}
