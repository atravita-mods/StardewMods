﻿using AtraShared.Niceties;
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
    private static bool Prefix(string name, ref bool __result)
    {
        if (Context.IsWorldReady && !name.Equals("random", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("turn_off", StringComparison.OrdinalIgnoreCase) && !name.Equals("title_day", StringComparison.OrdinalIgnoreCase))
        {
            if (Game1.soundBank is SoundBankWrapper soundBank)
            {
                try
                {
                    SoundBank? soundBankImpl = SoundBankWrapperHandler.GetActualSoundBank(soundBank);
                    if (!SoundBankWrapperHandler.HasCue(soundBankImpl, name))
                    {
                        ModEntry.ModMonitor.Log($"Overwriting IsValidJukeboxSong for invalid cue {name}");
                        __result = false;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"Failed in checking jukebox songs for invalid cues for cue {name}\n\n{ex}", LogLevel.Error);
                }
            }
            else
            {
                ModEntry.ModMonitor.LogOnce($"Stardew's implementation of soundbank seems to have changed since I wrote this. Please report this as an error to the mod page.", LogLevel.Error);
            }
        }
        return true;
    }
}
