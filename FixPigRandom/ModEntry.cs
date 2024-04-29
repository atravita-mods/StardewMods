using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using AtraBase.Toolkit;

using AtraCore.Framework.ReflectionManager;

using AtraShared.ConstantsAndEnums;
using AtraShared.Utils;
using AtraShared.Utils.Extensions;
using AtraShared.Utils.HarmonyHelper;

using HarmonyLib;

using Microsoft.Xna.Framework;

using Netcode;

using StardewValley.Network;
using StardewValley.TerrainFeatures;

namespace FixPigRandom;

/// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Reviewed.")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed.")]
internal sealed class ModEntry : Mod
{
    #region thread locals
    private static readonly ThreadLocal<Vector2[]> _directions = new(static () => new Vector2[] { Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX, -Vector2.UnitY } );
    private static readonly ThreadLocal<Queue<Vector2>> _queue = new(static () => new());
    private static readonly ThreadLocal<HashSet<Vector2>> _visited = new(static () => new());
    #endregion

    private static readonly Dictionary<long, Random> Cache = [];

    private static IMonitor modMonitor = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        modMonitor = this.Monitor;
        helper.Events.GameLoop.DayEnding += static (_, _) => Cache.Clear();
        helper.Events.GameLoop.GameLaunched += (_, _) => this.ApplyPatches(new(this.ModManifest.UniqueID));

        this.Monitor.Log($"Starting up: {this.ModManifest.UniqueID} - {typeof(ModEntry).Assembly.FullName}");
    }

    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            harmony.Patch(
                original: typeof(FarmAnimal).GetCachedMethod(nameof(FarmAnimal.DigUpProduce), ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new(typeof(ModEntry).GetCachedMethod(nameof(Transpiler), ReflectionCache.FlagTypes.StaticFlags)));

            harmony.Patch(
                original: typeof(FarmAnimal).GetCachedMethod(nameof(FarmAnimal.behaviors), ReflectionCache.FlagTypes.InstanceFlags),
                transpiler: new(typeof(ModEntry).GetCachedMethod(nameof(FarmAnimalBehaviorTranspiler), ReflectionCache.FlagTypes.StaticFlags)));
        }
        catch (Exception ex)
        {
            modMonitor.Log(string.Format(ErrorMessageConsts.HARMONYCRASH, ex), LogLevel.Error);
        }
        harmony.Snitch(this.Monitor, harmony.Id, transpilersOnly: true);
    }

    private static bool ReplacementSpawnObject(Vector2 tile, SObject obj, GameLocation loc, bool playSound, Action<SObject> modifyObject)
    {
        if (obj is null || loc is null || tile == Vector2.Zero)
        {
            return false;
        }

        Queue<Vector2> queue = _queue.Value!;
        HashSet<Vector2> visited = _visited.Value!;

        queue.Clear();
        visited.Clear();

        visited.Add(tile);
        queue.Enqueue(tile);
        for (int attempts = 0; attempts < 100; attempts++)
        {
            Vector2 current = queue.Dequeue();
            if (loc.CanItemBePlacedHere(current))
            {
                // place object
                obj.IsSpawnedObject = true;
                obj.CanBeGrabbed = true;

                obj.TileLocation = current;
                modifyObject?.Invoke(obj);

                loc.Objects[current] = obj;

                if (playSound)
                {
                    loc.playSound("coin");
                }
                if (ReferenceEquals(loc, Game1.currentLocation))
                {
                    loc.temporarySprites.Add(new(5, current * Game1.tileSize, Color.White));
                }

                return true;
            }

            Vector2[] directions = _directions.Value!;
            Utility.Shuffle(Random.Shared, directions);
            foreach (Vector2 d in directions)
            {
                Vector2 next = current + d;
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    [MethodImpl(TKConstants.Hot)]
    private static Random GetRandom(FarmAnimal pig) => GetRandom(pig.myID.Value);

    [MethodImpl(TKConstants.Hot)]
    private static Random GetRandom(long id)
    {
        try
        {
            if (!Cache.TryGetValue(id, out Random? random))
            {
                unchecked
                {
                    modMonitor.DebugOnlyLog($"Cache miss: {id}", LogLevel.Info);
                    Cache[id] = random = RandomUtils.GetSeededRandom(2, (int)(id >> 1));
                }
            }
            return random;
        }
        catch (Exception ex)
        {
            modMonitor.LogError($"generating random for pig {id}", ex);
        }

        return Random.Shared;
    }

    private static IEnumerable<CodeInstruction>? Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, modMonitor, gen);

            helper
            .FindNext(
            [ // find the creation of the random and replace it with our own.
                OpCodes.Ldarg_0,
                (OpCodes.Ldfld, typeof(FarmAnimal).GetCachedField(nameof(FarmAnimal.myID), ReflectionCache.FlagTypes.InstanceFlags)),
                OpCodes.Callvirt,
                OpCodes.Conv_R8,
            ])
            .Advance(1)
            .RemoveIncluding(
            [
                (OpCodes.Call, typeof(Utility).GetCachedMethod(nameof(Utility.CreateRandom), ReflectionCache.FlagTypes.StaticFlags)),
            ])
            .Insert(
            [
                new(OpCodes.Call, typeof(ModEntry).GetCachedMethod<FarmAnimal>(nameof(GetRandom), ReflectionCache.FlagTypes.StaticFlags)),
            ])
            .FindNext([
                (OpCodes.Call, typeof(Utility).GetCachedMethod(nameof(Utility.spawnObjectAround), ReflectionCache.FlagTypes.StaticFlags))
            ])
            .ReplaceOperand(typeof(ModEntry).GetCachedMethod(nameof(ReplacementSpawnObject), ReflectionCache.FlagTypes.StaticFlags));

#if DEBUG
            helper.FindNext(
            [
                OpCodes.Ldnull,
                (OpCodes.Callvirt, typeof(Netcode.NetFieldBase<string, Netcode.NetString>).GetCachedProperty("Value", ReflectionCache.FlagTypes.InstanceFlags).GetSetMethod()),
            ])
            .Advance(2)
            .Insert(
            [
                new(OpCodes.Ldsfld, typeof(ModEntry).GetCachedField(nameof(modMonitor), ReflectionCache.FlagTypes.StaticFlags)),
                new(OpCodes.Ldstr, "Truffles Over"),
                new(OpCodes.Ldc_I4_1), // LogLevel.Debug
                new(OpCodes.Callvirt, typeof(IMonitor).GetCachedMethod(nameof(IMonitor.Log), ReflectionCache.FlagTypes.InstanceFlags)),
            ]);
#endif

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            modMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }

    private static IEnumerable<CodeInstruction>? FarmAnimalBehaviorTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
    {
        try
        {
            ILHelper helper = new(original, instructions, modMonitor, gen);

            /* Deleting the segment, which prevents a pig from producing if any of its four corners has anything in it.
             * There are later checks for collision anyways.
             *
             *      Rectangle rect = this.GetBoundingBox();
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 v = Utility.getCornersOfThisRectangle(ref rect, i);
                        Vector2 vec = new Vector2((int)(v.X / 64f), (int)(v.Y / 64f));
                        if (location.terrainFeatures.ContainsKey(vec) || location.objects.ContainsKey(vec))
                        {
                            return false;
                        }
                    }
             *
             **/

            helper
            .FindNext([
                (OpCodes.Call, typeof(FarmAnimal).GetCachedMethod(nameof(FarmAnimal.GetHarvestType), ReflectionCache.FlagTypes.InstanceFlags))
            ])
            .FindNext([
                OpCodes.Ldarg_0,
                (OpCodes.Callvirt, typeof(Character).GetCachedMethod(nameof(Character.GetBoundingBox), ReflectionCache.FlagTypes.InstanceFlags)),
                SpecialCodeInstructionCases.StLoc
            ])
            .RemoveUntil([
                (OpCodes.Call, typeof(Game1).GetCachedProperty(nameof(Game1.player), ReflectionCache.FlagTypes.StaticFlags).GetGetMethod()!),
                (OpCodes.Callvirt, typeof(Character).GetCachedProperty(nameof(Farmer.currentLocation), ReflectionCache.FlagTypes.InstanceFlags).GetGetMethod()!)
             ]);

            // helper.Print();
            return helper.Render();
        }
        catch (Exception ex)
        {
            modMonitor.LogTranspilerError(original, ex);
        }
        return null;
    }
}