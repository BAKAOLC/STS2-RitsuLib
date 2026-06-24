using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal static class LifecyclePatchTaskBridge
    {
        public static Task After(Task originalTask, Action continuation)
        {
            return HarmonyAsyncTaskBridge.After(originalTask, continuation);
        }

        public static Task<T> After<T>(Task<T> originalTask, Action<T> continuation)
        {
            return HarmonyAsyncTaskBridge.After(originalTask, continuation);
        }

        public static Task<T> After<T>(Task<T> originalTask, Func<T, Task> continuation)
        {
            return HarmonyAsyncTaskBridge.After(originalTask, continuation);
        }
    }
}
