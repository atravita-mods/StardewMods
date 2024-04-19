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
            string breedTopic = $"atravita.gotPet_{__instance.petType.Value}_{__instance.whichBreed.Value}";
            foreach (Farmer? farmer in Game1.getAllFarmers())
            {
                farmer.activeDialogueEvents.Remove("gotPet");
                farmer.removeActiveDialogMemoryEvents("gotPet");

                farmer.activeDialogueEvents.Remove(specificTopic);
                farmer.removeActiveDialogMemoryEvents(specificTopic);

                farmer.activeDialogueEvents.Remove(breedTopic);
                farmer.removeActiveDialogMemoryEvents(breedTopic);
            }

            who.autoGenerateActiveDialogueEvent("atravita.butterflied_pet");
            who.autoGenerateActiveDialogueEvent($"atravita.butterflied_pet_{__instance.petType.Value}");

            who.stats.Increment("PetsButterflied");
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("editing active conversation topics", ex);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Event), "namePet")]
    private static void PostfixEventNamePet()
    {
        AddConversationTopic(Game1.player.whichPetType);
        AddConversationTopic($"{Game1.player.whichPetType}_{Game1.player.whichPetBreed}");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PetLicense), "namePet")]
    private static void PostfixPetLicense(string name)
    {

        if (name.TrySplitOnce('|', out ReadOnlySpan<char> petType, out ReadOnlySpan<char> petBreed))
        {
            AddConversationTopic(petType);
            AddConversationTopic($"{petType}_{petBreed}");
        }
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
