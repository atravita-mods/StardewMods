using HarmonyLib;
using MiniAtraShared.Extensions;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;

namespace RecipeSortFix;
internal static class Patcher
{
    internal static void Apply(Harmony harmony)
    {
        harmony.Patch(
            AccessTools.Method(typeof(CraftingPage), "GetRecipesToDisplay"),
            postfix: new HarmonyMethod(typeof(Patcher), nameof(Postfix))
            );
    }

    private static void Postfix(List<string> __result, CraftingPage __instance)
    {
        try
        {
            __result.Sort((a, b) =>
            {
                return GetDisplayName(a, __instance.cooking).CompareTo(GetDisplayName(b, __instance.cooking));
            });
        }
        catch (Exception ex)
        {
            ModEntry.ModMonitor.LogError("resorting recipes", ex);
        }
    }

    private static string GetDisplayName(string name, bool isCooking)
    {
        var asset = isCooking ? DataLoader.CookingRecipes(Game1.content) : DataLoader.CraftingRecipes(Game1.content);

        var data = asset.GetValueOrDefault(name);

        if (data is null)
        {
            return "ERROR";
        }

        var fields = data.Split('/');

        if (!ArgUtility.TryGetOptional(fields, isCooking ? 4 : 5, out string? tokenizableDisplayName, out var error, null))
        {
            return "ERROR";
        }

        if (tokenizableDisplayName is not null)
        {
            return TokenParser.ParseText(tokenizableDisplayName);
        }

        // else parse from raw output items.
        if (!ArgUtility.TryGet(fields, 2, out var rawOutputItems, out error, allowBlank: false))
        {
            return "ERROR";
        }


        var item = ArgUtility.SplitBySpaceAndGet(rawOutputItems, 0);
        if (item is null)
        {
            return "ERROR";
        }
        if (!isCooking && ArgUtility.GetBool(fields, 3))
        {
            item = ItemRegistry.ManuallyQualifyItemId(item, ItemRegistry.type_bigCraftable);
        }

        var creation = ItemRegistry.GetData(item);
        return TokenParser.ParseText(creation.DisplayName) ?? "ERROR";
    }
}
