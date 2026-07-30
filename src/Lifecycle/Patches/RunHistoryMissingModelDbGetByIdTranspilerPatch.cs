using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Redirects character and Act lookups on the Run History UI to <see cref="RunHistoryMissingModelSupport" />,
    ///         allowing history from unavailable mods to use deprecated-model placeholders.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将游戏历史界面中的角色与章节查找重定向到 <see cref="RunHistoryMissingModelSupport" />，
    ///         使来自当前不可用模组的历史记录可使用已弃用模型占位项。
    ///     </para>
    /// </summary>
    internal class RunHistoryMissingModelDbGetByIdTranspilerPatch : IPatchMethod
    {
        private static readonly MethodInfo CharacterFallback =
            AccessTools.DeclaredMethod(typeof(RunHistoryMissingModelSupport),
                nameof(RunHistoryMissingModelSupport.CharacterForRunHistory));

        private static readonly MethodInfo ActFallback =
            AccessTools.DeclaredMethod(typeof(RunHistoryMissingModelSupport),
                nameof(RunHistoryMissingModelSupport.ActForRunHistory));

        public static string PatchId => "run_history_missing_model_db_getbyid_transpile";

        public static string Description =>
            "Transpile run-history methods to use Character/Act fallbacks when ModelDb has no entry";

        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NRunHistoryPlayerIcon), nameof(NRunHistoryPlayerIcon.LoadRun),
                    [typeof(RunHistoryPlayer), typeof(RunHistory)]),
                new(typeof(NMapPointHistory), nameof(NMapPointHistory.LoadHistory), [typeof(RunHistory)]),
                new(typeof(NMapPointHistoryEntry), "DoCombatAnimateInEffects", [typeof(RoomType)]),
                new(typeof(NRunHistory), "SelectPlayer", [typeof(NRunHistoryPlayerIcon)]),
                new(typeof(NRunHistory), "LoadGoldHpAndPotionInfo", [typeof(NRunHistoryPlayerIcon)]),
                new(typeof(NRunHistory), "LoadDeathQuote", [typeof(RunHistory), typeof(ModelId)]),
                new(typeof(NRunHistory), nameof(NRunHistory.GetDeathQuote),
                    [typeof(RunHistory), typeof(ModelId), typeof(GameOverType)]),
            ];
        }

        /// <summary>
        ///     <para xml:lang="en">Redirects supported <c>ModelDb.GetById</c> calls to RitsuLib fallbacks.</para>
        ///     <para xml:lang="zh-CN">将受支持的 <c>ModelDb.GetById</c> 调用重定向到 RitsuLib 回退实现。</para>
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var rewriter = HarmonyIlRewriter.From(instructions);
            const string operation = "[RunHistory] Redirect ModelDb.GetById calls";
            var targets = rewriter.FindCalls(IsSupportedModelDbGetById, operation);

            if (!targets.Any && !rewriter.FindAll(IsFallbackCall, "[RunHistory] existing fallback calls").Any)
                return rewriter.InstructionsChecked(operation);

            var report = rewriter.RedirectCalls(
                operation,
                ResolveModelDbFallback,
                static code => code.Any(IsFallbackCall));
            if (report.Changed)
                report.RequireApplied();

            return rewriter.InstructionsChecked(operation);
        }

        private static MethodInfo? ResolveModelDbFallback(MethodInfo called)
        {
            if (IsModelDbGetByIdFor(called, typeof(CharacterModel)))
                return CharacterFallback;

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (IsModelDbGetByIdFor(called, typeof(ActModel)))
                return ActFallback;

            return null;
        }

        private static bool IsFallbackCall(CodeInstruction instruction)
        {
            return HarmonyIl.IsCall(method => method == CharacterFallback || method == ActFallback)(instruction);
        }

        private static bool IsSupportedModelDbGetById(MethodInfo method)
        {
            return IsModelDbGetByIdFor(method, typeof(CharacterModel)) || IsModelDbGetByIdFor(method, typeof(ActModel));
        }

        private static bool IsModelDbGetByIdFor(MethodInfo mi, Type typeArg)
        {
            if (!mi.IsGenericMethod || mi.DeclaringType != typeof(ModelDb))
                return false;

            var def = mi.GetGenericMethodDefinition();
            if (def.Name != nameof(ModelDb.GetById))
                return false;

            var args = mi.GetGenericArguments();
            return args.Length == 1 && args[0] == typeArg;
        }
    }
}
