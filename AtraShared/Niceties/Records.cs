using AtraShared.ConstantsAndEnums;
using Microsoft.Xna.Framework;

namespace AtraShared.Niceties;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter. These are records.
public readonly record struct Tile(string Map, Vector2 Pos);

public readonly record struct ItemRecord(ItemTypeEnum Type, int Id);
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter