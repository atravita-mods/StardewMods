using System.Reflection;
using System.Reflection.Emit;
using AtraBase.Toolkit.Extensions;
using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace ExperimentalLagReduction.HarmonyPatches;

internal static class RedirectToLazyLoad
{
    /// <summary>
    /// Applies these patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    internal static void ApplyPatches(Harmony harmony)
    {
        if (!ModEntry.Config.ForceLazyTextureLoad)
        {
            return;
        }

        HarmonyMethod harmonyMethod = new(typeof(RedirectToLazyLoad).StaticMethodNamed(nameof(Transpiler)));

        if (typeof(Game1).GetCachedMethod<NPC, GameLocation, Vector2>(nameof(Game1.warpCharacter), ReflectionCache.FlagTypes.StaticFlags) is MethodBase warpChar)
        {
            harmony.Patch(warpChar, transpiler: harmonyMethod);
        }
        if (typeof(GameLocation).GetCachedMethod(nameof(GameLocation.cleanupBeforePlayerExit), ReflectionCache.FlagTypes.InstanceFlags) is MethodBase beforePlayerExit)
        {
            harmony.Patch(beforePlayerExit, transpiler: harmonyMethod);
        }
        if (typeof(NPC).GetCachedMethod(nameof(NPC.wearNormalClothes), ReflectionCache.FlagTypes.InstanceFlags) is MethodBase wearNormal)
        {
            harmony.Patch(wearNormal, transpiler: harmonyMethod);
        }

        harmony.Snitch(ModEntry.ModMonitor, harmony.Id, true);
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new (original, instructions, ModEntry.ModMonitor, gen);

            helper.ForEachMatch(
                new CodeInstructionWrapper[]
                {
                    new(OpCodes.Callvirt, typeof(AnimatedSprite).GetCachedMethod(nameof(AnimatedSprite.LoadTexture), ReflectionCache.FlagTypes.InstanceFlags)),
                },
                (helper) =>
                {
                    helper.ReplaceInstruction(
                        instruction: new CodeInstruction(OpCodes.Call, typeof(RedirectToLazyLoad).GetCachedMethod(nameof(ResetSprite), ReflectionCache.FlagTypes.StaticFlags)),
                        keepLabels: true);
                    return true;
                });

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling {original.GetFullName()}\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }

        return null;
    }

    /// <summary>
    /// A call that requests the sprite be reset WITHOUT reloading it immediately.
    /// </summary>
    /// <param name="sprite">sprite to reset.</param>
    /// <param name="textureName">Name of the new texture.</param>
    private static void ResetSprite(this AnimatedSprite sprite, string textureName)
    {
        sprite.textureName.Value = textureName;
    }
}