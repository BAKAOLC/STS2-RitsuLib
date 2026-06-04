using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.HarmonyIl;

namespace STS2RitsuLib.Scaffolding.Content.Patches
{
    /// <summary>
    ///     Replaces the vanilla relic collection's hard-coded ancient act list with the runtime <see cref="ModelDb.Acts" />
    ///     sequence so registered mod acts can contribute ancient relic subcategories.
    /// </summary>
    public class RelicCollectionActListPatch : IPatchMethod
    {
        private static readonly MethodInfo ModelDbActsGetter =
            AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.Acts));

        private static readonly MethodInfo RuntimeActListMethod =
            AccessTools.DeclaredMethod(typeof(RelicCollectionActListPatch), nameof(GetRuntimeActList));

        /// <inheritdoc />
        public static string PatchId => "relic_collection_runtime_act_list";

        /// <inheritdoc />
        public static string Description => "Use runtime ModelDb.Acts for relic collection ancient categories";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))];
        }

        /// <summary>
        ///     Stores the runtime act list into the local variable that vanilla later uses for ancient category generation.
        /// </summary>
        [HarmonyAfter(Const.BaseLibHarmonyId)]
        [HarmonyPriority(Priority.Last)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var rewriter = HarmonyIlRewriter.From(instructions);
            const string operation = "[RelicCollection] Replace hard-coded ancient act list";

            if (rewriter.Contains(instruction => HarmonyIl.IsCallTo(instruction, RuntimeActListMethod)))
                return rewriter.InstructionsChecked(operation);

            if (TryFindVanillaActListStore(rewriter, out var match))
            {
                rewriter.InsertBefore(match,
                [
                    HarmonyIl.Pop(),
                    HarmonyIl.Call(RuntimeActListMethod),
                ]);
                return rewriter.InstructionsChecked(operation);
            }

            RitsuLibFramework.Logger.Warn(
                "[RelicCollection] Could not find vanilla act-list validation pattern; runtime act list patch skipped.");

            return rewriter.Instructions();
        }

        private static List<ActModel> GetRuntimeActList()
        {
            return ModelDb.Acts
                .DistinctBy(static act => act.Id)
                .Select(static (act, index) => new { Act = act, Index = index })
                .OrderBy(static item => GetVanillaActOrder(item.Act))
                .ThenBy(static item => item.Index)
                .Select(static item => item.Act)
                .ToList();
        }

        private static int GetVanillaActOrder(ActModel act)
        {
            return act switch
            {
                Overgrowth => 0,
                Underdocks => 1,
                Hive => 2,
                Glory => 3,
                _ => 4,
            };
        }

        private static bool IsModelDbActsCall(CodeInstruction instruction)
        {
            return HarmonyIl.IsCall(ModelDbActsGetter)(instruction);
        }

        private static bool IsLinqCall(CodeInstruction instruction, string methodName)
        {
            return HarmonyIl.IsCall(method =>
                method.DeclaringType == typeof(Enumerable) &&
                method.Name == methodName &&
                (!method.IsGenericMethod || method.GetGenericArguments().Contains(typeof(ActModel))))(instruction);
        }

        private static bool IsActListErrorString(CodeInstruction instruction)
        {
            return HarmonyIl.OperandMatches<string>(instruction,
                value => value.Contains("act list", StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryFindVanillaActListStore(HarmonyIlRewriter rewriter, out HarmonyIlMatch match)
        {
            foreach (var errorMatch in rewriter.FindAll(IsActListErrorString, "relic collection act-list error string")
                         .Items)
            {
                if (!rewriter.TryFindBefore(errorMatch,
                        instruction => IsLinqCall(instruction, nameof(Enumerable.Any)), out var anyMatch))
                    continue;

                if (!rewriter.TryFindBefore(anyMatch,
                        instruction => IsLinqCall(instruction, nameof(Enumerable.Except)), out var exceptMatch))
                    continue;

                if (!rewriter.TryFindBefore(exceptMatch, IsModelDbActsCall, out var actsMatch))
                    continue;

                if (!rewriter.TryFindBefore(actsMatch, instruction => instruction.IsStloc(), out match))
                    continue;

                return true;
            }

            match = default;
            return false;
        }
    }
}
