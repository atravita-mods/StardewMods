using HarmonyLib;

using Netcode;

namespace ExperimentalLagReduction.HarmonyPatches.MiniChanges;

[HarmonyPatch(typeof(NetFields))]
internal static class NetfieldsMemoryPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NetFields.AddField))]
    private static void PostfixAddField(INetSerializable field)
    {
        field.Name = string.Intern(field.Name);
    }

    [HarmonyPrefix]
    [HarmonyPatch(MethodType.Constructor, [typeof(string)])]
    private static void PostfixConstructor(ref string name)
    {
        name = string.Intern(name);
    }
}
