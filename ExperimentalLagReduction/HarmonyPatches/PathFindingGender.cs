namespace ExperimentalLagReduction.HarmonyPatches;

using NetEscapades.EnumGenerators;

[EnumExtensions]
public enum PathfindingGender
{
    Male = Gender.Male,
    Female = Gender.Female,
    Undefined = Gender.Undefined,

    Invalid = -2,

}