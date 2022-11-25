using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterIntegratedModItems.Framework.DataModels;

public sealed class LocationWatcher
{
    public HashSet<string> SeenLocations { get; set; } = new();
}