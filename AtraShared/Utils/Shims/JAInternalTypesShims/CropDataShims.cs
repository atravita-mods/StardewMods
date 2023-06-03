using System.Reflection;

using AtraBase.Toolkit;
using AtraBase.Toolkit.Reflection;

using FastExpressionCompiler.LightExpression;

using HarmonyLib;

namespace AtraShared.Utils.Shims.JAInternalTypesShims;

/// <summary>
/// Shims against JA's crop data class.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = StyleCopErrorConsts.AccessorsNearFields)]
internal static class CropDataShims
{
    private static readonly Lazy<Func<object, string?>?> getSeedName = new(
        () =>
        {
            Type cropData = AccessTools.TypeByName("JsonAssets.Data.CropData");

            if (cropData is null)
            {
                return null;
            }

            ParameterExpression obj = Expression.ParameterOf<object>("obj");
            TypeBinaryExpression isInst = Expression.TypeIs(obj, cropData);
            ParameterExpression ret = Expression.ParameterOf<string>("ret");

            BinaryExpression returnnull = Expression.Assign(ret, Expression.ConstantNull<string>());

            MethodInfo nameGetter = cropData.InstancePropertyNamed("SeedName").GetGetMethod()
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("SeedName");

            UnaryExpression casted = Expression.TypeAs(obj, cropData);
            BinaryExpression assign = Expression.Assign(ret, Expression.Call(casted, nameGetter));

            ConditionalExpression ifStatement = Expression.IfThenElse(isInst, assign, returnnull);

            BlockExpression block = Expression.Block(typeof(string), new List<ParameterExpression>() { ret }, ifStatement, ret);
            return Expression.Lambda<Func<object, string?>>(block, obj).CompileFast();
        });

    /// <summary>
    /// Gets the seed name from a JA ObjectData.
    /// </summary>
    public static Func<object, string?>? GetSeedName => getSeedName.Value;

    private static readonly Lazy<Func<object, IList<string>?>?> getSeedRestrictions = new(
        () =>
        {
            Type cropData = AccessTools.TypeByName("JsonAssets.Data.CropData");

            if (cropData == null)
            {
                return null;
            }

            ParameterExpression obj = Expression.ParameterOf<object>("obj");
            TypeBinaryExpression isInst = Expression.TypeIs(obj, cropData);
            ParameterExpression ret = Expression.ParameterOf<string[]>("ret");

            BinaryExpression returnnull = Expression.Assign(ret, Expression.ConstantNull<IList<string>?>());

            MethodInfo restrictionGetter = cropData.InstancePropertyNamed("SeedPurchaseRequirements").GetGetMethod()
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("SeedPurchaseRequirements");

            UnaryExpression casted = Expression.TypeAs(obj, cropData);
            BinaryExpression assign = Expression.Assign(ret, Expression.Call(casted, restrictionGetter));

            ConditionalExpression ifStatement = Expression.IfThenElse(isInst, assign, returnnull);

            BlockExpression block = Expression.Block(typeof(IList<string>), new List<ParameterExpression>() { ret }, ifStatement, ret);
            return Expression.Lambda<Func<object, IList<string>?>>(block, obj).CompileFast();
        });

    /// <summary>
    /// Gets the seed purchase requirements.
    /// </summary>
    public static Func<object, IList<string>?>? GetSeedRestrictions => getSeedRestrictions.Value;

    private static readonly Lazy<Func<object, int>?> getSeedPurchase = new(() =>
    {
        Type cropData = AccessTools.TypeByName("JsonAssets.Data.CropData");

        if (cropData == null)
        {
            return null;
        }

        ParameterExpression obj = Expression.ParameterOf<object>("obj");
        TypeBinaryExpression isInst = Expression.TypeIs(obj, cropData);
        ParameterExpression ret = Expression.ParameterOf<int>("ret");

        BinaryExpression returnnull = Expression.Assign(ret, Expression.ConstantInt(0));

        MethodInfo restrictionGetter = cropData.InstancePropertyNamed("SeedPurchasePrice").GetGetMethod()
            ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("SeedPurchasePrice");

        UnaryExpression casted = Expression.TypeAs(obj, cropData);
        BinaryExpression assign = Expression.Assign(ret, Expression.Call(casted, restrictionGetter));

        ConditionalExpression ifStatement = Expression.IfThenElse(isInst, assign, returnnull);

        BlockExpression block = Expression.Block(typeof(int), new List<ParameterExpression>() { ret }, ifStatement, ret);
        return Expression.Lambda<Func<object, int>>(block, obj).CompileFast();
    });

    /// <summary>
    /// Gets a value indicating how much the seed purchase price for this crop is.
    /// </summary>
    public static Func<object, int>? GetSeedPurchase => getSeedPurchase.Value;
}
