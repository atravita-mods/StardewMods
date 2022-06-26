using Microsoft.Xna.Framework;

namespace AtraShared.Niceties;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "This is a record lol.")]
public readonly record struct Tile(string Map, Vector2 Pos);
