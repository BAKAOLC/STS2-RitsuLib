using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Saves;

namespace STS2RitsuLib.Lifecycle.Patches
{
    internal static class RunHistoryMissingModelScope
    {
        [ThreadStatic] private static int _depth;

        internal static bool IsActive => _depth > 0;

        internal static void Enter()
        {
            _depth++;
        }

        internal static void Exit()
        {
            if (_depth > 0)
                _depth--;
        }
    }

    /// <summary>
    ///     Creates an execution scope for run-history UI methods that may read missing mod models.
    /// </summary>
    public class RunHistoryMissingModelDbGetByIdTranspilerPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "run_history_missing_model_db_getbyid_transpile";

        /// <inheritdoc />
        public static string Description =>
            "Create run-history scope for missing-model fallbacks";

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
        ///     Enters run-history missing-model support scope.
        /// </summary>
        public static void Prefix()
        {
            RunHistoryMissingModelScope.Enter();
        }

        /// <summary>
        ///     Exits run-history scope even if the target method throws.
        /// </summary>
        public static void Finalizer()
        {
            RunHistoryMissingModelScope.Exit();
        }
    }

    /// <summary>
    ///     Uses run-history-specific fallbacks for missing character/act lookups.
    /// </summary>
    public class RunHistoryMissingModelDbGetByIdPatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "run_history_missing_model_db_getbyid";

        /// <inheritdoc />
        public static string Description =>
            "Use Character/Act fallbacks in run-history scope when ModelDb.GetById has no entry";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.GetById), [typeof(ModelId)])];
        }

        /// <summary>
        ///     Replaces vanilla GetById throws with run-history fallback models.
        /// </summary>
        public static bool Prefix<T>(ModelId id, ref T __result) where T : AbstractModel
        {
            if (!RunHistoryMissingModelScope.IsActive)
                return true;

            if (typeof(T) == typeof(CharacterModel))
            {
                __result = (T)(AbstractModel)RunHistoryMissingModelSupport.CharacterForRunHistory(id);
                return false;
            }

            if (typeof(T) == typeof(ActModel))
            {
                __result = (T)(AbstractModel)RunHistoryMissingModelSupport.ActForRunHistory(id);
                return false;
            }

            return true;
        }
    }
}
