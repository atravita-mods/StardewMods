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
        ModEntry.ModMonitor.DebugOnlyLog($"begun using! {x/64} {y/64} - {who.getTileLocationPoint().ToString()}", LogLevel.Alert);

        // use the watering can arms.
        who.jitterStrength = 0.25f;
        switch (who.FacingDirection)
        {
            case Game1.up:
                who.FarmerSprite.setCurrentFrame(180);
                break;
            case Game1.right:
                who.FarmerSprite.setCurrentFrame(172);
                break;
            case Game1.down:
                who.FarmerSprite.setCurrentFrame(164);
                break;
            case Game1.left:
                who.FarmerSprite.setCurrentFrame(188);
                break;
        }
        this.Update(who.FacingDirection, 0, who);
        return false;
    }

    public override void endUsing(GameLocation location, Farmer who)
    {
        // use the watering can arms.
        switch (who.FacingDirection)
        {
            case 2:
                ((FarmerSprite)who.Sprite).animateOnce(164, 125f , 3);
                break;
            case 1:
                ((FarmerSprite)who.Sprite).animateOnce(172, 125f, 3);
                break;
            case 0:
                ((FarmerSprite)who.Sprite).animateOnce(180, 125f, 3);
                break;
            case 3:
                ((FarmerSprite)who.Sprite).animateOnce(188, 125f, 3);
                break;
        }
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
