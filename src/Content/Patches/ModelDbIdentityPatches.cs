using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Content.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Supplies the registered public model entry for RitsuLib-owned model types.
    ///     </para>
    ///     <para xml:lang="zh-CN">为 RitsuLib 所属模型类型提供已注册的公共模型条目。</para>
    /// </summary>
    internal class ModelDbModdedEntryPatch : IPatchMethod
    {
        public static string PatchId => "modeldb_modded_entry_identity";

        public static string Description =>
            "Force RitsuLib-registered models to use a fixed mod-scoped ModelDb entry format";

        public static bool IsCritical => true;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(ModelDb), nameof(ModelDb.GetEntry), [typeof(Type)])];
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Replaces <paramref name="__result" /> with the registered public entry when
        ///         <paramref name="type" /> is owned by a mod.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         当 <paramref name="type" /> 由模组拥有时，将 <paramref name="__result" /> 替换为已注册公共条目。
        ///     </para>
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Type type, ref string __result)
        {
            if (!ModContentRegistry.TryGetFixedPublicEntry(type, out var fixedEntry))
                return;

            __result = fixedEntry;
        }
    }
}
