using System.Reflection;
using HarmonyLib;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Interop.Internal
{
    /// <summary>
    ///     Harmony transpiler entry for ModInterop. Uses <see cref="ThreadLocal{T}" /> so prefix IL is passed to
    ///     <see cref="Transpile" /> without locking across <see cref="Harmony.Patch" /> (avoids deadlock if Harmony
    ///     invokes the transpiler synchronously while still inside user code).
    /// </summary>
    internal static class HarmonyInsertBeforeRetTranspiler
    {
        private static readonly ThreadLocal<List<CodeInstruction>?> PendingPrefix = new();

        private static readonly HarmonyMethod InsertBeforeRetHarmonyMethod =
            new(typeof(HarmonyInsertBeforeRetTranspiler), nameof(Transpile));

        internal static void SetBufferAndPatch(Harmony harmony, MethodBase target, List<CodeInstruction> prefix)
        {
            ArgumentNullException.ThrowIfNull(harmony);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(prefix);
            if (PendingPrefix.Value is not null)
                throw new InvalidOperationException(
                    "Nested ModInterop transpiler setup on the same thread is not supported.");

            PendingPrefix.Value = [..prefix];
            try
            {
                harmony.Patch(target, transpiler: InsertBeforeRetHarmonyMethod);
            }
            finally
            {
                PendingPrefix.Value = null;
            }
        }

        public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            var p = PendingPrefix.Value;
            if (p is null || p.Count == 0)
                throw new InvalidOperationException(
                    "ModInterop transpiler ran without a pending prefix on this thread.");

            PendingPrefix.Value = null;
            return HarmonyVerifiedIl.InsertBeforeFirstRet(instructions, p);
        }
    }
}
