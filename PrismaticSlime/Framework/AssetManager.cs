using AtraCore;
using AtraCore.Models;
using AtraShared.ConstantsAndEnums;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace PrismaticSlime.Framework;
internal static class AssetManager
{
    private static readonly int ringHash = "atravita.PrismaticSlimeRing".GetHashCode();
    private static readonly int eggHash = "atravita.PrismaticSlimeEgg".GetHashCode();

    private static readonly string OBJECTDATA = PathUtilities.NormalizeAssetName("Data/ObjectInformation");
    private static readonly string RINGMASK = PathUtilities.NormalizeAssetName("Mods/atravita_Prismatic_Ring/Texture");

    internal static void Apply(AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(OBJECTDATA))
        {
            e.Edit(EditObjects);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(AtraCoreConstants.PrismaticMaskData))
        {
            e.Edit(EditPrismaticMasks);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(RINGMASK))
        {
            e.LoadFromModFile<Texture2D>("assets/json-assets/Objects/PrismaticSlimeRing/mask.png", AssetLoadPriority.Exclusive);
        }
    }

    private static void EditObjects(IAssetData asset)
    {
        if (ModEntry.PrismaticSlimeEgg != -1)
        {
            IAssetDataForDictionary<int, string>? editor = asset.AsDictionary<int, string>();
            if (editor.Data.TryGetValue(ModEntry.PrismaticSlimeEgg, out string? val))
            {
                editor.Data[ModEntry.PrismaticSlimeEgg] = val.Replace("Basic -20", "Basic");
            }
            else
            {
                ModEntry.ModMonitor.Log($"Could not find {ModEntry.PrismaticSlimeEgg} in ObjectInformation to edit! This mod may not function properly.", LogLevel.Error);
            }
        }
    }

    private static void EditPrismaticMasks(IAssetData asset)
    {
        IAssetDataForDictionary<int, DrawPrismaticModel>? editor = asset.AsDictionary<int, DrawPrismaticModel>();

        DrawPrismaticModel? ring = new()
        {
            ItemType = ItemTypeEnum.Ring,
            Identifier = "atravita.PrismaticSlimeRing",
            Mask = RINGMASK,
        };

        DrawPrismaticModel? egg = new()
        {
            ItemType = ItemTypeEnum.SObject,
            Identifier = "atravita.PrismaticSlimeEgg",
        };

        int index = ringHash;
        while (!editor.Data.TryAdd(index, ring))
        {
            index++;
        }

        index = eggHash;
        while (!editor.Data.TryAdd(index, egg))
        {
            index++;
        }
    }
}
