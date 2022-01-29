﻿using System.Reflection;
using System.Text;
using GingerIslandMainlandAdjustments.CustomConsoleCommands;
using GingerIslandMainlandAdjustments.DialogueChanges;
using GingerIslandMainlandAdjustments.Integrations;
using GingerIslandMainlandAdjustments.ScheduleManager;
using GingerIslandMainlandAdjustments.Utils;
using HarmonyLib;
using StardewModdingAPI.Events;

namespace GingerIslandMainlandAdjustments;

/// <inheritdoc />
public class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        Globals.Initialize(helper, this.Monitor);

        ConsoleCommands.Register(this.Helper.ConsoleCommands);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        // Register events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.TimeChanged += MidDayScheduleEditor.AttemptAdjustGISchedule;
        helper.Events.GameLoop.DayEnding += this.DayEnding;
        helper.Events.GameLoop.ReturnedToTitle += this.ReturnedToTitle;

        helper.Events.Input.ButtonPressed += this.Input_ButtonPressed;

        helper.Events.Player.Warped += this.OnPlayerWarped;

        AssetManager manager = new();
        // Add my asset manager
        helper.Content.AssetLoaders.Add(manager);
        helper.Content.AssetEditors.Add(manager);
    }

    private void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        SandyShop.AddBoxToShop(e);
    }

    private void Input_ButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        SandyShop.HandleShop(e);
    }

    /// <summary>
    /// Clear caches when returning to title.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Possible parameters.</param>
    private void ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        MidDayScheduleEditor.Reset();
        IslandSouthPatches.ClearCache();
        GIScheduler.ClearCache();
        DialogueUtilities.ClearDialogueLog();
    }

    /// <summary>
    /// Clear cache at day end.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Possible parameters.</param>
    private void DayEnding(object? sender, DayEndingEventArgs e)
    {
        MidDayScheduleEditor.Reset();
        IslandSouthPatches.ClearCache();
        GIScheduler.ClearCache();
        DialogueUtilities.ClearDialogueLog();
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        // handle patches from annotations.
        harmony.PatchAll();
        foreach (MethodBase? method in harmony.GetPatchedMethods())
        {
            if (method is null)
            {
                continue;
            }
            Patches patches = Harmony.GetPatchInfo(method);

            StringBuilder sb = new();
            sb.Append("Patched method ").Append(method.GetFullName());
            foreach (Patch patch in patches.Prefixes.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tPrefixed with method: ").Append(patch.PatchMethod.GetFullName());
            }
            foreach (Patch patch in patches.Postfixes.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tPostfixed with method: ").Append(patch.PatchMethod.GetFullName());
            }
            foreach (Patch patch in patches.Transpilers.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tTranspiled with method: ").Append(patch.PatchMethod.GetFullName());
            }
            foreach (Patch patch in patches.Finalizers.Where((Patch p) => p.owner.Equals(this.ModManifest.UniqueID)))
            {
                sb.AppendLine().Append("\tFinalized with method: ").Append(patch.PatchMethod.GetFullName());
            }
            Globals.ModMonitor.Log(sb.ToString(), LogLevel.Trace);
        }
    }

    /// <summary>
    /// Initialization after other mods have started.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Possible parameters.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Generate the GMCM for this mod.
        GenerateGMCM.Build(this.ModManifest);
    }
}
