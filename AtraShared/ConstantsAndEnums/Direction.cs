using CommunityToolkit.Diagnostics;

using Microsoft.Xna.Framework;

using NetEscapades.EnumGenerators;

using static System.Numerics.BitOperations;

namespace AtraShared.ConstantsAndEnums;

/// <summary>
/// A bitwise enum for directions.
/// </summary>
[Flags]
[EnumExtensions]
public enum Direction : byte
{
    /// <summary>
    /// No direction.
    /// </summary>
    None = 0,

    /// <summary>
    /// Upwards.
    /// </summary>
    Up = 0b1 << Game1.up,

    /// <summary>
    /// Downwards.
    /// </summary>
    Down = 0b1 << Game1.down,

    /// <summary>
    /// Rightwards.
    /// </summary>
    Right = 0b1 << Game1.right,

    /// <summary>
    /// Leftwards
    /// </summary>
    Left = 0b1 << Game1.left,
}

/// <summary>
/// Extension methods for the <see cref="Direction"/> enum.
/// </summary>
public static partial class DirectionExtensions
{
    #region consts

    private static readonly Direction[] cardinal =
    [
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right,
    ];

    private static readonly Direction[] ordinal =
    [
        Direction.Up | Direction.Right,
        Direction.Up | Direction.Left,
        Direction.Down | Direction.Right,
        Direction.Down | Direction.Left,
    ];

    private static readonly Direction[] valid =
    [
        Direction.Up,
        Direction.Down,
        Direction.Left,
        Direction.Right,
        Direction.Up | Direction.Right,
        Direction.Up | Direction.Left,
        Direction.Down | Direction.Right,
        Direction.Down | Direction.Left,
    ];

    /// <summary>
    /// Gets all valid directions.
    /// </summary>
    public static ReadOnlySpan<Direction> Valid => valid.AsSpan();

    /// <summary>
    /// Gets the cardinal directions.
    /// </summary>
    public static ReadOnlySpan<Direction> Cardinal => cardinal.AsSpan();

    /// <summary>
    /// Gets the ordinal directions.
    /// </summary>
    public static ReadOnlySpan<Direction> Ordinal => ordinal.AsSpan();

    #endregion

    /// <summary>
    /// Gets the matching direction enum out of the game direction.
    /// </summary>
    /// <param name="direction">Game direction.</param>
    /// <returns>Direction enum.</returns>
    public static Direction FromGameDirection(int direction)
    {
        Guard.IsBetweenOrEqualTo(direction, 0, 3);
        return (Direction)(0b1 << direction);
    }

    /// <summary>
    /// Checks to see if a direction enum is valid.
    /// </summary>
    /// <param name="direction">Direction enum to check.</param>
    /// <returns>True if it corresponds to (a) no direction, (b) one of the four cardinal directions, or (c) a diagonal. False otherwise.</returns>
    public static bool IsValidDirection(this Direction direction)
    {
        switch (PopCount((uint)direction))
        {
            case 0:
            case 1:
                return true;
            case 2:
                if (direction.HasFlagFast(Direction.Up) && direction.HasFlagFast(Direction.Down))
                {
                    return false;
                }

                if (direction.HasFlagFast(Direction.Left) && direction.HasFlagFast(Direction.Right))
                {
                    return false;
                }

                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the float rotation that faces into a specific direction.
    /// </summary>
    /// <param name="direction">Direction enum.</param>
    /// <returns>Float corresponding to a rotation facing the direction. (ie Up = -pi/2).</returns>
    public static float GetRotationFacing(this Direction direction)
    {
        if (!IsValidDirection(direction))
        {
            ThrowHelper.ThrowArgumentException($"{direction.ToHexString()} is not valid");
        }

        return direction switch
        {
            Direction.Up => -MathF.PI / 2,
            Direction.Left => MathF.PI,
            Direction.Down => MathF.PI / 2,
            Direction.Right => 0,
            Direction.Up | Direction.Left => -3 * MathF.PI / 4,
            Direction.Up | Direction.Right => -MathF.PI / 4,
            Direction.Down | Direction.Left => 3 * MathF.PI / 4,
            Direction.Down | Direction.Right => MathF.PI / 4,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<float>($"Unexpected direction! {direction.ToHexString()}"),
        };
    }

    /// <summary>
    /// Gets a vector facing into a specific direction. ie, <see cref="Direction.Up"/> gets -Vector2.UnitY.
    /// </summary>
    /// <param name="direction">Direction enum.</param>
    /// <returns>vector pointing in that direction.</returns>
    public static Vector2 GetVectorFacing(this Direction direction)
    {
        if (!IsValidDirection(direction))
        {
            ThrowHelper.ThrowArgumentException($"{direction.ToHexString()} is not valid");
        }

        int x = 0;
        int y = 0;

        if (direction.HasFlagFast(Direction.Left))
        {
            x = -1;
        }
        else if (direction.HasFlagFast(Direction.Right))
        {
            x = 1;
        }

        if (direction.HasFlagFast(Direction.Up))
        {
            y = -1;
        }
        else if (direction.HasFlagFast(Direction.Down))
        {
            y = 1;
        }

        return new(x, y);
    }
}