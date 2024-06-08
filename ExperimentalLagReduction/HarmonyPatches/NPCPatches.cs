using AtraBase.Toolkit.Reflection;

using AtraCore.Framework.ReflectionManager;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace ExperimentalLagReduction.HarmonyPatches;

[HarmonyPatch(typeof(Game1))]
internal static class NPCPatches
{
    private static readonly Lazy<Action<NPC, string>> _lastLocationSetter = new(() =>
        typeof(NPC).GetCachedField("LastLocationNameForAppearance", ReflectionCache.FlagTypes.InstanceFlags)
                   .GetInstanceFieldSetter<NPC, string>()
    );

    [HarmonyPatch(nameof(Game1.warpCharacter), [typeof(NPC), typeof(GameLocation), typeof(Vector2)])]
    private static void Prefix(NPC character, GameLocation targetLocation)
    {
        if (character.IsVillager && targetLocation.farmers?.Count is null or 0)
        {
            _lastLocationSetter.Value(character, targetLocation.NameOrUniqueName);
            character.Portrait = null; // setting portrait to null will cause the game to call ChooseAppearance before displaying it again.
        }
    }
}
