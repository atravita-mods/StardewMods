using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI.Events;

namespace AtraCore.Framework.Overlays;
internal sealed class SteamOverlay: AbstractOverlayManager
{
    private Vector2 _offset = Vector2.Zero;

    public SteamOverlay(IGameLoopEvents events, IDisplayEvents draw, IMonitor monitor, Farmer player)
        : base(events, draw, monitor, player)
    {
    }

    /// <inheritdoc />
    protected override void Draw(SpriteBatch b) => throw new NotImplementedException();
    protected override void Tick() => throw new NotImplementedException();
}
