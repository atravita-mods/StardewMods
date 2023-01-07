using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI.Utilities;

namespace Stackify.Framework;
public sealed class ModConfig
{
    public KeybindList ColorStackBind { get; set; } = KeybindList.Parse("LeftShift");

    public KeybindList QualityStackBind { get; set; } = KeybindList.Parse("LeftAlt");
}
