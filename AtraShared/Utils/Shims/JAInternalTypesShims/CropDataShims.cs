using System.Reflection;
using AtraBase.Toolkit.Reflection;
using FastExpressionCompiler.LightExpression;
using HarmonyLib;

namespace AtraShared.Utils.Shims.JAInternalTypesShims;

/// <summary>
/// Shims against JA's crop data class.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Accessors kept near fields.")]
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

            var obj = Expression.ParameterOf<object>("obj");
            var isInst = Expression.TypeIs(obj, cropData);
            var ret = Expression.ParameterOf<string>("ret");

            var returnnull = Expression.Assign(ret, Expression.ConstantNull<string>());

            MethodInfo nameGetter = cropData.InstancePropertyNamed("SeedName").GetGetMethod()
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("SeedName");

            var casted = Expression.TypeAs(obj, cropData);
            var assign = Expression.Assign(ret, Expression.Call(casted, nameGetter));

            var ifStatement = Expression.IfThenElse(isInst, assign, returnnull);
            List<ParameterExpression> param = new()
            {
                ret
            };

            var block = Expression.Block(typeof(string), param, ifStatement, ret);
            return Expression.Lambda<Func<object, string?>>(block, obj).CompileFast();
        });

    /// <summary>
    /// Gets the seed name from a JA ObjectData.
    /// </summary>
    public static Func<object, string?>? GetSeedName => getSeedName.Value;

#warning - incomplete
    private static  readonly Lazy<Func<object, IList<string>?>?> getSeedRestrictions = new(
        () =>
        {
            Type cropData = AccessTools.TypeByName("JsonAssets.Data.CropData");

            if (cropData == null)
            {
                return null;
            }

            var obj = Expression.ParameterOf<object>("obj");
            var isInst = Expression.TypeIs(obj, cropData);
            var ret = Expression.ParameterOf<string[]>("ret");

            var returnnull = Expression.Assign(ret, Expression.ConstantNull<IList<string>?>());

            MethodInfo restrictionGetter = cropData.InstancePropertyNamed("SeedPurchaseRequirements").GetGetMethod()
                ?? ReflectionThrowHelper.ThrowMethodNotFoundException<MethodInfo>("SeedPurchaseRequirements");

            var casted = Expression.TypeAs(obj, cropData);
            var assign = Expression.Assign(ret, Expression.Call(casted, restrictionGetter));

            var ifStatement = Expression.IfThenElse(isInst, assign, returnnull);
            List<ParameterExpression> param = new();
            param.Add(ret);

            var block = Expression.Block(typeof(IList<string>), param, ifStatement, ret);
            return Expression.Lambda<Func<object, IList<string>?>>(block, obj).CompileFast();
        });

    public static Func<object, IList<string>?>? GetSeedRestrictions => getSeedRestrictions.Value;
}
