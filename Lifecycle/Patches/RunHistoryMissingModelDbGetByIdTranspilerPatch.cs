using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;

namespace STS2RitsuLib.Lifecycle.Patches
{
    /// <summary>
    ///     Replaces <c>ModelDb.GetById&lt;CharacterModel&gt;</c> and <c>GetById&lt;ActModel&gt;</c> in run-history UI with
    ///     <see cref="RunHistoryMissingModelSupport" /> so missing mod content does not throw.
    /// </summary>
    public class RunHistoryMissingModelDbGetByIdTranspilerPatch : IPatchMethod
    {
        private static readonly MethodInfo CharacterFallback =
            AccessTools.DeclaredMethod(typeof(RunHistoryMissingModelSupport),
                nameof(RunHistoryMissingModelSupport.CharacterForRunHistory));

        private static readonly MethodInfo ActFallback =
            AccessTools.DeclaredMethod(typeof(RunHistoryMissingModelSupport),
                nameof(RunHistoryMissingModelSupport.ActForRunHistory));

        /// <inheritdoc />
        public static string PatchId => "run_history_missing_model_db_getbyid_transpile";

        /// <inheritdoc />
        public static string Description =>
            "Transpile run-history methods to use Character/Act fallbacks when ModelDb has no entry";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
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
        ///     Harmony transpiler: redirect ModelDb lookups to RitsuLib fallbacks.
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.operand is MethodInfo called)
                {
                    if (IsModelDbGetByIdFor(called, typeof(CharacterModel)))
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = CharacterFallback;
                    }
                    else if (IsModelDbGetByIdFor(called, typeof(ActModel)))
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = ActFallback;
                    }
                }

                yield return code;
            }
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
