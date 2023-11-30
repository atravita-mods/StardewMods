namespace MoreFertilizers.DataModels.UserModels;

/// <summary>
/// Checks to see if this rule should apply to paddy crops, non paddy crops, or both.
/// </summary>
[Flags]
public enum PaddyStatus
{
    IsPaddyCrop = 0b1,
    IsNotPaddyCrop = 0b10,
    Both = 0b11,
}
