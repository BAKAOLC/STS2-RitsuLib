using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     Bridges model-saved data through <see cref="SavedProperties" />.
    ///     将模型保存数据桥接到 <see cref="SavedProperties" />。
    /// </summary>
    internal static class ModelSavedDataPatches
    {
        /// <summary>
        ///     Exports registered model-saved data after vanilla model properties are serialized.
        ///     在原版模型属性序列化后导出已注册模型保存数据。
        /// </summary>
        internal sealed class SavedPropertiesFromInternalPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_model_saved_data_SavedProperties_FromInternal";

            /// <inheritdoc />
            public static string Description =>
                "Bridge ModelSavedData through SavedProperties save -> SavedProperties.FromInternal(...)";

            /// <inheritdoc />
            public static bool IsCritical => true;

            /// <inheritdoc />
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
                if (model is not AbstractModel abstractModel)
                    return;

                var json = ModelSavedDataRegistry.Export(abstractModel);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var props = __result ?? new SavedProperties();
                SavedAttachedStateRegistry.AddToProperties(
                    props,
                    ModelSavedDataRuntime.SavedPropertiesName,
                    json);
                __result = props;
            }
        }

        /// <summary>
        ///     Imports registered model-saved data after vanilla model properties are deserialized.
        ///     在原版模型属性反序列化后导入已注册模型保存数据。
        /// </summary>
        internal sealed class SavedPropertiesFillInternalPatch : IPatchMethod
        {
            /// <inheritdoc />
            public static string PatchId => "ritsulib_model_saved_data_SavedProperties_FillInternal";

            /// <inheritdoc />
            public static string Description =>
                "Bridge ModelSavedData through SavedProperties load -> SavedProperties.FillInternal(...)";

            /// <inheritdoc />
            public static bool IsCritical => true;

            /// <inheritdoc />
            public static ModPatchTarget[] GetTargets()
            {
                return
                [
                    new(typeof(SavedProperties), nameof(SavedProperties.FillInternal), [typeof(object)]),
                ];
            }

            public static void Postfix(SavedProperties __instance, object model)
            {
                if (model is not AbstractModel abstractModel)
                    return;

                if (SavedAttachedStateRegistry.TryGetFromProperties<string>(
                        __instance,
                        ModelSavedDataRuntime.SavedPropertiesName,
                        out var json))
                    ModelSavedDataRegistry.Import(abstractModel, json);
            }
        }
    }
}
