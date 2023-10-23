using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using StardewValley.Menus;

namespace NovaNPCTest.HarmonyPatches;

[HarmonyPatch(typeof(DialogueBox))]
internal static class DialogueBoxPatcher
{
    [HarmonyPatch("shouldPortraitShake")]
    private static bool Prefix(Dialogue d, ref bool __result)
    {
        if (ModEntry.PortraitsToShake?.TryGetValue(d.speaker.Name, out int[]? shakes) == true
            && shakes.Contains(d.getPortraitIndex()))
        {
            __result = true;
            return false;
        }

        return true;
    }
}
