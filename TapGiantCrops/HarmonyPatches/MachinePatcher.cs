using HarmonyLib;

namespace TapGiantCrops.HarmonyPatches;

[HarmonyPatch(typeof(MachineDataUtility))]
internal static class MachinePatcher
{
    [HarmonyPatch(nameof(MachineDataUtility.PlayEffects))]
    private static bool Prefix(SObject machine, ref bool __result)
    {
        if (machine.Location is null)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
