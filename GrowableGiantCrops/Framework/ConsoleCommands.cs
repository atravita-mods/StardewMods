using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrowableGiantCrops.Framework;
internal static class ConsoleCommands
{
    internal static void RegisterCommands(ICommandHelper command)
    {
        command.Add("av.ggc.add_shovel", "Adds a shovel to your inventory", AddShovel);
    }

    private static void AddShovel(string commands, string[] args)
    {
        ShovelTool shovel = new();
        Game1.player.addItemToInventoryBool(shovel, makeActiveObject: true);
    }
}
