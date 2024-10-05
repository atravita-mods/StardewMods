using System.Reflection.Emit;
using System.Reflection;

namespace SinZsEventTester.Framework;

/// <summary>
/// Queues commands to SMAPI
/// </summary>
internal static class SMAPICommandQueuer
{

    /// <summary>
    /// Queues up a console command. Thanks, Shockah!
    /// </summary>
    private static Lazy<Action<string>> _queueConsoleCommand = new(() => {
        var sCoreType = Type.GetType( "StardewModdingAPI.Framework.SCore,StardewModdingAPI")!;
        var commandQueueType = Type.GetType("StardewModdingAPI.Framework.CommandQueue,StardewModdingAPI")!;
        var sCoreGetter = sCoreType.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static)!.GetGetMethod(true)!;
        var rawCommandQueueField = sCoreType.GetField("RawCommandQueue",BindingFlags.NonPublic | BindingFlags.Instance)!;
        var queueAddMethod = commandQueueType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance)!;

        var method = new DynamicMethod("QueueConsoleCommand", null, [typeof(string)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Call, sCoreGetter);
        il.Emit(OpCodes.Ldfld, rawCommandQueueField);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, queueAddMethod);
        il.Emit(OpCodes.Ret);
        return method.CreateDelegate<Action<string>>();
    });

    internal static Action<string> QueueConsoleCommand => _queueConsoleCommand.Value;
}
