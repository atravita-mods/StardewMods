using AtraCore.Framework.ItemManagement;

using AtraShared.ConstantsAndEnums;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// Manages console commands for this mod.
/// </summary>
internal static class ConsoleCommands
{
    internal static void RegisterCommands(ICommandHelper command)
    {
        command.Add("av.ggc.add_shovel", "Adds a shovel to your inventory", AddShovel);

        command.Add("av.ggc.add_giant", "Adds a giant crop to your inventory", AddGiant);
    }

    private static void AddShovel(string commands, string[] args)
    {
        ShovelTool shovel = new();
        Game1.player.addItemToInventoryBool(shovel, makeActiveObject: true);
    }

    private static void AddGiant(string commands, string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            ModEntry.ModMonitor.Log("Expected one or two arguments", LogLevel.Error);
            return;
        }

        if (args.Length != 2 || !int.TryParse(args[1], out int count))
        {
            count = 1;
        }

        string name = args[0].Trim();

        if (!int.TryParse(name, out int productID))
        {
            productID = DataToItemMap.GetID(ItemTypeEnum.SObject, name);
        }

        if (productID < 0)
        {
            ModEntry.ModMonitor.Log($"Could not resolve product '{name}'.", LogLevel.Error);
            return;
        }
    }
}
