﻿using AtraShared.ConstantsAndEnums;
using AtraShared.Niceties;
using AtraShared.Utils.Extensions;

using HarmonyLib;

using Microsoft.Xna.Framework.Audio;

using StardewValley.Menus;

namespace StopRugRemoval.HarmonyPatches.Niceties.CrashHandling;

/// <summary>
/// Prevents a deleted cue from breaking the jukebox.
/// </summary>
[HarmonyPatch(typeof(ChooseFromListMenu))]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = StyleCopConstants.NamedForHarmony)]
internal static class PreventJukeboxCrash
{
    [HarmonyPatch(nameof(ChooseFromListMenu.IsValidJukeboxSong))]
    private static bool Prefix(string name, ref bool __result)
    {
        if (Context.IsWorldReady && ModEntry.Config.FilterJukeboxSongs && !name.Equals("random", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("turn_off", StringComparison.OrdinalIgnoreCase) && !name.Equals("title_day", StringComparison.OrdinalIgnoreCase)
            && Game1.soundBank is not DummySoundBank)
        {
            if (Game1.soundBank is SoundBankWrapper soundBank)
            {
                try
                {
                    SoundBank? soundBankImpl = SoundBankWrapperHandler.GetActualSoundBank(soundBank);
                    if (!SoundBankWrapperHandler.HasCue(soundBankImpl, name))
                    {
                        ModEntry.ModMonitor.Log($"Overwriting IsValidJukeboxSong for invalid cue {name}", LogLevel.Debug);
                        __result = false;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.LogError($"Failed in checking jukebox songs for invalid cues for cue {name}", ex);
                }
            }
            else
            {
                ModEntry.ModMonitor.LogOnce($"Stardew's implementation of soundbank seems to have changed since I wrote this: {Game1.soundBank.GetType()}", LogLevel.Debug);
            }
        }
        return true;
    }
}
