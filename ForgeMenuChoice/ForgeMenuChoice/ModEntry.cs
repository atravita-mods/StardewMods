using AtraBase.Toolkit.Reflection;
using AtraShared.Utils.Extensions;
using ForgeMenuChoice.HarmonyPatches;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace ForgeMenuChoice;

/// <inheritdoc/>
internal class ModEntry : Mod
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Gets the logger for this file.
    /// </summary>
    internal static IMonitor ModMonitor { get; private set; }

    /// <summary>
    /// Gets the content helper for this mod.
    /// </summary>
    internal static IContentHelper ContentHelper { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
        ContentHelper = helper.Content;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        => this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.PatchAll();

            if (this.Helper.ModRegistry.Get("spacechase0.SpaceCore") is not IModInfo spacecore)
            {
                this.Monitor.Log($"Spacecore not installed, compat patches unnecessary.", LogLevel.Debug);
            }
            else
            {
                if (AccessTools.TypeByName("SpaceCore.Interface.NewForgeMenu") is Type spaceforge)
                {
                    this.Monitor.Log($"Got spacecore's forge for compat patching.", LogLevel.Debug);
                    harmony.Patch(
                        original: spaceforge.InstanceMethodNamed("cleanupBeforeExit"),
                        prefix: new HarmonyMethod(typeof(ForgeMenuPatches), nameof(ForgeMenuPatches.PrefixBeforeExit)));
                    harmony.Patch(
                        original: spaceforge.InstanceMethodNamed("IsValidCraft"),
                        prefix: new HarmonyMethod(typeof(ForgeMenuPatches), nameof(ForgeMenuPatches.PrefixIsValidCraft)));
                    harmony.Patch(
                        original: spaceforge.InstanceMethodNamed("draw", new Type[] { typeof(SpriteBatch) }),
                        postfix: new HarmonyMethod(typeof(ForgeMenuPatches), nameof(ForgeMenuPatches.PostfixDraw)));
                    harmony.Patch(
                        original: spaceforge.InstanceMethodNamed("receiveLeftClick"),
                        postfix: new HarmonyMethod(typeof(ForgeMenuPatches), nameof(ForgeMenuPatches.PostFixLeftClick)));
                    harmony.Patch(
                        original: spaceforge.InstanceMethodNamed("receiveRightClick"),
                        postfix: new HarmonyMethod(typeof(ForgeMenuPatches), nameof(ForgeMenuPatches.PostfixRightClick)));
                    harmony.Patch(
                        original: spaceforge.InstanceMethodNamed("gameWindowSizeChanged"),
                        postfix: new HarmonyMethod(typeof(ForgeMenuPatches), nameof(ForgeMenuPatches.PostfixGameWindowSizeChanged)));
                }
                else
                {
                    this.Monitor.Log($"Failed to grab Spacecore for compat patching, this mod may not work.", LogLevel.Warn);
                }
            }
        }
        catch (Exception ex)
        {
            ModMonitor.Log($"Mod failed while applying patches:\n{ex}", LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, this.ModManifest.UniqueID);
    }
}