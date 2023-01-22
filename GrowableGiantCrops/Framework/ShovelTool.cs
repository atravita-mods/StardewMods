using System.Xml.Serialization;

using AtraShared.Utils.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley.Tools;

namespace GrowableGiantCrops.Framework;

/// <summary>
/// A shovel.
/// </summary>
[XmlType("Mods_atravita_Shovel")]
public sealed class ShovelTool : GenericTool
{
    public ShovelTool()
        : base(I18n.Shovel_Name(), I18n.Shovel_Description(), 0, 0, 0)
    {
    }

    public override Item getOne()
    {
        ShovelTool newShovel = new();
        newShovel._GetOneFrom(this);
        return newShovel;
    }

    public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
    {
        ModEntry.ModMonitor.DebugOnlyLog("begun using!", LogLevel.Alert);
        return base.beginUsing(location, x, y, who);
    }

    public override bool onRelease(GameLocation location, int x, int y, Farmer who) => false;

    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        spriteBatch.Draw(
            texture: AssetManager.ToolTexture,
            position: location + new Vector2(32f, 32f),
            new Rectangle(80, 16, 16, 16),
            color: color * transparency,
            rotation: 0f,
            new Vector2(8f, 8f),
            scale: 4f * scaleSize,
            effects: SpriteEffects.None,
            layerDepth);
    }

    protected override string loadDisplayName() => I18n.Shovel_Name();
    protected override string loadDescription() => I18n.Shovel_Description();
}
