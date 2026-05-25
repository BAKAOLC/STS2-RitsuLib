using System.Reflection;
using System.Reflection.Emit;
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
            var pattern = HarmonyIlPattern.Sequence(
                HarmonyIl.IsStloc(),
                IsModelDbActsCall,
                HarmonyIl.IsLdloc(),
                instruction => IsLinqCall(instruction, nameof(Enumerable.Except)),
                instruction => IsLinqCall(instruction, nameof(Enumerable.Any)),
                HarmonyIl.IsBranch(),
                IsActListErrorString);

            var report = rewriter.TryInsertBeforeFirst(
                "[RelicCollection] Replace hard-coded ancient act list",
                pattern,
                [
                    HarmonyIl.Pop(),
                    HarmonyIl.Call(RuntimeActListMethod),
                ],
                code => code.Any(instruction => HarmonyIl.IsCallTo(instruction, RuntimeActListMethod)));

            if (report.Succeeded)
            {
                if (report.Applied > 0)
                    report.RequireExactly(1);
                return rewriter.InstructionsChecked("[RelicCollection] Replace hard-coded ancient act list");
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
            return HarmonyIl.IsCallTo(instruction, ModelDbActsGetter);
        }

        private static bool IsLinqCall(CodeInstruction instruction, string methodName)
        {
            if (instruction.opcode != OpCodes.Call || instruction.operand is not MethodInfo method)
                return false;

            if (method.DeclaringType != typeof(Enumerable) || method.Name != methodName)
                return false;

            return !method.IsGenericMethod || method.GetGenericArguments().Contains(typeof(ActModel));
        }

        private static bool IsActListErrorString(CodeInstruction instruction)
        {
            return instruction.opcode == OpCodes.Ldstr && instruction.operand is string value &&
                   value.Contains("act list", StringComparison.OrdinalIgnoreCase);
        }
    }
}
