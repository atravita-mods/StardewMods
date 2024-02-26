using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using HarmonyLib;
using StardewModdingAPI.Events;

using StardewValley.Extensions;
using StardewValley.Locations;

namespace StopRugRemoval.HarmonyPatches.Volcano;

/// <summary>
/// Data class that holds the last chest values.
/// </summary>
public class VolcanoData
{
    /// <summary>
    /// Gets or sets the last ID from the rare chest.
    /// </summary>
    public int RareChest { get; set; } = -1;

    /// <summary>
    /// Gets or sets the last ID from the common chest.
    /// </summary>
    public int CommonChest { get; set; } = -1;
}

/// <summary>
/// Adjusts the contents of the volcano chest so it doesn't have duplicates.
/// </summary>
[HarmonyPatch(typeof(VolcanoDungeon))]
internal static class VolcanoChestAdjuster
{
    private const string RecieveDataValue = "SENDINGVOLCANODATA";
    private const string SAVEDATAKEY = "atravita.StopRugRemoval.VolcanoData";

    private static VolcanoData? data;

    /// <summary>
    /// Handles broadcasting data from player to player.
    /// </summary>
    /// <param name="multiplayer">SMAPI's multiplayer helper.</param>
    /// <param name="playerIDs">Player IDs to send to. Leave null to mean everyone.</param>
    internal static void BroadcastData(IMultiplayerHelper multiplayer, long[]? playerIDs = null)
    {
        if (Context.IsMultiplayer)
        {
            multiplayer.SendMessage(data, RecieveDataValue, [ModEntry.UNIQUEID], playerIDs);
        }
    }

    /// <summary>
    /// Handles receiving the data from another player.
    /// </summary>
    /// <param name="e">Event args.</param>
    internal static void RecieveData(ModMessageReceivedEventArgs e)
    {
        if (e.Type is RecieveDataValue)
        {
            data = e.ReadAs<VolcanoData>();
        }
    }

    /// <summary>
    /// Call for host only = loads the data from the save.
    /// </summary>
    /// <param name="dataHelper">SMAPI's data helper.</param>
    /// <param name="multiplayerHelper">SMAPI's multiplayer helper.</param>
    internal static void LoadData(IDataHelper dataHelper, IMultiplayerHelper multiplayerHelper)
    {
        data = dataHelper.ReadSaveData<VolcanoData>(SAVEDATAKEY) ?? new();
        BroadcastData(multiplayerHelper);
    }

    /// <summary>
    /// Call for the host only = saves data to the save.
    /// </summary>
    /// <param name="dataHelper">SMAPI's data helper.</param>
    /// <param name="multiplayerHelper">SMAPI's multiplayer helper.</param>
    internal static void SaveData(IDataHelper dataHelper, IMultiplayerHelper multiplayerHelper)
    {
        if (data is not null && (data.CommonChest != -1 || data.RareChest != -1))
        {
            dataHelper.WriteSaveData(SAVEDATAKEY, data);
        }
        else
        {
            // erase data if it's not relevant.
            dataHelper.WriteSaveData<VolcanoData>(SAVEDATAKEY, null);
        }
        BroadcastData(multiplayerHelper);
    }

    /// <summary>
    /// Adjusts the value given by the random for a common chest.
    /// </summary>
    /// <param name="prevValue">Value given by the random.</param>
    /// <returns>True to continue, false to loop back.</returns>
    [MethodImpl(TKConstants.Hot)]
    private static bool AdjustCommonChest(int prevValue)
    {
        if (data is null)
        {
            return true;
        }
        else if (prevValue == data.CommonChest)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"{prevValue} is a common chest repeat, forcing a loop", LogLevel.Info);
            return false;
        }
        else
        {
            data.CommonChest = prevValue;
            ModEntry.ModMonitor.DebugOnlyLog($"Stashed common chest value {prevValue}", LogLevel.Info);
            BroadcastData(ModEntry.MultiplayerHelper);
            return true;
        }
    }

    /// <summary>
    /// Adjusts the value given by the random for a rare chest.
    /// </summary>
    /// <param name="prevValue">Value given by the random.</param>
    /// <returns>True to continue, false to loop back.</returns>
    [MethodImpl(TKConstants.Hot)]
    private static bool AdjustRareChest(int prevValue)
    {
        if (data is null)
        {
            return true;
        }
        else if (prevValue == data.RareChest)
        {
            ModEntry.ModMonitor.DebugOnlyLog($"{prevValue} is a rare chest repeat, forcing a loop", LogLevel.Info);
            return false;
        }
        else
        {
            data.RareChest = prevValue;
            ModEntry.ModMonitor.DebugOnlyLog($"Stashed rare chest value {prevValue}", LogLevel.Info);
            BroadcastData(ModEntry.MultiplayerHelper);
            return true;
        }
    }

#pragma warning disable SA1116 // Split parameters should start on line after declaration
    [HarmonyPatch(nameof(VolcanoDungeon.PopulateChest))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);

            helper.FindNext(
            [
                OpCodes.Ldarg_3,
                OpCodes.Ldc_I4_1,
                OpCodes.Beq,
            ])
            .Advance(2)
            .StoreBranchDest() // this leads to common chests.
            .FindNext(
            [ // Find the first call to Random.Next and the local it stores to.
                new(SpecialCodeInstructionCases.LdArg),
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod<int>(nameof(Random.Next), ReflectionCache.FlagTypes.InstanceFlags)),
                new(SpecialCodeInstructionCases.StLoc),
            ]);

            // We'll define a local to cut off the jumpbacks after a certain point.
            // Don't want to actually loop forever.
            helper.DeclareLocal(typeof(int), out LocalBuilder countdown)
            .GetLabels(out IList<Label> firstLabelsToMove)
            .Insert(
            [
                new(OpCodes.Ldc_I4_5),
                new(OpCodes.Stloc, countdown),
            ], withLabels: firstLabelsToMove)
            .DefineAndAttachLabel(out Label firstJumpBack)
            .Advance(3);

            CodeInstruction? firstLdloc = helper.CurrentInstruction.ToLdLoc();

            helper.FindNext(
            [ // Find the block just after the while loop
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.random), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Call, typeof(RandomExtensions).GetCachedMethod<Random>(nameof(RandomExtensions.NextBool), ReflectionCache.FlagTypes.StaticFlags)),
            ])
            .GetLabels(out IList<Label> secondLabelsToMove)
            .DefineAndAttachLabel(out Label firstNoRepeat)
            .Insert(
            [
                firstLdloc,
                new(OpCodes.Call, typeof(VolcanoChestAdjuster).StaticMethodNamed(nameof(AdjustCommonChest))),
                new(OpCodes.Brtrue_S, firstNoRepeat),
                new(OpCodes.Ldloc, countdown),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Add),
                new(OpCodes.Stloc, countdown),
                new(OpCodes.Ldloc, countdown),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ble, firstNoRepeat),
                new(OpCodes.Br, firstJumpBack),
            ], withLabels: secondLabelsToMove);

            // Okay, common chests done. Let's go find rare chests.
            helper.AdvanceToStoredLabel()
            .FindNext(
            [ // Find the call to Random.Next and the local it stores to for rare chests.
                new(SpecialCodeInstructionCases.LdArg),
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod<int>(nameof(Random.Next), ReflectionCache.FlagTypes.InstanceFlags)),
                new(SpecialCodeInstructionCases.StLoc),
            ]);

            // Repeat the declaration of a local
            helper.DeclareLocal(typeof(int), out LocalBuilder secondCountdown)
            .GetLabels(out IList<Label> thirdLabelsToMove)
            .Insert(
            [
                new(OpCodes.Ldc_I4_5),
                new(OpCodes.Stloc, secondCountdown),
            ], withLabels: thirdLabelsToMove)
            .DefineAndAttachLabel(out Label secondJumpBack)
            .Advance(3);

            CodeInstruction? secondLdloc = helper.CurrentInstruction.ToLdLoc();

            helper.FindNext(
            [ // Find the block just after the while loop
                new(OpCodes.Ldsfld, typeof(Game1).GetCachedField(nameof(Game1.random), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.NextDouble), ReflectionCache.FlagTypes.InstanceFlags, Type.EmptyTypes)),
            ])
            .GetLabels(out IList<Label> fourthLabelsToMove)
            .DefineAndAttachLabel(out Label secondNoRepeat)
            .Insert(
            [
                secondLdloc,
                new(OpCodes.Call, typeof(VolcanoChestAdjuster).StaticMethodNamed(nameof(AdjustRareChest))),
                new(OpCodes.Brtrue_S, secondNoRepeat),
                new(OpCodes.Ldloc, secondCountdown),
                new(OpCodes.Ldc_I4_M1),
                new(OpCodes.Add),
                new(OpCodes.Stloc, secondCountdown),
                new(OpCodes.Ldloc, secondCountdown),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ble, secondNoRepeat),
                new(OpCodes.Br, secondJumpBack),
            ], withLabels: fourthLabelsToMove);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
#pragma warning restore SA1116 // Split parameters should start on line after declaration
}