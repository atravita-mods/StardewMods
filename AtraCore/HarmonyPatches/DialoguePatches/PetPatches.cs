using AtraBase.Toolkit.Extensions;

using AtraShared.Utils.Extensions;

using HarmonyLib;

using StardewValley.Characters;
using StardewValley.Objects;

namespace AtraCore.HarmonyPatches.DialoguePatches;

/// <summary>
/// Holds patches against pets.
/// </summary>
[HarmonyPatch]
internal static class PetPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Pet), nameof(Pet.applyButterflyPowder))]
    private static void PostfixButterflyPowder(Pet __instance, Farmer who, string responseKey)
    {
        if (responseKey?.Contains("Yes") != true)
        {
            return;
        }

        try
        {
            string specificTopic = $"atravita.gotPet_{__instance.petType.Value}";
            foreach (Farmer? farmer in Game1.getAllFarmers())
            {
                farmer.activeDialogueEvents.Remove("gotPet");
                farmer.removeActiveDialogMemoryEvents("gotPet");

                farmer.activeDialogueEvents.Remove(specificTopic);
                farmer.removeActiveDialogMemoryEvents(specificTopic);
            }

            who.autoGenerateActiveDialogueEvent("atravita.butterflied_pet");
            who.autoGenerateActiveDialogueEvent($"atravita.butterflied_pet_{__instance.petType.Value}");
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("editing active converation topics", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Event), "namePet")]
    private static void PostfixEventNamePet()
    {
        AddConversationTopic(Game1.player.whichPetType);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PetLicense), "namePet")]
    private static void PostfixPetLicense(string name)
    {
        ReadOnlySpan<char> petType = name.GetNthChunk('|', 0);
        if (petType.Length == 0)
        {
            return;
        }

        AddConversationTopic(petType);
    }

    private static void AddConversationTopic(ReadOnlySpan<char> petType)
    {
        string topic = $"atravita.gotPet_{petType}";
        foreach (Farmer? farmer in Game1.getAllFarmers())
        {
            farmer.autoGenerateActiveDialogueEvent(topic);
        }
    }
}
