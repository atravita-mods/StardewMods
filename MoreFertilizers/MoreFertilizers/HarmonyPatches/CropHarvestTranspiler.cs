using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AtraBase.Toolkit;
using AtraBase.Toolkit.Extensions;
using AtraCore.Framework.ReflectionManager;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;
using AtraShared.Wrappers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MiniAtraShared.Extensions;
using MoreFertilizers.Framework;
using Netcode;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace MoreFertilizers.HarmonyPatches;

/// <summary>
/// Holds the transpiler against Crop.harvest.
/// Covers organic, bountiful, and joja fertilizers.
/// </summary>
[HarmonyPatch(typeof(Crop))]
internal static class CropHarvestTranspiler
{
    private const string DGAModDataKey = "atravita.MoreFertilizers/DGASeedID";

    private static bool hasQualityMod = false;

    internal static void Initialize(IModRegistry registry)
    {
        hasQualityMod = registry.IsLoaded("spacechase0.AQualityMod");
    }

    #region helpers
    [MethodImpl(TKConstants.Hot)]
    private static int GetQualityForJojaFert(int prevQual, HoeDirt? dirt)
        => dirt?.fertilizer.Value switch
        {
            ModEntry.JojaFertilizerID => 1,
            ModEntry.DeluxeJojaFertilizerID => Random.Shared.OfChance(0.2) ? 2 : 1,
            ModEntry.SecretJojaFertilizerID => hasQualityMod
                                ? ((Random.Shared.OfChance(0.75) || dirt.HasJojaCrop()) ? -2 : 1)
                                : ((Random.Shared.OfChance(0.5) && !dirt.HasJojaCrop()) ? 1 : 0),
            _ => prevQual,
        };

    // Handles organic/beverage fertilizer for DGA.
    [MethodImpl(TKConstants.Hot)]
    private static Item? HandleOrganicAndBeverageItem(Item? item, HoeDirt? dirt, JunimoHarvester? junimo)
    {
        HandleBeverageFertilizer(item, dirt, junimo);
        return item is SObject obj ? MakeObjectOrganic(obj, dirt) : item;
    }

    [MethodImpl(TKConstants.Hot)]
    private static SObject HandleOrganicAndBeverage(SObject obj, HoeDirt? dirt, JunimoHarvester? junimo)
    {
        HandleBeverageFertilizer(obj, dirt, junimo);
        return MakeObjectOrganic(obj, dirt);
    }

    [MethodImpl(TKConstants.Hot)]
    private static SObject MakeObjectOrganic(SObject obj, HoeDirt? dirt)
    {
        switch (dirt?.fertilizer.Value)
        {
            case ModEntry.OrganicFertilizerID when obj.Name.ContainsIgnoreCase("Joja"):
                obj.modData?.SetBool(CanPlaceHandler.Organic, true);
                obj.Price = (int)(obj.Price * 1.1);
                obj.Name += " (Organic)";
                obj.MarkContextTagsDirty();
                break;
            case ModEntry.DeluxeJojaFertilizerID:
            case ModEntry.JojaFertilizerID:
            case ModEntry.SecretJojaFertilizerID:
                obj.modData?.SetBool(CanPlaceHandler.Joja, true);
                obj.MarkContextTagsDirty();
                break;
        }
        return obj;
    }

    // Drops the beverage if needed.
    [MethodImpl(TKConstants.Hot)]
    private static void HandleBeverageFertilizer(Item? item, HoeDirt? dirt, JunimoHarvester? junimo)
    {
        if (item is not null
           && dirt?.fertilizer.Value == ModEntry.MiraculousBeveragesID
           && MiraculousFertilizerHandler.GetBeverage(item) is SObject beverage)
        {
            if (junimo is not null)
            {
                junimo.tryToAddItemToHut(beverage);
            }
            else
            {
                Game1.createItemDebris(beverage, dirt.Tile * 64f, -1);
            }
        }
    }

    [MethodImpl(TKConstants.Hot)]
    private static int IncrementForBountiful(int prevValue, HoeDirt? dirt)
    {
        if (dirt?.fertilizer?.Value == ModEntry.BountifulFertilizerID
            && Random.Shared.OfChance(0.1))
        {
            ModEntry.ModMonitor.DebugOnlyLog("IncrementedOnceForBountiful", LogLevel.Info);
            return prevValue * 2;
        }
        return prevValue;
    }

    [MethodImpl(TKConstants.Hot)]
    private static int AdjustExperience(int prevValue, HoeDirt? dirt)
    {
        if (dirt?.fertilizer?.Value == ModEntry.WisdomFertilizerID)
        {
            return (1.5 * prevValue).RandomRoundProportional();
        }
        return prevValue;
    }

    [MethodImpl(TKConstants.Hot)]
    private static int AdjustRegrow(int prevValue, HoeDirt? dirt)
    {
        if (dirt?.fertilizer?.Value == ModEntry.SecretJojaFertilizerID
            && (Random.Shared.OfChance(0.5) || dirt.HasJojaCrop()))
        {
            return Math.Max(1, ((hasQualityMod ? 0.65 : 0.8) * prevValue).RandomRoundProportional());
        }
        return prevValue;
    }

    [MethodImpl(TKConstants.Hot)]
    private static void DropSeedsForSeedyFertilizer(int x, int y, HoeDirt? dirt, JunimoHarvester? jumino)
    {
        if (dirt?.crop is not null && dirt.fertilizer?.Value == ModEntry.SeedyFertilizerID
            && Random.Shared.OfChance(0.1))
        {
            string seedIndex = dirt.crop.isWildSeedCrop() ? dirt.crop.netSeedIndex.Value : dirt.crop.whichForageCrop.Value;
            if (Game1Wrappers.ObjectData.ContainsKey(seedIndex))
            {
                SObject seeds = new(seedIndex, Random.Shared.Next(3));
                if (jumino is null)
                {
                    Game1.createItemDebris(seeds, new Vector2((x * Game1.tileSize) + 32, (y * Game1.tileSize) + 32), -1, dirt.Location);
                }
                else
                {
                    jumino.tryToAddItemToHut(seeds);
                }
            }
        }
    }

    #endregion

    [HarmonyPatch(nameof(Crop.harvest))]
    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, ModEntry.ModMonitor, gen);
            helper.FindNext(
            [// if (this.forageCrop) and advance past this
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.forageCrop), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call),
                new(OpCodes.Brfalse),
            ])
            .Advance(3)
            .StoreBranchDest()
            .AdvanceToStoredLabel()
            .FindNext(
            [ // find if (this.minHarvest > 1 || this.maxHarvest < 1)
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.maxHarvest), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call),
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Ble_S),
            ])
            .Advance(4)
            .StoreBranchDest() // We'll be sticking our incrementer at the end of this block, so store the branch dest so we can go there later.
            .FindNext(
            [// Advance into that block, find num = random2.Next(this.minHarvest + ...). This lets us grab the right local.
                new(OpCodes.Add),
                new(OpCodes.Call, typeof(Math).GetCachedMethod(nameof(Math.Max), ReflectionCache.FlagTypes.StaticFlags, [typeof(int), typeof(int)] )),
                new(OpCodes.Callvirt, typeof(Random).GetCachedMethod(nameof(Random.Next), ReflectionCache.FlagTypes.InstanceFlags, [typeof(int), typeof(int)])),
                new(SpecialCodeInstructionCases.StLoc),
            ])
            .FindNext(
            [
                new(SpecialCodeInstructionCases.StLoc),
            ]);

            CodeInstruction numberToHarvestLdLoc = helper.CurrentInstruction.ToLdLoc();
            CodeInstruction numberToHarvestStLoc = helper.CurrentInstruction.Clone();

            // Advance out of that block.
            helper.AdvanceToStoredLabel()
            .GetLabels(out IList<Label>? numAdjustLabels, clear: true)
            .Insert(
            [ // and insert our incrementer.
                numberToHarvestLdLoc,
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(IncrementForBountiful), ReflectionCache.FlagTypes.StaticFlags)),
                numberToHarvestStLoc,
            ], withLabels: numAdjustLabels)
            .FindNext(
            [ // find if(this.indexOfHarvest == )
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld),
                new(OpCodes.Call),
                new(OpCodes.Ldc_I4, 771),
            ])
            .Push() // we'll be inserting the quality just before this, so temporarily save it.
            .FindNext(
            [
                new(OpCodes.Ldc_I4_0),
                new(SpecialCodeInstructionCases.StLoc),
            ])
            .Advance(1);

            CodeInstruction qualityStLoc = helper.CurrentInstruction.Clone();
            CodeInstruction qualityLdLoc = helper.CurrentInstruction.ToLdLoc();

            helper.Pop()
            .GetLabels(out IList<Label>? jojaLabels, clear: true)
            .Insert(
            [
                qualityLdLoc,
                new(OpCodes.Ldarg_3), // HoeDirt soil
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(GetQualityForJojaFert), ReflectionCache.FlagTypes.StaticFlags)),
                qualityStLoc,
            ], withLabels: jojaLabels)
            .FindNext(
            [ // if (this.programColored)
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.programColored), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call),
            ])
            .FindNext(
            [
                new(SpecialCodeInstructionCases.LdLoc),
                new(OpCodes.Callvirt, typeof(SObject).GetCachedProperty(nameof(SObject.Quality), ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
                new(SpecialCodeInstructionCases.StLoc, typeof(SObject)),
            ])
            .Advance(2)
            .GetLabels(out IList<Label>? firstSObjectCreationLabels, clear: true)
            .Insert(
            [ // Insert function to make the object organic if needed.
                new(OpCodes.Ldarg_3),
                new(OpCodes.Ldarg, 4),
                new (OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(HandleOrganicAndBeverage), ReflectionCache.FlagTypes.StaticFlags)),
            ], withLabels: firstSObjectCreationLabels)
            .FindNext(
            [ // find the sunflower seeds block (421)
                new (OpCodes.Ldarg_0),
                new (OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.indexOfHarvest), ReflectionCache.FlagTypes.InstanceFlags)),
                new (OpCodes.Call), // this is a op_Implicit
                new (OpCodes.Ldc_I4, 421),
            ])
            .GetLabels(out IList<Label>? seedyLabels)
            .Insert(
            [ // and just insert the seed fertilizer just before it.
                new(OpCodes.Ldarg_1),
                new(OpCodes.Ldarg_2),
                new(OpCodes.Ldarg_3),
                new(OpCodes.Ldarg, 4),
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(DropSeedsForSeedyFertilizer), ReflectionCache.FlagTypes.StaticFlags)),
            ], withLabels: seedyLabels)
            .FindNext(
            [// if (this.programColored), the second instance.
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.programColored), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call),
            ])
            .FindNext(
            [ // Find the place where the second creation of an SObject/ColoredSObject is saved.
                new (OpCodes.Newobj, typeof(ColoredObject).GetCachedConstructor(ReflectionCache.FlagTypes.InstanceFlags, [typeof(int), typeof(int), typeof(Color)] )),
                new (SpecialCodeInstructionCases.StLoc, typeof(SObject)),
            ])
            .Advance(1);

            CodeInstruction secondSObjectLdLoc = helper.CurrentInstruction.ToLdLoc();
            CodeInstruction secondSObjectStLoc = helper.CurrentInstruction.Clone();

            helper.GetLabels(out IList<Label>? secondSObjectCreationLabels, clear: true)
            .Insert(
            [ // Insert function to make the object organic if needed.
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(MakeObjectOrganic), ReflectionCache.FlagTypes.StaticFlags)),
            ], withLabels: secondSObjectCreationLabels)
            .Advance(1)
            .Insert(
            [// Insert instructions to adjust the quality here too.
                secondSObjectLdLoc,
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(GetQualityForJojaFert), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Callvirt, typeof(SObject).GetCachedProperty(nameof(SObject.Quality), ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
            ])
            .FindNext(
            [ // Find the block where the player is given XP
                new(OpCodes.Call, typeof(Game1).GetCachedProperty(nameof(Game1.player), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()),
                new(OpCodes.Ldc_I4_0),
                new(SpecialCodeInstructionCases.LdLoc),
            ])
            .FindNext(
            [
                new(OpCodes.Conv_I4),
                new(OpCodes.Callvirt, typeof(Farmer).GetCachedMethod(nameof(Farmer.gainExperience), ReflectionCache.FlagTypes.InstanceFlags)),
            ])
            .Advance(1)
            .Insert(
            [ // insert a call to a function that changes the experience gained.
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(AdjustExperience), ReflectionCache.FlagTypes.StaticFlags)),
            ])
            .FindNext(
            [ // find this.dayOfCurrentPhase.Value = this.regrowAfterHarvest
                new(OpCodes.Ldfld, typeof(Crop).GetCachedField(nameof(Crop.regrowAfterHarvest), ReflectionCache.FlagTypes.InstanceFlags)),
                new(OpCodes.Call),
                new(OpCodes.Callvirt, typeof(NetFieldBase<int, NetInt>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
            ])
            .Advance(2)
            .Insert(
            [
                new(OpCodes.Ldarg_3),
                new(OpCodes.Call, typeof(CropHarvestTranspiler).GetCachedMethod(nameof(AdjustRegrow), ReflectionCache.FlagTypes.StaticFlags)),
            ]);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

}
