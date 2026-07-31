using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Integrates <see cref="SavedAttachedState{TKey,TValue}" /> instances with
    ///         <see cref="SavedProperties" /> serialization and deserialization.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="SavedAttachedState{TKey,TValue}" /> 实例接入
    ///         <see cref="SavedProperties" /> 的序列化与反序列化流程。
    ///     </para>
    /// </summary>
    internal static class SavedAttachedStatePatches
    {
        private static void ExportAttachedStates(ref SavedProperties? __result, object model)
        {
            var states = SavedAttachedStateRegistry.GetStatesForModel(model);
            if (states.Count == 0)
                return;

            var props = __result ?? new SavedProperties();
            var added = false;
            foreach (var state in states)
                if (state.Export(model, props))
                    added = true;

            if (__result == null && added)
                __result = props;
        }

        private static void ImportAttachedStates(SavedProperties __instance, object model)
        {
            foreach (var state in SavedAttachedStateRegistry.GetStatesForModel(model))
                state.Import(model, __instance);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Exports registered saved attached states after vanilla model properties are serialized.
        ///     </para>
        ///     <para xml:lang="zh-CN">在原版模型属性序列化后导出已注册的持久化附加状态。</para>
        /// </summary>
        internal sealed class SavedPropertiesFromInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_saved_attached_state_SavedProperties_FromInternal";

            public static string Description =>
                "Bridge SavedAttachedState through SavedProperties save -> SavedProperties.FromInternal(...)";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SavedProperties), nameof(SavedProperties.FromInternal),
                        [typeof(object), typeof(ModelId)]),
                ];
            }

            public static void Postfix(ref SavedProperties? __result, object model, ModelId? id)
            {
                ExportAttachedStates(ref __result, model);
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Imports registered saved attached states after vanilla model properties are deserialized.
        ///     </para>
        ///     <para xml:lang="zh-CN">在原版模型属性反序列化后导入已注册的持久化附加状态。</para>
        /// </summary>
        internal sealed class SavedPropertiesFillInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_saved_attached_state_SavedProperties_FillInternal";

            public static string Description =>
                "Bridge SavedAttachedState through SavedProperties load -> SavedProperties.FillInternal(...)";

            public static bool IsCritical => false;

            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SavedProperties), nameof(SavedProperties.FillInternal), [typeof(object)]),
                ];
            }

            public static void Postfix(SavedProperties __instance, object model)
            {
                ImportAttachedStates(__instance, model);
            }
        }
    }
}
