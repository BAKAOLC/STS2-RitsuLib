using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Bridges <see cref="SavedAttachedState{TKey,TValue}" /> instances into <see cref="SavedProperties" />
    ///     serialization and deserialization.
    ///     将 <see cref="SavedAttachedState{TKey,TValue}" /> 实例桥接到 <see cref="SavedProperties" />
    ///     序列化和反序列化。
    /// </summary>
    public static class SavedAttachedStatePatches
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
        ///     Exports registered saved attached states after vanilla model properties are serialized.
        ///     在原版模型属性序列化后导出已注册的已保存附加状态。
        /// </summary>
        public sealed class SavedPropertiesFromInternalPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_saved_attached_state_SavedProperties_FromInternal";

            /// <inheritdoc />
            public static string Description =>
                "Bridge SavedAttachedState through SavedProperties save -> SavedProperties.FromInternal(...)";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SavedProperties), nameof(SavedProperties.FromInternal),
                        [typeof(object), typeof(ModelId)]),
                ];
            }

            /// <summary>
            ///     Exports registered saved attached states after vanilla model properties are serialized.
            ///     在原版模型属性序列化后导出已注册的已保存附加状态。
            /// </summary>
            public static void Postfix(ref SavedProperties? __result, object model, ModelId? id)
            {
                ExportAttachedStates(ref __result, model);
            }
        }

        /// <summary>
        ///     Imports registered saved attached states after vanilla model properties are deserialized.
        ///     在原版模型属性反序列化后导入已注册的已保存附加状态。
        /// </summary>
        public sealed class SavedPropertiesFillInternalPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_saved_attached_state_SavedProperties_FillInternal";

            /// <inheritdoc />
            public static string Description =>
                "Bridge SavedAttachedState through SavedProperties load -> SavedProperties.FillInternal(...)";

            /// <inheritdoc />
            public static bool IsCritical => false;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SavedProperties), nameof(SavedProperties.FillInternal), [typeof(object)]),
                ];
            }

            /// <summary>
            ///     Imports registered saved attached states after vanilla model properties are deserialized.
            ///     在原版模型属性反序列化后导入已注册的已保存附加状态。
            /// </summary>
            public static void Postfix(SavedProperties __instance, object model)
            {
                ImportAttachedStates(__instance, model);
            }
        }
    }
}
