using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Interop.Patches
{
    /// <summary>
    ///     Bridges <see cref="SavedAttachedState{TKey,TValue}" /> instances into <see cref="SavedProperties" />
    ///     serialization and deserialization.
    /// </summary>
    public sealed class SavedAttachedStatePatch : IPatchMethod
    {
        /// <inheritdoc />
        public static string PatchId => "ritsulib_saved_attached_state";

        /// <inheritdoc />
        public static string Description => "Bridge SavedAttachedState through SavedProperties save and load";

        /// <inheritdoc />
        public static bool IsCritical => false;

        /// <inheritdoc />
        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SavedProperties), nameof(SavedProperties.FromInternal), [typeof(object), typeof(ModelId)]),
                new(typeof(SavedProperties), nameof(SavedProperties.FillInternal), [typeof(object)]),
            ];
        }

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     Exports registered saved attached states after vanilla model properties are serialized.
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, ref SavedProperties? __result,
                SavedProperties? __instance, object model)
            // ReSharper restore InconsistentNaming
        {
            switch (__originalMethod.Name)
            {
                case nameof(SavedProperties.FromInternal):
                    PostfixFromInternal(ref __result, model);
                    break;
                case nameof(SavedProperties.FillInternal) when __instance != null:
                    PostfixFillInternal(__instance, model);
                    break;
            }
        }

        // ReSharper disable once InconsistentNaming
        private static void PostfixFromInternal(ref SavedProperties? __result, object model)
        {
            var states = SavedAttachedStateRegistry.GetStatesForModel(model);
            if (states.Count == 0)
                return;

            var props = __result ?? new SavedProperties();
            var added = false;
            foreach (var state in states)
            {
                state.Export(model, props);
                added = true;
            }

            if (__result == null && added)
                __result = props;
        }

        // ReSharper disable once InconsistentNaming
        private static void PostfixFillInternal(SavedProperties __instance, object model)
        {
            foreach (var state in SavedAttachedStateRegistry.GetStatesForModel(model))
                state.Import(model, __instance);
        }
    }
}
