using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Models.Capabilities.Patches
{
    /// <summary>
    ///     <para xml:lang="en">Bridges model-saved data through <see cref="SavedProperties" />.</para>
    ///     <para xml:lang="zh-CN">通过 <see cref="SavedProperties" /> 传递模型保存数据。</para>
    /// </summary>
    internal static class ModelSavedDataPatches
    {
        private static void RemoveSavedData(SavedProperties properties)
        {
            RemoveByName(ref properties.ints);
            RemoveByName(ref properties.bools);
            RemoveByName(ref properties.strings);
            RemoveByName(ref properties.intArrays);
            RemoveByName(ref properties.modelIds);
            RemoveByName(ref properties.cards);
            RemoveByName(ref properties.cardArrays);
            return;

            static void RemoveByName<T>(ref List<SavedProperties.SavedProperty<T>>? values)
            {
                if (values == null)
                    return;

                values.RemoveAll(static value => value.name == ModelSavedDataRuntime.SavedPropertiesName);
                if (values.Count == 0)
                    values = null;
            }
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Exports registered model-saved data after base-game model properties are serialized.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         游戏原版模型属性序列化后，导出已注册的模型保存数据。
        ///     </para>
        /// </summary>
        internal sealed class SavedPropertiesFromInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_saved_data_SavedProperties_FromInternal";

            public static string Description =>
                "Bridge ModelSavedData through SavedProperties save -> SavedProperties.FromInternal(...)";

            public static bool IsCritical => true;

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
        ///     <para xml:lang="en">
        ///         Imports registered model-saved data after base-game model properties are deserialized.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         游戏原版模型属性反序列化后，导入已注册的模型保存数据。
        ///     </para>
        /// </summary>
        internal sealed class SavedPropertiesFillInternalPatch : IPatchMethod
        {
            public static string PatchId => "ritsulib_model_saved_data_SavedProperties_FillInternal";

            public static string Description =>
                "Bridge ModelSavedData through SavedProperties load -> SavedProperties.FillInternal(...)";

            public static bool IsCritical => true;

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

                if (!ModelSavedDataRegistry.IsPropertyNameRegistered)
                {
                    RemoveSavedData(__instance);
                    return;
                }

                SavedAttachedStateRegistry.TryGetFromProperties<string>(
                    __instance,
                    ModelSavedDataRuntime.SavedPropertiesName,
                    out var json);
                ModelSavedDataRegistry.Import(abstractModel, json);
            }
        }
    }
}
