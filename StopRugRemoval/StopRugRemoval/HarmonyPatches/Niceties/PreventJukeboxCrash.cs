using AtraShared.Niceties;
using HarmonyLib;
using Microsoft.Xna.Framework.Audio;
using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties;

/// <summary>
/// Prevents a deleted cue from breaking the jukebox.
/// </summary>
[HarmonyPatch(typeof(ChooseFromListMenu))]
internal static class PreventJukeboxCrash
{
    [HarmonyPatch(nameof(ChooseFromListMenu.IsValidJukeboxSong))]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony Convention")]
    private static void Postfix(string name, ref bool __result)
    {
        if (__result)
        {
            if (Game1.soundBank is SoundBankWrapper soundBank)
            {
                SoundBank? soundBankImpl = SoundBankWrapperHandler.GetActualSoundBank(soundBank);
                if (!SoundBankWrapperHandler.HasCue(soundBankImpl, name))
                {
                    ModEntry.ModMonitor.Log($"Overwriting IsValidJukeboxSong for invalid cue {name}");
                    __result = false;
                }
            }
            else
            {
                ModEntry.ModMonitor.LogOnce($"Stardew's implementation of soundbank seems to have changed since I wrote this. Please report this as an error to the mod page.", LogLevel.Error);
            }
        }
    }
}
