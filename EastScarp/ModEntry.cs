using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EastScarp;
internal sealed class ModEntry: Mod
{
    internal static IMonitor ModMonitor = null!;

    public override void Entry(IModHelper helper)
    {
        ModMonitor = this.Monitor;
    }
}
