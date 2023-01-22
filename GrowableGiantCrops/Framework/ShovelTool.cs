using System.Xml.Serialization;

using StardewValley.Tools;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A shovel.
/// </summary>
[XmlType("Mods_atravita_Shovel")]
public sealed class ShovelTool : GenericTool
{
    public ShovelTool()
    {
    }

    public override Item getOne()
    {
        ShovelTool newShovel = new();
        newShovel._GetOneFrom(this);
        return newShovel;
    }

    protected override string loadDisplayName() => I18n.Shovel_Name();

    protected override string loadDescription() => I18n.Shovel_Description();
}
