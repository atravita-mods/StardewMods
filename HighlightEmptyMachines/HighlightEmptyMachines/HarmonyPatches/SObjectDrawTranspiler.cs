﻿using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Niceties;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using HighlightEmptyMachines.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace HighlightEmptyMachines.HarmonyPatches;

/// <summary>
/// Transpilers against SObject's draw to color bigcraftables if they're empty or in an invalid position.
/// </summary>
[HarmonyPatch(typeof(SObject))]
internal class SObjectDrawTranspiler
{
    [MethodImpl(TKConstants.Hot)]
    private static Color BigCraftableNeedsInputLayerColor(SObject obj)
    {
        if (obj.heldObject.Value is not null)
        {
            return Color.White;
        }
        if (ModEntry.Config.VanillaMachines.TryGetValue((VanillaMachinesEnum)obj.ParentSheetIndex, out bool val) && val)
        {
            if (obj is Cask cask && Game1.currentLocation is GameLocation loc && !cask.IsValidCaskLocation(loc))
            {
                return ModEntry.Config.InvalidColor;
            }
            return ModEntry.Config.EmptyColor;
        }
        else if (PFMMachineHandler.ValidMachines.TryGetValue(obj.ParentSheetIndex, out PFMMachineStatus status))
        {
            return status switch
            {
                PFMMachineStatus.Invalid => ModEntry.Config.InvalidColor,
                PFMMachineStatus.Enabled => ModEntry.Config.EmptyColor,
                _ => Color.White,
            };
        }
        return Color.White;
    }

#pragma warning disable SA1116 // Split parameters should start on line after declaration. Reviewed
    [HarmonyPatch(nameof(SObject.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) })]
    [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = "Only used for matching.")]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(new CodeInstructionWrapper[]
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(SObject).GetCachedField(nameof(SObject.bigCraftable), ReflectionCache.FlagTypes.InstanceFlags)),
            })
            .FindNext(new CodeInstructionWrapper[]
            {
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(Item).GetCachedField(nameof(Item.parentSheetIndex), ReflectionCache.FlagTypes.InstanceFlags)),
                new (OpCodes.Call),
                new (OpCodes.Ldc_I4, 272),
                new (OpCodes.Bne_Un),
            })
            .Advance(4)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .FindNext(new CodeInstructionWrapper[]
            {
                new (OpCodes.Call, typeof(Color).GetCachedProperty(nameof(Color.White), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
            })
            .GetLabels(out IList<Label> colorLabels, clear: true)
            .ReplaceInstruction(OpCodes.Call, typeof(SObjectDrawTranspiler).GetCachedMethod(nameof(BigCraftableNeedsInputLayerColor), ReflectionCache.FlagTypes.StaticFlags))
            .Insert(new CodeInstruction[]
            {
                new(OpCodes.Ldarg_0),
            }, withLabels: colorLabels);
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.Log($"Mod crashed while transpiling SObject.draw\n\n{ex}", LogLevel.Error);
            original?.Snitch(ModEntry.ModMonitor);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}